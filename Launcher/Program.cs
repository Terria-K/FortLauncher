using System.Runtime.InteropServices;

const int LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

if (Environment.OSVersion.Platform == PlatformID.Win32NT)
{
    try
    {
        NativeMethods.SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
        NativeMethods.AddDllDirectory(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            Environment.Is64BitProcess ? "x64" : "x86"
        ));
    }
    catch
    {
        // Pre-Windows 7, KB2533623 
        NativeMethods.SetDllDirectory(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            Environment.Is64BitProcess ? "x64" : "x86"
        ));
    }
}

using var game = new FortLauncher.Launcher();
game.Run();



public static class NativeMethods 
{

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetDefaultDllDirectories(int directoryFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern void AddDllDirectory(string lpPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetDllDirectory(string lpPathName);
}