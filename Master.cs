using System;
using Cutulu;
using Godot;

namespace Passwords
{
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
        [Export] public Node EntryPanel;
        [Export] public LineEdit EntryId;
        [Export] public LineEdit EntryPassword;
        [Export] public Button EntryAddButton;
        [Export] public Node EntryRoot;
        [Export] public PackedScene EntryPrefab;

        public const string PasswordPath = $"{IO.USER_PATH}sesam.key";
        public const string FilePath = $"{IO.USER_PATH}sesam.file";

        public static Entries Entries { get; set; }
        private static Master Singleton;

        public override void _Ready()
        {
            Singleton = this;

            // Login
            if (PasswordPath.Exists())
            {
                LoginButton.ConnectButton(this, "OnLogin");

                Enable(1);
                LoginPassword.GrabFocus();
            }

            // Setup
            else
            {
                SetupHideButton.ConnectButton(this, "OnSetupHide");
                SetupButton.ConnectButton(this, "OnSetup");

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
            EntryAddButton.ConnectButton(this, "OnAddEntry");

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
    }

    public static class Password
    {
        private static string hashedPassword;

        public static string RawPassword { private get; set; }
        private static string HashedPassword
        {
            set => hashedPassword = value;
            get => hashedPassword.IsEmpty() ? hashedPassword = IO.Read<string>(Master.PasswordPath, IO.FileType.Binary) : hashedPassword;
        }

        public static void Set(string rawPassword) => IO.Write(HashedPassword = rawPassword.HashPassword(), Master.PasswordPath, IO.FileType.Binary);
        public static bool Check(string rawPassword) => HashedPassword.Equals(rawPassword.HashPassword());

        public static string Encrypt(string input) => input.EncryptString(RawPassword).EncryptString(HashedPassword);
        public static string Decrypt(string input) => input.DecryptString(HashedPassword).DecryptString(RawPassword);
    }

    public class Entries
    {
        public Entry[] EntryIndex { get; set; }

        public Entries() => EntryIndex = new Entry[1] { new("sampleId", "samplePassword") };

        /// <summary>
        /// Read entries from file system
        /// </summary>
        public static Entries Read(string rawPassword)
        {
            if (Password.Check(rawPassword))
            {
                try
                {
                    var encrypted = IO.ReadString(Master.FilePath);
                    Debug.Log(encrypted);

                    var decrypted = Password.Decrypt(encrypted);
                    Debug.Log(decrypted);

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

            IO.WriteString(Master.FilePath, encrypted);
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