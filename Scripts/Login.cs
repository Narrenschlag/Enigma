namespace Enigma;

using Cutulu.Core;
using Cutulu.Encryption;
using Cutulu.UI;
using Godot;

public partial class Login : VBoxContainer
{
    [ExportGroup("Open")]
    [Export] private FileDrop File { get; set; }
    [Export] private FileDrop Key { get; set; }
    [Export] private HiddenInput Password { get; set; }
    [Export] private Button OpenButton { get; set; }

    [ExportGroup("Create")]
    [Export] private HiddenInput NewPassword { get; set; }
    [Export] private HiddenInput ConfirmPassword { get; set; }
    [Export] private Button CreateButton { get; set; }

    [ExportGroup("Feedback")]
    [Export] private string FeedbackFormat = "[center][color=red][b]{text}";
    [Export] private RichTextLabel Feedback { get; set; }

    public override void _Ready() => PostFeedback(string.Empty);

    public void Open()
    {
        if (File.HasFile() == false) PostFeedback("No file selected.");
        else if (Key.HasFile() == false) PostFeedback("No key selected.");
        else if (Password.Text.IsEmpty()) PostFeedback("Password is empty.");
        else
        {
            if (Master.LoadKey(Key.Files[0], Password.Text) == false)
                PostFeedback("Failed to open key file.");

            if (Master.UnlockEntries(File.Files[0]) == false)
                PostFeedback("Failed to open entry file.");

            else
            {
                PostFeedback(string.Empty);
                Visible = false;
            }
        }
    }

    public void Create()
    {
        if (NewPassword.Text.Length < 4) PostFeedback("Password must be at least 4 characters.");
        else if (NewPassword.Text.Length > SmartEncryption.KeySize) PostFeedback($"Password must be at most {SmartEncryption.KeySize} characters.");
        else if (NewPassword.Text != ConfirmPassword.Text) PostFeedback("Passwords have to match.");
        else
        {
            if (Master.WriteKey($"res://Docs/new_key.key", SmartEncryption.GenerateKey(), NewPassword.Text) == false)
                PostFeedback("Failed to create new key file.");

            else if (Master.OpenEntries(new(), $"res://Docs/new_entries.key") == false)
                PostFeedback("Failed to create entry file.");

            else
            {
                PostFeedback(string.Empty);
                Visible = false;
            }
        }
    }

    private void PostFeedback(string text) => Feedback.Text = FeedbackFormat.Replace("{text}", text);
}