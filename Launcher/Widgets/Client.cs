using System.Diagnostics;
using System.Text;
using ImGuiNET;

namespace FortLauncher;

public partial class Launcher 
{
    public List<Client> Client = new();
    private List<Client> clientToRemove = new();
    private HashSet<string> clientPaths = new();
    private string Client_hovered = "";
    private bool Client_renamePopup;
    private bool Client_patchPopup;
    private string Client_newRename = "";
    private Client currentClientEdit;
    public Client SelectedClient;

    public void WidgetClient() 
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 150));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, 305));
        ImGui.Begin("Clients", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);
        ImGui.SetCursorPosX((250 - ImGui.CalcTextSize("Clients").X) * 0.5f);
        ImGui.Text("Clients");

        ImGui.Separator();

        ImGui.BeginChild("Client-child", new System.Numerics.Vector2(230, 210));

        foreach (var client in Client) 
        {
            ImGui.PushID(client.Path);
            if (ImGui.Selectable(client.Name))
            {
                SelectedClient = client;
            }
            ImGui.PopID();
            if (ImGui.BeginPopupContextItem(client.Path)) 
            {
                var str = client.ClientType.HasFlag(ClientType.FortRise) ? "Unpatch" : "Patch";
                if (ImGui.MenuItem(str))
                {
                    Client_patchPopup = true;
                    currentClientEdit = client;
                    Task.Run(() => Client_Patch(client));
                }
                if (ImGui.MenuItem("Rename"))
                {
                    Client_newRename = client.Name;
                    Client_renamePopup = true;
                    currentClientEdit = client;
                }
                if (ImGui.MenuItem("Remove"))
                    Client_Remove(client.Name);
                
                ImGui.EndPopup();
            }
            if (ImGui.IsItemHovered()) 
                Client_hovered = client.CutPath;
            else Client_hovered = string.Empty;
        }

        Client_DoRemove();


        ImGui.EndChild();
        ImGui.SetCursorPosY(260);
        if (ImGui.Button("+ Add")) 
        {
            Client_FolderPick();
        }
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

            process.StartInfo.FileName = Path.GetFullPath("installer/Installer.NoAnsi.exe");
            process.StartInfo.Arguments = textUnpatch + " \"" + client.Path + "\"";
            process.StartInfo.WorkingDirectory = Path.GetFullPath("installer");
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
            Client_patchPopup = false;
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
        var client = Client.Where(x => x.Name == clientName).First();
        clientToRemove.Add(client);
        if (SelectedClient == client)
            SelectedClient = null;
    }

    public void Client_DoRemove() 
    {
        foreach (var client in clientToRemove) 
        {
            Client.Remove(client);
            clientPaths.Remove(client.Path);
        }
        clientToRemove.Clear();
    }

    public void Client_FolderPick()
    {
        var folderPicker = NativeFileDialogSharp.Dialog.FolderPicker();
        if (!folderPicker.IsOk)
            return;
        
        if (clientPaths.Contains(folderPicker.Path))
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

        Client.Add(new Client(name, folderPathStr, type));
        clientPaths.Add(folderPathStr);
    }
}

public class Client 
{
    public string Name;
    public string Path;
    public ClientType ClientType;

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

    public Client(string name, string path, ClientType type) 
    {
        Name = name;
        Path = path;
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