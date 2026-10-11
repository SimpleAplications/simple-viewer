using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace ReiksViewer;

/// <summary>The actions whose key the user can choose in Options.</summary>
public enum KeyAction
{
    PlayPause,
    NextFile,
    PreviousFile,
    PlayForward,
    PlayBackward,
    QuickHide,
}

/// <summary>
/// The user's options: chosen keys, window colour and playback choices. Stored in
/// %APPDATA%\ReiksViewer\settings.json. Nothing about opened files is ever written.
/// </summary>
public sealed class Settings
{
    public Dictionary<KeyAction, Key> Keys { get; set; } = DefaultKeys();
    public string WindowColor { get; set; } = "#000000";

    /// <summary>Two-letter language code, or empty to follow Windows.</summary>
    public string Language { get; set; } = "";

    /// <summary>Open the next file when a video ends, or after a few seconds on a picture.</summary>
    public bool PlayFolder { get; set; }
    /// <summary>With <see cref="PlayFolder"/>: pick the next file at random.</summary>
    public bool Shuffle { get; set; }
    /// <summary>With <see cref="PlayFolder"/>: start over after the last file.</summary>
    public bool RepeatFolder { get; set; }
    /// <summary>Play the current video again when it ends, and stay on the current file.</summary>
    public bool RepeatFile { get; set; }

    /// <summary>Leave the window out of screenshots, recordings and screen sharing.</summary>
    public bool HideFromCapture { get; set; }
    /// <summary>Show "Reiks Viewer" instead of the file's name in the title bar and taskbar.</summary>
    public bool HideFileNames { get; set; }

    public static Dictionary<KeyAction, Key> DefaultKeys() => new()
    {
        [KeyAction.PlayPause] = Key.Space,
        [KeyAction.NextFile] = Key.V,
        [KeyAction.PreviousFile] = Key.C,
        [KeyAction.PlayForward] = Key.X,
        [KeyAction.PlayBackward] = Key.Z,
        [KeyAction.QuickHide] = Key.H,
    };

    static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ReiksViewer", "settings.json");

    // Where the options were kept when the app was still called Simple; read once, never written.
    static string LegacyFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Simple", "settings.json");

    public static Settings Load()
    {
        Settings? settings = null;
        try
        {
            string path = File.Exists(FilePath) ? FilePath : LegacyFilePath;
            if (File.Exists(path)) settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Unreadable or damaged file: start from the defaults.
        }
        settings ??= new Settings();
        settings.Keys ??= new Dictionary<KeyAction, Key>();
        // A file from an older version may lack actions added since.
        foreach (var (action, key) in DefaultKeys()) settings.Keys.TryAdd(action, key);
        settings.WindowColor ??= "#000000";
        settings.Language ??= "";
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The options still apply for this session; they just won't be remembered.
        }
    }
}
