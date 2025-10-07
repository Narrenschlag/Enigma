namespace Enigma;

using Godot;

[GlobalClass]
public partial class HiddenInput : HBoxContainer
{
    [Export] private bool HiddenByDefault { get; set; } = true;
    [Export] private string PlaceholderText { get; set; } = "Enter password";

    [Export] public LineEdit Edit { get; set; }
    [Export] public Button HideButton { get; set; }

    public string Text => Edit?.Text ?? string.Empty;

    public override void _EnterTree()
    {
        Edit.TextChanged += OnTextChanged;
        HideButton.Pressed += OnHide;

        Edit.Secret = HiddenByDefault;
    }

    public override void _ExitTree()
    {
        Edit.TextChanged -= OnTextChanged;
        HideButton.Pressed -= OnHide;
    }

    public virtual void OnTextChanged(string _) { }
    private void OnHide() => Edit.Secret = !Edit.Secret;
}