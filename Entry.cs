using Cutulu;
using Godot;

namespace Passwords
{
    public partial class Entry : HBoxContainer
    {
        [Export] public LineEdit Id { get; set; }
        [Export] public LineEdit Password { get; set; }
        [Export] public Button CopyButton { get; set; }
        [Export] public Button HideButton { get; set; }
        [Export] public Button DeleteButton { get; set; }

        public ushort Index { get; private set; }
        private string ogId, ogPassword;

        public void OnSetup(ushort i, ref Entries.Entry entry)
        {
            Index = i;

            Id.Text = ogId = entry.Id;
            Password.Text = ogPassword = entry.Password;

            Id.Connect("focus_exited", this, "OnUpdate");
            Id.Connect("text_submitted", this, "OnUpdate");

            Password.Connect("focus_exited", this, "OnUpdate");
            Password.Connect("text_submitted", this, "OnUpdate");

            CopyButton.ConnectButton(this, "OnCopy");
            HideButton.ConnectButton(this, "OnHide");
            DeleteButton.ConnectButton(this, "OnDelete");

            Password.Secret = true;
        }

        private void OnUpdate(string _) => OnUpdate();
        private void OnUpdate()
        {
            Id.Text = Id.Text.Trim();
            Password.Text = Password.Text.Trim();

            // Changed
            if (ogId.Equals(Id.Text) != ogPassword.Equals(Password.Text))
            {
                Master.Entries.EntryIndex[Index] = new(Id.Text, Password.Text);
                Master.Entries.Write();

                Debug.Log("Updated...");
            }
        }

        private void OnCopy() => Application.Clipboard = Password.Text.Trim();

        private void OnHide() => Password.Secret = !Password.Secret;

        private void OnDelete()
        {
            Master.Entries.EntryIndex = Master.Entries.EntryIndex.RemoveAt(Index);
            Master.Entries.Write();
            Master.UpdateEntries();
        }
    }
}