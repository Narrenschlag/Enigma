namespace Passwords
{
    using Cutulu.Core;
    using System;
    using Godot;

    public partial class Master : Node
    {
        [ExportGroup("Login")]
        [Export] public LineEdit LoginPassword;
        [Export] public Button LoginButton;

        [ExportGroup("Setup")]
        [Export] public LineEdit SetupPassword;
        [Export] public Button SetupHideButton;
        [Export] public LineEdit SetupPasswordConfirm;
        [Export] public Button SetupButton;

        [ExportGroup("Entries")]
        [Export] public Button BackupPaste;
        [Export] public Button BackupLoad;
        [Export] public Node EntryPanel;
        [Export] public LineEdit EntryId;
        [Export] public LineEdit EntryPassword;
        [Export] public Button EntryAddButton;
        [Export] public Node EntryRoot;
        [Export] public PackedScene EntryPrefab;

        public const string PasswordPath = $"{CONST.USER_PATH}sesam.key";
        public const string FilePath = $"{CONST.USER_PATH}sesam.file";

        public static Entries Entries { get; set; }
        private static Master Singleton;

        public override void _Ready()
        {
            Singleton = this;

            // Login
            if (PasswordPath.PathExists())
            {
                LoginButton.Connect("pressed", new Callable(this, "OnLogin"));

                Enable(1);
                LoginPassword.GrabFocus();
            }

            // Setup
            else
            {
                SetupHideButton.Connect("pressed", new(this, "OnSetupHide"));
                SetupButton.Connect("pressed", new(this, "OnSetup"));

                Enable(0);
                SetupPassword.GrabFocus();
            }
        }

        private void Enable(byte mode)
        {
            SetupButton.GetParent().SetActive(mode == 0);
            LoginButton.GetParent().SetActive(mode == 1);
            EntryPanel.SetActive(mode == 2);
        }

        private void OnLogin()
        {
            try
            {
                Password.RawPassword = LoginPassword.Text.Trim();

                Entries = Entries.Read(LoginPassword.Text.Trim());
                OnUnlock();
            }

            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        private void OnSetup()
        {
            try
            {
                if (SetupPassword.Text.Trim().Equals(SetupPasswordConfirm.Text.Trim()) == false)
                {
                    throw new("Passwords are not matching");
                }

                Password.RawPassword = SetupPassword.Text.Trim();
                Password.Set(SetupPassword.Text.Trim());

                Entries = new();
                Entries.Write();

                OnUnlock();
            }

            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        private void OnSetupHide() => SetupPassword.Secret = !SetupPassword.Secret;

        private void OnAddEntry()
        {
            string password = EntryPassword.Text.Trim();
            string id = EntryId.Text.Trim();

            if (id.IsEmpty() || password.IsEmpty()) return;

            Entries.EntryIndex = Entries.EntryIndex.AddToArray(new(id, password));
            EntryPassword.Text = "";
            EntryId.Text = "";

            Entries.Write();
            UpdateEntries();
        }

        private void OnUnlock()
        {
            EntryAddButton.Connect("pressed", new(this, "OnAddEntry"));
            BackupPaste.Connect("pressed", new(this, "OnBackupPaste"));
            BackupLoad.Connect("pressed", new(this, "OnBackupLoad"));

            UpdateEntries();
        }

        public static void UpdateEntries()
        {
            Singleton.Enable(2);

            Singleton.EntryRoot.Clear();
            for (ushort i = 0; i < Entries.EntryIndex.Length; i++)
            {
                var entry = Singleton.EntryPrefab.Instantiate<Entry>(Singleton.EntryRoot);
                entry.OnSetup(i, ref Entries.EntryIndex[i]);
            }
        }

        private void OnBackupPaste() => Entries.PasteBackup();
        private void OnBackupLoad() => Entries.CopyBackup();
    }

    public static class Password
    {
        private static string hashedPassword;

        public static string RawPassword { private get; set; }
        private static string HashedPassword
        {
            set => hashedPassword = value;
            get => hashedPassword.IsEmpty() ? hashedPassword = new File(Master.PasswordPath).Read().TryDecode(out string hp) ? hp : default : hashedPassword;
        }

        public static void Set(string rawPassword) => new File(Master.PasswordPath).Write((HashedPassword = rawPassword.HashPassword()).Encode());
        public static bool Check(string rawPassword) => HashedPassword.Equals(rawPassword.HashPassword());

        public static string Encrypt(string input) => input.EncryptString(RawPassword, 3).EncryptString(RawPassword);
        public static string Decrypt(string input) => input.DecryptString(RawPassword).DecryptString(RawPassword, 3);
    }

    public class Entries
    {
        public Entry[] EntryIndex { get; set; }

        public Entries() => EntryIndex = new Entry[1] { new("sampleId", "samplePassword") };
        private Entries(int length) => EntryIndex = new Entry[length];

        /// <summary>
        /// Read entries from file system
        /// </summary>
        public static Entries Read(string rawPassword)
        {
            if (Password.Check(rawPassword))
            {
                try
                {
                    var encrypted = new File(Master.FilePath).ReadString();
                    var decrypted = Password.Decrypt(encrypted);

                    return decrypted.json<Entries>();
                }
                catch
                {
                    throw new("No file found");
                }
            }

            else throw new("Password is not matching");
        }

        /// <summary>
        /// Write entries to file system
        /// </summary>
        public void Write()
        {
            string json = this.json();

            string encrypted = Password.Encrypt(json);

            new File(Master.FilePath).WriteString(encrypted);
        }

        public void CopyBackup() => Application.Clipboard = EntryIndex.json();

        public void PasteBackup()
        {
            var paste = Application.Clipboard.json<Entry[]>();
            if (paste == null) return;

            var backup = EntryIndex;
            EntryIndex = new Entry[backup.Length + paste.Length];

            for (int i = 0; i < EntryIndex.Length; i++)
            {
                EntryIndex[i] = i >= backup.Length ? paste[i - backup.Length] : backup[i];
            }

            Write();
            Master.UpdateEntries();
        }

        public struct Entry
        {
            public string Id { get; set; }
            public string Password { get; set; }

            public Entry(string id, string password)
            {
                Id = id;
                Password = password;
            }
        }
    }
}