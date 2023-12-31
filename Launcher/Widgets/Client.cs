using System.Diagnostics;
using ImGuiNET;
using TeuJson;
using TeuJson.Attributes;

namespace FortLauncher;

public partial class Launcher 
{
    private List<Client> clientToRemove = new();
    private HashSet<string> clientPaths = new();
    private string Client_hovered = "";
    private bool Client_renamePopup;
    private string Client_newRename = "";
    private Client currentClientEdit;
    public Client SelectedClient;

    public void WidgetClient() 
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 150));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, 305));
        ImGui.Begin("Clients", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);
        ImGui.SetCursorPosX((250 - ImGui.CalcTextSize("TowerFall Directories").X) * 0.5f);
        ImGui.Text("TowerFall Directories");

        ImGui.Separator();

        ImGui.BeginChild("Client-child", new System.Numerics.Vector2(230, 210));

        foreach (var client in Data.Clients) 
        {
            ImGui.PushID(client.Path);
            if (ImGui.Selectable(client.Name))
            {
                SelectedClient = client;
                Data.CurrentClientPath = SelectedClient.Path;
                Save();
            }
            ImGui.PopID();
            if (ImGui.BeginPopupContextItem(client.Path)) 
            {
                var str = client.ClientType.HasFlag(ClientType.FortRise) ? "Unpatch" : "Patch";
                if (ImGui.MenuItem(str))
                {
                    State = LauncherState.Patching;
                    Task.Run(() => Client_Patch(client));
                }
                if (ImGui.MenuItem("Rename"))
                {
                    Client_newRename = client.Name;
                    Client_renamePopup = true;
                    currentClientEdit = client;
                    Save();
                }
                if (ImGui.MenuItem("Remove")) 
                {
                    Client_Remove(client.Name);
                    Save();
                }
                
                ImGui.EndPopup();
            }
            if (ImGui.IsItemHovered()) 
                Client_hovered = client.CutPath;
            else Client_hovered = string.Empty;
        }

        Client_DoRemove();


        ImGui.EndChild();

        ImGui.SetCursorPosX((250 - 300 * 0.5f));
        ImGui.SetCursorPosY(250);
        ImGui.Separator();

        const int posX = 195;

        ImGui.SetCursorPosX(((posX) - 300 * 0.5f));
        ImGui.SetCursorPosY(260);
        if (ImGui.Button("+ Add")) 
        {
            Client_FolderPick();
        }
        ImGui.SetCursorPosX(((posX + 50) - 300 * 0.5f));
        ImGui.SetCursorPosY(260);
        ImGui.BeginDisabled((SelectedClient == null || string.IsNullOrEmpty(selectedInstallerVersion)));
        if (ImGui.Button("Patch")) 
        {
            State = LauncherState.Patching;
            Task.Run(() => Client_Patch(SelectedClient));
        }
        ImGui.EndDisabled();

        ImGui.SetCursorPosX(((posX + 100) - 300 * 0.5f));
        ImGui.SetCursorPosY(260);
        ImGui.BeginDisabled((SelectedClient == null || !SelectedClient.ClientType.HasFlag(ClientType.FortRise)) || string.IsNullOrEmpty(selectedInstallerVersion));
        if (ImGui.Button("Unpatch")) 
        {
            State = LauncherState.Patching;
            Task.Run(() => Client_Patch(SelectedClient));
        }
        ImGui.EndDisabled();
        ImGui.Text(Client_hovered);

        ImGui.End();
        if (Client_renamePopup)  
        {
            ImGui.OpenPopup("Rename");
            WidgetClient_Rename(currentClientEdit);
        }
    }

    private async Task Client_Patch(Client client) 
    {
        Client_consoleRunning = true;
        var process = new Process();
        try 
        {
            var isUnpatch = client.ClientType.HasFlag(ClientType.FortRise);
            var textUnpatch = isUnpatch ? "--unpatch" : "--patch";

            process.StartInfo.FileName = Path.GetFullPath($"installer/{selectedInstallerVersion}/Installer.NoAnsi.exe");
            process.StartInfo.Arguments = textUnpatch + " \"" + client.Path + "\"";
            process.StartInfo.WorkingDirectory = Path.GetFullPath($"installer/{selectedInstallerVersion}");
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += Console_Output;

            if (process.Start()) 
            {
                process.BeginOutputReadLine();
                await process.WaitForExitAsync();
                process.OutputDataReceived -= Console_Output;
                if (isUnpatch)
                    Client_RemoveFortRise(client);
                else
                    Client_AddFortRise(client);
                Client_consoleRunning = false;
            }
            Save();
        }
        catch (Exception ex)
        {
            consoleTexts += ex.ToString() + "\n";
            consoleTexts += ex.StackTrace + "\n";
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);
            Client_consoleRunning = false;
#if DEBUG
            throw;
#endif
        }
        finally 
        {
            process.Dispose();
        }
    }

    private void Console_Output(object sender, DataReceivedEventArgs e) 
    {
        consoleTexts += e.Data + "\n";
    }

    private string consoleTexts = "";
    private bool Client_consoleRunning;


    private void Client_RemoveFortRise(Client client) 
    {
        client.ClientType &= ~ClientType.FortRise;
        client.Name = client.Name.Replace(" (FortRise)", "");
    }

    private void Client_AddFortRise(Client client) 
    {
        client.ClientType |= ClientType.FortRise;
        client.Name += " (FortRise)";
    }

    private void WidgetPatch(Client client) 
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(60, 60));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(Width - 120, Height - 120));
        ImGui.Begin("Patching " + client.Name, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

        var flags = ImGuiInputTextFlags.ReadOnly;
        try 
        {
            ImGui.InputTextMultiline("##console-window", ref consoleTexts, 100, new System.Numerics.Vector2(Width - 140, Height - 180), flags);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);
        }

        ImGui.BeginDisabled(Client_consoleRunning);

        if (ImGui.Button("Close")) 
        {
            State = LauncherState.Main;
            consoleTexts = string.Empty;
        }
        ImGui.EndDisabled();

        ImGui.End();
    }

    public void WidgetClient_Rename(Client client) 
    {
        if (client == null)
            return;
        const float RenameWidth = 200;
        const float RenameHeight = 120;
        ImGui.SetNextWindowPos(new System.Numerics.Vector2((Width * 0.5f) - (RenameWidth / 2), (Height * 0.5f) - (RenameHeight / 2)));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(RenameWidth, RenameHeight));
        ImGui.BeginPopupModal("Rename", ref Client_renamePopup, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

        ImGui.InputText("Name ", ref Client_newRename, 50);
        if (ImGui.Button("OK")) 
        {
            Client_renamePopup = false;
            client.Name = Client_newRename;
            Client_newRename = string.Empty;
            currentClientEdit = null;
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) 
        {
            Client_renamePopup = false;
        }

        ImGui.EndPopup();
    }
    
    public void Client_Remove(string clientName) 
    {
        var client = Data.Clients.Where(x => x.Name == clientName).First();
        clientToRemove.Add(client);
        if (SelectedClient == client) 
        {
            SelectedClient = null;
            Data.CurrentClientPath = "";
        }
    }

    public void Client_DoRemove() 
    {
        foreach (var client in clientToRemove) 
        {
            Data.Clients.Remove(client);
            clientPaths.Remove(client.Path);
        }
        if (clientToRemove.Count > 0) 
        {
            clientToRemove.Clear();
            Save();
        }
    }

    public void Client_FolderPick()
    {
        var folderPicker = NativeFileDialogSharp.Dialog.FolderPicker();
        if (!folderPicker.IsOk)
            return;
        
        if (clientPaths.Contains(folderPicker.Path.Replace('\\', '/'))) 
            return;
        ReadOnlySpan<char> folderPath = folderPicker.Path;
        var towerFallPath = Path.Join(folderPath, "TowerFall.exe");
        if (!File.Exists(towerFallPath))
            return;

        var type = ClientType.None;
        var name = "TowerFall";

        var steamPath = Path.Join(folderPath, "Steamworks.NET.dll");
        if (File.Exists(steamPath))
        {
            name += " (Steam)";
            type |= ClientType.Steam;
        }

        var fortRisePath = Path.Join(folderPath, "TowerFall.FortRise.mm.dll");
        if (File.Exists(fortRisePath))
        {
            name += " (FortRise)";
            type |= ClientType.FortRise;
        }

        var folderPathStr = new string(folderPath);

        var newClient = new Client(name, folderPathStr, type);

        Data.Clients.Add(newClient);
        clientPaths.Add(folderPathStr.Replace('\\', '/'));
        Save();
        SelectedClient = newClient;
        Data.CurrentClientPath = newClient.Path;
    }
}

