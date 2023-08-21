using FortLauncher.ClientProcess;
using FortLauncher.Mods;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using TeuJson;

namespace FortLauncher;

public partial class Launcher 
{
    private List<ModsData> Mods_modData;
    private HashSet<string> Mods_blacklistdata;
    private string[] Mods_allMods;

    public void WidgetMods() 
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(40, 40));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(Width - 80, Height - 80));
        ImGui.Begin("Mods List", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);
        if (ImGui.BeginTabBar("Tab")) 
        {
            if (ImGui.BeginTabItem("Installed Mods")) 
            {
                ImGui.BeginChild("installed-mod-child", new System.Numerics.Vector2(Width - 130, Height - 185));
                if (SelectedClient == null) 
                {
                    ImGui.Text("Client hasn't been selected");
                }
                else 
                {
                    if (Mods_allMods.Length == 0)
                        ImGui.Text("No Mods Installed");
                    else 
                    {
                        ImGui.SetCursorPosX(380);
                        if (ImGui.Button("Enable All")) 
                        {
                            var cloned = Mods_blacklistdata.ToArray();
                            foreach (var mod in cloned) 
                            {
                                ModManager.RemoveToBlacklist(SelectedClient, mod, Mods_blacklistdata);
                            }
                        }
                        ImGui.SetCursorPosX(377);
                        if (ImGui.Button("Disable All")) 
                        {
                            foreach (var mod in Mods_allMods) 
                            {
                                ModManager.AddToBlacklist(SelectedClient, mod, Mods_blacklistdata);
                            }
                        }
                        foreach (var mod in Mods_allMods) 
                        {
                            WidgetModButton(mod);
                        }
                    }
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Mod List")) 
            {
                ImGui.BeginChild("mod-list-child", new System.Numerics.Vector2(Width - 130, Height - 185));

                if (Mods_modData != null)
                for (int i = 0; i < Mods_modData.Count; i++) 
                {
                    var data = Mods_modData[i];
                    ImGui.BeginChild(data.Name + "_MODS", new System.Numerics.Vector2(210, 250));
                    if (ImGui.ImageButton(data.Name + "__IMAGE", data.TexturePtr, new System.Numerics.Vector2(200, 150))) 
                    {
                        ProcessManager.OpenUrl(data.Url);
                    }
                    ImGui.Text(data.Name);
                    ImGui.Text(data.Submitter);
                    ImGui.Text(data.Description);
                    ImGui.Text($"{data.Downloads} Downloads");
                    ImGui.BeginDisabled(SelectedClient == null);
                    if (ImGui.Button("Download")) 
                    {
                        ProcessManager.OpenUrl(data.DownloadUrl);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("View")) 
                    {
                        ProcessManager.OpenUrl(data.Url);
                    }
                    ImGui.EndDisabled();
                    ImGui.EndChild();
                    if (i % 4 != 3)
                        ImGui.SameLine();
                }

                ImGui.EndChild();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        if (ImGui.Button("More Mods")) 
        {
            ProcessManager.OpenUrl("https://gamebanana.com/games/18654");
        }
        ImGui.SameLine();
        if (ImGui.Button("Refresh")) 
        {
            Task.Run(() => GetMods());
        }
        ImGui.SameLine();
        if (ImGui.Button("Back")) 
        {
            State = LauncherState.Main;
        }
        ImGui.End();
    }

    private void WidgetModButton(string mod) 
    {
        bool isWhitelisted = !ModManager.IsBlacklisted(mod, Mods_blacklistdata);
        ImGui.BeginChild(mod + "_Mod", new System.Numerics.Vector2(800, 30));
        ImGui.SetCursorPosX(200);
        ImGui.SetCursorPosY(10);
        ImGui.Text(mod);
        ImGui.SameLine();

        ImGui.SetCursorPosX(600);
        if (ImGui.Checkbox("", ref isWhitelisted)) 
        {
            ModManager.ToggleBlacklist(SelectedClient, mod, Mods_blacklistdata);
        }
        ImGui.EndChild();
    }

    public async Task NoRefreshMods() 
    {
        try 
        {
            if (Mods_modData == null)
                await GetMods();
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);
        }
    }

    public async Task GetMods() 
    {
        try 
        {
            Mods_modData = new();
            var httpClient = new HttpClient();
            var json = await httpClient.GetStringAsync("https://api.gamebanana.com/Core/List/New?itemtype=Mod&gameid=18654&page=1");

            var arrayOfModsID = JsonTextReader.FromText(json).AsJsonArray;
            foreach (JsonArray mod in arrayOfModsID.AsParallel())
            {
                var result = await httpClient.GetStringAsync(
                    $"https://api.gamebanana.com/Core/Item/Data?itemtype={mod[0]}&itemid={mod[1]}&fields=name,Owner().name,description,downloads,Preview().sSubFeedImageUrl(),Url().sDownloadUrl()");
                var jsonArray = JsonTextReader.FromText(result).AsJsonArray;
                await AddToModList(httpClient, jsonArray);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    private async Task AddToModList(HttpClient httpClient, JsonArray result)
    {
        var name = result[0].AsString;
        var submitter = result[1].AsString;
        var description = result[2].AsString;
        var downloads = result[3].AsInt32;
        var imageUrl = result[4].AsString.Replace("\\/", "/");
        var downloadUrl = result[5].AsString;

        Stream imageStream;
        var fileImage = imageUrl.Replace("/", "_").Replace(":", "").Replace("?", "");
        if (File.Exists("cache/" + fileImage + ".png"))
        {
            imageStream = File.OpenRead("cache/" + fileImage + ".png");
            Console.WriteLine("Loaded Cache");
        }
        else
        {
            var bytes = await httpClient.GetByteArrayAsync(imageUrl);
            imageStream = new MemoryStream(bytes);
            if (!Directory.Exists("cache"))
                Directory.CreateDirectory("cache");

            using (var file = File.Create("cache/" + fileImage + ".png"))
                imageStream.CopyTo(file);
        }
        var tex2D = Texture2D.FromStream(GraphicsDevice, imageStream);
        var texturePtr = renderer.BindTexture(tex2D);
        imageStream.Dispose();

        var modData = new ModsData()
        {
            Name = name,
            Submitter = submitter,
            Description = description,
            Downloads = downloads,
            Url = downloadUrl.Replace("\\/", "/").Replace("download/", ""),
            Image = imageUrl,
            DownloadUrl = downloadUrl.Replace("\\/", "/"),
            TexturePtr = texturePtr
        };

        Mods_modData.Add(modData);
    }
}


public class ModsData 
{
    public string Name;
    public string Submitter;
    public string Description;
    public int Downloads;
    public string Image;
    public string DownloadUrl;
    

    public string Url;

    public Texture2D Texture;
    public nint TexturePtr;
}