using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TeuJson;

namespace FortLauncher;

public partial class Launcher 
{
    public bool Mods_popup;
    private StringBuilder urlBuilder = new();
    private List<ModsData> Mods_modData;

    public void WidgetMods() 
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(40, 40));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(Width - 80, Height - 80));
        ImGui.Begin("Mods List", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);
        ImGui.BeginChild("mod-list-child", new System.Numerics.Vector2(Width - 130, Height - 185));

        if (Mods_modData != null)
        for (int i = 0; i < Mods_modData.Count; i++) 
        {
            var data = Mods_modData[i];
            ImGui.BeginChild(data.Name + "_MODS", new System.Numerics.Vector2(210, 250));
            if (ImGui.ImageButton(data.Name + "__IMAGE", data.TexturePtr, new System.Numerics.Vector2(200, 150))) 
            {
                OpenUrl(data.Url);
            }
            ImGui.Text(data.Name);
            ImGui.Text(data.Submitter);
            ImGui.Text(data.Description);
            ImGui.Text($"{data.Downloads} Downloads");
            ImGui.BeginDisabled(SelectedClient == null);
            if (ImGui.Button("Download")) 
            {
                OpenUrl(data.DownloadUrl);
            }
            ImGui.SameLine();
            if (ImGui.Button("View")) 
            {
                OpenUrl(data.Url);
            }
            ImGui.EndDisabled();
            ImGui.EndChild();
            if (i % 3 != 3)
                ImGui.SameLine();
        }

        ImGui.EndChild();
        ImGui.Separator();
        if (ImGui.Button("More Mods")) 
        {
            OpenUrl("https://gamebanana.com/games/18654");
        }
        ImGui.SameLine();
        if (ImGui.Button("Refresh")) 
        {
            Task.Run(() => GetMods());
        }
        ImGui.SameLine();
        if (ImGui.Button("Back")) 
        {
            Mods_popup = false;
        }
        ImGui.End();
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
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
            urlBuilder.Append("https://api.gamebanana.com/Core/Item/Data?");
            for (int i = 0; i < arrayOfModsID.Count; i++)
            {
                var mod = arrayOfModsID[i].AsJsonArray;
                AddMod(mod, i);
            }
            var results = await httpClient.GetStringAsync(urlBuilder.ToString());
            var arrayOfMods = JsonTextReader.FromText(results).AsJsonArray;
            foreach (JsonArray result in arrayOfMods.AsParallel())
            {
                await AddToModList(httpClient, result);
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

    public void AddMod(JsonArray array, int index) 
    {
        urlBuilder.Append($"&itemtype[{index}]=Mod&itemid[{index}]={array[1].AsInt32}&fields[{index}]=name,Owner().name,description,downloads,Preview().sSubFeedImageUrl(),Url().sDownloadUrl()");
    }

    public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
    {
        //initialize a texture
        var texture = new Texture2D(device, width, height);

        //the array holds the color for each pixel in the texture
        Color[] data = new Color[width * height];
        for(var pixel = 0; pixel < data.Length; pixel++)
        {
            //the function applies the color according to the specified pixel
            data[pixel] = paint( pixel );
        }

        //set the color
        texture.SetData( data );

        return texture;
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