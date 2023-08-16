using ImGuiLib;
using ImGuiNET;
using Microsoft.Xna.Framework;
using TeuJson;

namespace FortLauncher;

public partial class Launcher : Game
{
    private GraphicsDeviceManager graphics;
    private ImGuiRenderer renderer;

    public SaveData Data;

    public const int Width = 1024;
    public const int Height = 640;
    public LauncherState State;

    public enum LauncherState { Main, Settings, Patching, Installer, Mods }

    public Launcher() 
    {
        Window.Title = "Fort Launcher";
        Window.AllowUserResizing = false;
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = Width;
        graphics.PreferredBackBufferHeight = Height;
        graphics.PreferMultiSampling = true;

        IsMouseVisible = true;
    }

    public void Save() 
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var combined = Path.Combine(appData, "FortRiseLauncher", "savedata.json");
        if (!Directory.Exists(Path.GetDirectoryName(combined))) 
            Directory.CreateDirectory(Path.GetDirectoryName(combined));
        
        JsonConvert.SerializeToFile(Data, combined);
    }

    public void Load() 
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var combined = Path.Combine(appData, "FortRiseLauncher", "savedata.json");

        if (!File.Exists(combined)) 
        {
            Data = new SaveData();
            return;
        }
        
        var json = JsonConvert.DeserializeFromFile<SaveData>(combined);
        Data = json;

        Data.Clients ??= new List<Client>();

        foreach (var client in Data.Clients) 
        {
            clientPaths.Add(client.Path);
            if (client.Path == Data.CurrentClientPath)
                SelectedClient = client;
        }
        selectedInstallerVersion = Data.CurrentInstaller;
    }

    protected override void Initialize()
    {
        renderer = new ImGuiRenderer(this);
        renderer.RebuildFontAtlas();

        Load();
        base.Initialize();

        ImGui.StyleColorsDark();

        FetchDownloadedVersions();
    }

    protected override void LoadContent()
    {
        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        renderer.BeforeLayout(gameTime);
        SetupDockspace();

        switch (State) 
        {
        case LauncherState.Main:
            WidgetClient();
            WidgetInstaller();
            WidgetMenuButtons();
            break;
        case LauncherState.Settings:
            WidgetSettings();
            break;
        case LauncherState.Patching:
            WidgetPatch(currentClientEdit ?? SelectedClient);
            break;
        case LauncherState.Installer:
            WidgetInstallerPopup();
            break;
        case LauncherState.Mods:
            WidgetMods();
            break;
        }

        renderer.AfterLayout();
        base.Draw(gameTime);
    }

    private void Quit() 
    {
        Exit();
    }

    private void SetupDockspace() 
    {
        var windowFlags = 
            ImGuiWindowFlags.MenuBar
            | ImGuiWindowFlags.NoDocking;
        
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(Width, Height));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        windowFlags |= ImGuiWindowFlags.NoTitleBar 
            | ImGuiWindowFlags.NoCollapse 
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoNavFocus;

        bool dockSpaceTrue = true;
        ImGui.Begin("Dockspace", ref dockSpaceTrue, windowFlags); 
        ImGui.PopStyleVar(2);

        // Dockspace
        ImGuiIOPtr ioPtr = ImGui.GetIO();

        if ((ioPtr.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0) 
        {
            var dockspaceID = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceID, System.Numerics.Vector2.Zero);
        }
    }
}