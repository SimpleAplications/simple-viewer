using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ReiksViewer;

/// <summary>
/// Whether this copy was installed from the Microsoft Store, and that package's names. A Store
/// copy sits in a folder nobody may change, is updated by the Store, and keeps what it writes
/// in a data folder of its own.
/// </summary>
static class StorePackage
{
    /// <summary>The package's family name, or null when the app was installed some other way.</summary>
    internal static readonly string? FamilyName = Read();

    internal static bool Installed => FamilyName != null;

    /// <summary>The package's own data folder, which the user can open and change.</summary>
    internal static string DataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", FamilyName ?? "", "LocalState");

    /// <summary>How Windows names the app: the package, then the app's id in its manifest.</summary>
    internal static string AppUserModelId => FamilyName + "!ReiksViewer";

    static string? Read()
    {
        const int InsufficientBuffer = 122;
        int length = 0;
        if (GetCurrentPackageFamilyName(ref length, null) != InsufficientBuffer) return null;
        var name = new StringBuilder(length);
        if (GetCurrentPackageFamilyName(ref length, name) != 0) return null;
        length = 0;
        if (GetCurrentPackagePath(ref length, null) != InsufficientBuffer) return null;
        var path = new StringBuilder(length);
        if (GetCurrentPackagePath(ref length, path) != 0) return null;
        // Only the package this program itself came in counts, not one it was started from.
        string Folder(string folder) => Path.GetFullPath(folder).TrimEnd('\\');
        return string.Equals(Folder(AppContext.BaseDirectory), Folder(path.ToString()), StringComparison.OrdinalIgnoreCase)
            ? name.ToString()
            : null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackageFamilyName(ref int length, StringBuilder? name);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackagePath(ref int length, StringBuilder? path);
}
