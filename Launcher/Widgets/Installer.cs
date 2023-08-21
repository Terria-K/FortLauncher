using System.Net;
using System.Runtime.InteropServices;
using ImGuiNET;
using Ionic.Zip;
using TeuJson;

namespace FortLauncher;

public partial class Launcher 
{
    private bool refreshed = true;
    private VersionTags currentTag;
    private List<VersionTags> tags;
    private string selectedInstallerVersion = "";
    private bool isDownloading;
    private CancellationTokenSource source;
    private StringProgress progress;

    public HashSet<string> Tags = new();

    public void WidgetInstaller() 
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(Width - 250, 150));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, 305));
        ImGui.Begin("Installer", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);
        ImGui.SetCursorPosX((250 - ImGui.CalcTextSize("Installers").X) * 0.5f);
        ImGui.Text("Installers");
        ImGui.Separator();

        ImGui.BeginChild("downloaded-installer-child", new System.Numerics.Vector2(230, 210));
        bool deletedSomething = false;

        foreach (var tag in Tags) 
        {
            ImGui.PushID(tag);
            if (ImGui.Selectable(tag))
            {
                selectedInstallerVersion = tag;
                Data.CurrentInstaller = selectedInstallerVersion;
                Save();
            }
            ImGui.PopID();
            if (ImGui.BeginPopupContextItem(tag)) 
            {
                if (ImGui.MenuItem("Delete")) 
                {
                    Directory.Delete($"installer/{tag}", true);
                    deletedSomething = true;
                    if (tag == selectedInstallerVersion)
                        selectedInstallerVersion = "";
                }
                
                ImGui.EndPopup();
            }
        }
        if (deletedSomething) 
        {
            FetchDownloadedVersions();
            Save();
        }

        ImGui.EndChild();

        const int posX = 220;

        ImGui.SetCursorPosX((250 - 300 * 0.5f));
        ImGui.SetCursorPosY(250);
        ImGui.Separator();
        ImGui.SetCursorPosX((posX - 300 * 0.5f));
        ImGui.SetCursorPosY(260);
        if (ImGui.Button("+ Add")) 
        {
            State = LauncherState.Installer;
        }

        ImGui.SetCursorPosX((posX + 50 - 300 * 0.5f));
        ImGui.SetCursorPosY(260);
        if (ImGui.Button("Refresh")) 
        {
            FetchDownloadedVersions();
        }
        ImGui.Text("Selected: " + selectedInstallerVersion);
        ImGui.End();
    }

    public void WidgetInstallerPopup() 
    {
        if (refreshed)
        {
            currentTag = null;
            if (tags != null) 
            {
                tags.Clear();
                tags = null;
            }
            Task.Run(async () => {
                var tags = await GetVersions();
                var tag = tags
                .Select(x => {
                    x.InitName();
                    return x;
                })
                .Where(x => x.Version >= new Version(4, 0, 0))
                .ToList();
                tag.Reverse();
                this.tags = tag;
            });
            refreshed = false;
        }

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(60, 60));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(Width - 120, Height - 120));
        ImGui.Begin("Installers", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

        ImGui.BeginChild("Installer-child", new System.Numerics.Vector2(Width - 130, Height - 185));

        if (tags != null) 
        {
            foreach (var tag in tags) 
            {
                if (ImGui.Selectable(tag.Name))
                {
                    currentTag = tag;
                }
            }
        }
        ImGui.EndChild();
        ImGui.Separator();

        ImGui.BeginDisabled(currentTag == null || Tags.Contains(currentTag.Name));
        if (ImGui.Button("Download")) 
        {
            source = new CancellationTokenSource();
            Task.Run(() => DownloadVersion(currentTag, source.Token), source.Token);
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(currentTag == null || !Tags.Contains(currentTag.Name));
        if (ImGui.Button("Delete")) 
        {
            Directory.Delete($"installer/{currentTag.Name}", true);
            FetchDownloadedVersions();
            if (currentTag.Name == selectedInstallerVersion)
                selectedInstallerVersion = "";
        }
        ImGui.EndDisabled();
        ImGui.SameLine();

        if (ImGui.Button("Refresh")) 
        {
            refreshed = true;
        }
        ImGui.SameLine();

        if (ImGui.Button("Back")) 
        {
            State = LauncherState.Main;
        }

        ImGui.End();

        if (isDownloading) 
        {
            ImGui.OpenPopup("Download");
            WidgetDownloadingPopup();
        }
    }

    private void WidgetDownloadingPopup() 
    {
        const float DownloadWidth = 200;
        const float DownloadHeight = 120;
        ImGui.SetNextWindowPos(new System.Numerics.Vector2((Width * 0.5f) - (DownloadWidth / 2), (Height * 0.5f) - (DownloadHeight / 2)));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(DownloadWidth, DownloadHeight));
        ImGui.BeginPopupModal("Download", ref isDownloading, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

        ImGui.SetCursorPosX((DownloadWidth * 0.5f) - (ImGui.CalcTextSize($"Downloading {currentTag.Name}...").X) / 2);
        ImGui.Text($"Downloading {currentTag.Name}...");
        ImGui.SetCursorPosX((DownloadWidth * 0.5f) - (ImGui.CalcTextSize($"{progress.Bytes}").X) / 2);
        ImGui.Text($"{progress.Bytes}");
        ImGui.SetCursorPosX((DownloadWidth * 0.37f));
        ImGui.SetCursorPosY(90);
        if (ImGui.Button("Cancel")) 
        {
            source.Cancel();
        }

        ImGui.End();
    }

    private async Task DownloadVersion(VersionTags tag, CancellationToken token) 
    {
        progress = new StringProgress();
        progress.Reset();
        isDownloading = true;
        try 
        {
            string tagZip;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                tagZip = "NoANSI";
            else
                tagZip = "OSXLinux";
            var zipName = $"FortRise.Installer.v{tag.Name}-{tagZip}.zip";
            var url = "https://github.com/Terria-K/FortRise/releases/download/" + tag.Name + $"/{zipName}";
            using var memStream = new MemoryStream();
            var httpClient = new HttpClient();
            await httpClient.DownloadAsync(url, memStream, progress, token);
            var bytes = memStream.ToArray();

            var installerDirectory = $"installer/{tag.Name}";

            if (!Directory.Exists(installerDirectory)) 
                Directory.CreateDirectory(installerDirectory);
            
            var filePath = Path.Combine(installerDirectory, $"v{tag.Name}.zip");
            
            await File.WriteAllBytesAsync(filePath, bytes);

            ExtractVersion(filePath, installerDirectory, zipName);
            selectedInstallerVersion = Data.CurrentInstaller = tag.Name;
        }
        catch (TaskCanceledException) 
        {
            Console.WriteLine("Task Cancelled!");
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);
#if DEBUG
            throw;
#endif
        }
        finally 
        {
            isDownloading = false;
            source.Dispose();
            FetchDownloadedVersions();
        }
    }

    private void ExtractVersion(string file, string to, string zipName) 
    {
        try 
        {
            using (var archive = ZipFile.Read(file)) 
            {
                foreach (var entry in archive.Entries) 
                {
                    if (entry.IsDirectory)
                        continue;
                    
                    using var memStream = new MemoryStream();
                    entry.Extract(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);

                    var index = entry.FileName.IndexOf("/");
                    var filename = Path.Combine(to, entry.FileName.Substring(index + 1));
                    var path = Path.GetDirectoryName(filename);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    File.WriteAllBytes(filename, memStream.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);
        }
        finally 
        {
            File.Delete(file);
        }
    }

    public void FetchDownloadedVersions() 
    {
        Tags.Clear();
        var installerDirectory = "installer";

        if (!Directory.Exists(installerDirectory))  
        {
            Directory.CreateDirectory(installerDirectory);
            return;
        }

        var directories = Directory.GetDirectories(installerDirectory);
        foreach (var dir in directories) 
        {
            var installerFile = Path.Combine(dir, "Installer.NoANSI.exe");
            if (File.Exists(installerFile))
                Tags.Add(Path.GetFileName(dir));
        }
        Tags = Tags.Reverse().ToHashSet();
    }

    private async Task<List<VersionTags>> GetVersions() 
    {
        try 
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var mediaType = new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github+json");

            var product = new System.Net.Http.Headers.ProductInfoHeaderValue("FortLauncher", "1.0");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(mediaType);
            httpClient.DefaultRequestHeaders.UserAgent.Add(product);
            var result = await httpClient.GetAsync("https://api.github.com/repos/Terria-K/FortRise/git/refs/tags");
            var json = await result.Content.ReadAsStringAsync();
            var jsonVersionTags = JsonTextReader.FromText(json);
            var versionTags = jsonVersionTags.ConvertToList<VersionTags>();
            return versionTags;
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);
#if DEBUG
            throw;
#else
            return new List<VersionTags>();
#endif
        }
    }
}