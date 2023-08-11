using ImGuiLib;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace FortLauncher;

public partial class Launcher : Game
{
    private GraphicsDeviceManager graphics;
    private ImGuiRenderer renderer;

    public const int Width = 1024;
    public const int Height = 640;

    public Launcher() 
    {
        Window.Title = "Fortrise Launcher";
        Window.AllowUserResizing = false;
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = Width;
        graphics.PreferredBackBufferHeight = Height;
        graphics.PreferMultiSampling = true;

        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        renderer = new ImGuiRenderer(this);
        renderer.RebuildFontAtlas();
        base.Initialize();

        ImGui.StyleColorsDark();
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


        if (Client_patchPopup)  
        {
            WidgetPatch(currentClientEdit);
        }
        else  
        {
            WidgetClient();
            WidgetMenuButtons();
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