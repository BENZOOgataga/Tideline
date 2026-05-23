# Agent Instructions

## Additional Instruction Sources

- Treat this file as the root agent entry point.
- Also check the root-level `.claude/` and `.agents/` directories for additional agent instructions and apply any relevant rules you find there.
- Re-check those directories when working in areas that may have more specific local guidance.

## Character Style

Match the prose character rule defined in [docs/plans/SPEC.md](docs/plans/SPEC.md):

- Use only straight quotes and basic punctuation in prose.
- Do not use em dashes, en dashes, the ellipsis character, or smart quotes.
- Code, identifiers, and config syntax are exempt.

## Commits

- Use Conventional Commits only.
- Keep commit subjects lowercase, in the imperative mood, with no trailing period.
- Do not add co-authors or `Co-authored-by` trailers.
