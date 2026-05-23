using System;
using System.Collections.Generic;
using System.Linq;
using Tideline.Core.Data;
using Tideline.Core.Models;
using Tideline.Core.Time;

namespace Tideline.Core.Briefing;

public enum BriefingBucket
{
    Pinned,
    Overdue,
    DueToday,
    Nudges,
    AgedSomeday,
}

public sealed record ScoredNote(Note Note, BriefingBucket Bucket, double Score);

public sealed record BriefingResult(IReadOnlyList<ScoredNote> Items)
{
    public bool IsEmpty => Items.Count == 0;
    public IReadOnlyList<ScoredNote> InBucket(BriefingBucket b) => Items.Where(i => i.Bucket == b).ToList();
}

/// <summary>
/// Date-driven scoring per SPEC section 8. Only due proximity and lateness drive
/// the score; the open questions in SPEC section 22 (snooze-as-signal, undated aging)
/// are deliberately NOT implemented here.
/// </summary>
public sealed class BriefingService
{
    public const int DefaultSomedayAgeDays = 30;
    public const int DefaultDecayThreshold = 3;

    private const double BaseOverdue = 1_000_000;
    private const double OverdueDayWeight = 1_000;
    private const double BaseDueToday = 500_000;
    private const double BaseDueSoon = 250_000;
    private const double DueSoonDayWeight = 1_000;
    private const double BaseNudge = 100_000;
    private const double BaseAgedSomeday = 1_000;

    private readonly NotesDb _db;
    private readonly NoteRepository _notes;
    private readonly IClock _clock;
    private readonly int _somedayAgeDays;

    public BriefingService(NotesDb db, NoteRepository notes, IClock clock, int somedayAgeDays = DefaultSomedayAgeDays)
    {
        _db = db;
        _notes = notes;
        _clock = clock;
        _somedayAgeDays = somedayAgeDays > 0 ? somedayAgeDays : DefaultSomedayAgeDays;
    }

    public BriefingResult Compute()
    {
        long nowMs = _clock.NowMs();
        DateTime today = _clock.Now().ToLocalTime().Date;
        long startOfTomorrowMs = new DateTimeOffset(today.AddDays(1), _clock.Now().ToLocalTime().Offset).ToUniversalTime().ToUnixTimeMilliseconds();
        long startOfTodayMs = new DateTimeOffset(today, _clock.Now().ToLocalTime().Offset).ToUniversalTime().ToUnixTimeMilliseconds();
        long somedayThresholdMs = nowMs - (long)TimeSpan.FromDays(_somedayAgeDays).TotalMilliseconds;

        List<ScoredNote> picked = new();
        HashSet<string> seen = new();

        foreach (Note note in _notes.All())
        {
            if (note.Pinned)
            {
                picked.Add(new ScoredNote(note, BriefingBucket.Pinned, double.MaxValue));
                seen.Add(note.Id);
                continue;
            }

            if (note.DueAt is long due)
            {
                if (due < startOfTodayMs)
                {
                    double daysOverdue = (nowMs - due) / 86_400_000.0;
                    double score = BaseOverdue + (daysOverdue * OverdueDayWeight);
                    picked.Add(new ScoredNote(note, BriefingBucket.Overdue, score));
                    seen.Add(note.Id);
                    continue;
                }
                if (due < startOfTomorrowMs)
                {
                    picked.Add(new ScoredNote(note, BriefingBucket.DueToday, BaseDueToday));
                    seen.Add(note.Id);
                    continue;
                }
                // Coming up but not today: classify as a nudge if the user already armed a remind.
                if (note.RemindAt is long r && r <= nowMs)
                {
                    double daysUntil = (due - nowMs) / 86_400_000.0;
                    double score = BaseDueSoon - (daysUntil * DueSoonDayWeight);
                    picked.Add(new ScoredNote(note, BriefingBucket.Nudges, score));
                    seen.Add(note.Id);
                }
                continue;
            }

            if (note.RemindAt is long ra && ra <= nowMs)
            {
                picked.Add(new ScoredNote(note, BriefingBucket.Nudges, BaseNudge));
                seen.Add(note.Id);
                continue;
            }

            if (note.SpaceId is null && note.DueAt is null && note.CreatedAt < somedayThresholdMs)
            {
                // Older notes float first within this bucket.
                double age = (nowMs - note.CreatedAt) / 86_400_000.0;
                double score = BaseAgedSomeday - age;
                picked.Add(new ScoredNote(note, BriefingBucket.AgedSomeday, score));
                seen.Add(note.Id);
            }
        }

        // Final order: bucket priority, then descending score inside the bucket.
        // Pinned first, then Overdue, Due today, Nudges, Aged someday.
        var ordered = picked
            .OrderBy(x => BucketRank(x.Bucket))
            .ThenByDescending(x => x.Score)
            .ToList();
        return new BriefingResult(ordered);
    }

    private static int BucketRank(BriefingBucket b) => b switch
    {
        BriefingBucket.Pinned => 0,
        BriefingBucket.Overdue => 1,
        BriefingBucket.DueToday => 2,
        BriefingBucket.Nudges => 3,
        BriefingBucket.AgedSomeday => 4,
        _ => 99,
    };
}
