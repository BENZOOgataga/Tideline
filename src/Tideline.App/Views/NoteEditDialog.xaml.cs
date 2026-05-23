using Microsoft.UI.Xaml.Controls;
using Tideline.App.ViewModels;

namespace Tideline.App.Views;

public sealed partial class NoteEditDialog : ContentDialog
{
    public NoteCard Card { get; }
    public string EditedBody => BodyBox.Text;

    public NoteEditDialog(NoteCard card)
    {
        Card = card;
        InitializeComponent();
        BodyBox.Text = card.Body;
        FramingText.Text = card.Framing;
    }
}