public partial class Client : ISerialize, IDeserialize
{
    [TeuObject]
    public string Name;
    [TeuObject]
    public string Path;
    [TeuObject]
    public ClientType ClientType;

    [Ignore]
    public string CutPath 
    {
        get
        {
            ReadOnlySpan<char> path = Path;
            if (path.Length < 32)
            {
                return new string(path);
            }
            Span<char> newPath = stackalloc char[32];
            path = path.Slice(0, 29);
            path.CopyTo(newPath);
            for (int i = 29; i < 32; i++)
            {
                newPath[i] += '.';
            }
            return new string(newPath);
        }
    }

    public Client() {}

    public Client(string name, string path, ClientType type) 
    {
        Name = name;
        Path = path.Replace("\\", "/");
        ClientType = type;
    }
}

[Flags]
public enum ClientType 
{
    None,
    FortRise,
    Steam
}

public partial class VersionTags : IDeserialize, ISerialize
{
    [Name("ref")]
    [TeuObject]
    public string Refs;

    [Name("node_id")]
    [TeuObject]
    public string NodeID;

    [Name("url")]
    [TeuObject]
    public string Url;

    [Name("object")]
    [TeuObject]
    public VersionObject Object;

    [Ignore]
    public string Name;

    [Ignore]
    public Version Version 
    {
        get 
        {
            var index = Name.IndexOf('-');
            if (index != -1) 
            {
                var name = Name.Substring(0, index);
                return new Version(name);
            }
            return new Version(Name);
        }
    }
    
    public void InitName() 
    {
        ReadOnlySpan<char> refTag = Refs.AsSpan();
        Name = new string(refTag.Slice(10));
    }

}

public partial class VersionObject: IDeserialize, ISerialize
{
    [Name("sha")]
    [TeuObject]
    public string Sha;

    [Name("type")]
    [TeuObject]
    public string Type;

    [Name("url")]
    [TeuObject]
    public string Url;
}