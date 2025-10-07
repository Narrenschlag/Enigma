namespace Enigma;

using System.Collections.Generic;
using Cutulu.Encryption;
using Cutulu.Core;
using Godot;

public partial class Master : Node
{
    [ExportGroup("Entries")]
    [Export] public Button BackupPaste;
    [Export] public Button BackupLoad;
    [Export] public Control EntryPanel;
    [Export] public LineEdit EntryId;
    [Export] public LineEdit EntryPassword;
    [Export] public Node EntryRoot;
    [Export] public PackedScene EntryPrefab;

    private static byte[] ENCRYPTED_KEY { get; set; }
    private static byte[] HIDDEN_KEY { get; set; }

    public static string LOADED_FILE_PATH { get; set; }
    private static Master Singleton { get; set; }

    public static Entries Entries { get; set; }

    public override void _EnterTree()
    {
        Singleton = this;
    }

    public override void _Process(double delta)
    {
        if (EntryId.HasFocus() || EntryPassword.HasFocus())
        {
            if (Input.IsActionJustPressed("submit")) OnAddEntry();
        }

        if (Input.IsActionJustPressed("hide"))
        {
            var entries = EntryRoot.GetNodesInChildren<Entry>();

            if (entries.NotNull())
                foreach (var entry in entries)
                    entry.HideEntry();
        }
    }

    private static byte[] FixPassword(string password)
    {
        var buffer = new List<byte>(password.Encode());

        var random = Noisef.GenerateNoise(buffer.ToArray().Decode<int>(), default);
        var steps = random.Value(buffer[^1]);
        byte step = 0;

        while (buffer.Count < SmartEncryption.KeySize)
        {
            var val = random.Value(
                buffer[(++step + buffer[^2]) % buffer.Count] * 603.7f,
                buffer[(++step + buffer[3]) % buffer.Count] * 0.2f,
                buffer[(++step + buffer[^3]) % buffer.Count] * Mathf.Pi
            );

            if ((steps += steps * random.Value(++step)) % 2 == 0)
                val *= random.Value(steps);

            if (buffer[++step % buffer.Count] % 2.0f == 0) buffer.Insert(Mathf.RoundToInt(steps * 10) % (buffer.Count - 4), (byte)Mathf.RoundToInt(val * 255));
            else buffer.Add((byte)Mathf.RoundToInt(val * 255));
        }

        return [.. buffer];
    }

    public static bool LoadKey(File file, string password)
    {
        if (file.IsNull() || file.Exists() == false || password.IsEmpty()) return false;

        ENCRYPTED_KEY = file.Read();

        try
        {
            var priv = ENCRYPTED_KEY.Decrypt(HIDDEN_KEY = FixPassword(password));
        }

        catch
        {
            return false;
        }

        return true;
    }

    public static bool WriteKey(string path, byte[] privateKey, string password)
    {
        if (privateKey.IsEmpty()) return false;

        new File(path).Write(ENCRYPTED_KEY = privateKey.Encrypt(FixPassword(password)));
        return LoadKey(new File(path), password);
    }

    public static bool UnlockEntries(File file)
    {
        if (file.IsNull() || file.Exists() == false) return false;

        var entries = Entries.Read(file, HIDDEN_KEY);
        if (entries.IsNull()) return false;
        else return OpenEntries(entries, file.SystemPath);
    }

    public static bool OpenEntries(Entries entries, string filePath)
    {
        if (entries.IsNull()) return false;

        LOADED_FILE_PATH = filePath;
        Entries = entries;

        Singleton.EntryPanel.Visible = true;
        UpdateEntries();
        return true;
    }

    private void OnAddEntry()
    {
        string password = EntryPassword.Text.Trim();
        string id = EntryId.Text.Trim();

        if (id.IsEmpty() || password.IsEmpty()) return;

        Entries.EntryIndex = Entries.EntryIndex.AddToArray(new(id, password));
        EntryPassword.Text = "";
        EntryId.Text = "";

        Save();
        UpdateEntries();
    }

    public static void Save()
    {
        Entries.Write(HIDDEN_KEY);
    }

    public static void UpdateEntries()
    {
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

public class Entries
{
    public Entry[] EntryIndex { get; set; }

    public Entries() => EntryIndex = [new("sampleId", "samplePassword")];

    /// <summary>
    /// Read entries from file system
    /// </summary>
    public static Entries Read(File file, byte[] key)
    {
        try
        {
            return file.Read()
                .Decrypt(key)
                .Decode<Entries>();
        }

        catch
        {
            Debug.LogError("No file found");

            return null;
        }
    }

    /// <summary>
    /// Write entries to file system
    /// </summary>
    public void Write(byte[] key)
    {
        new File(Master.LOADED_FILE_PATH).Write(this.Encode().Encrypt(key));
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

        Master.UpdateEntries();
        Master.Save();
    }

    public struct Entry(string id, string password)
    {
        public Entry() : this(string.Empty, string.Empty) { }

        public string Id { get; set; } = id;
        public string Password { get; set; } = password;
    }
}