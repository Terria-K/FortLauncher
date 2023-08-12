using System.Diagnostics;
using ImGuiNET;

namespace FortLauncher;

public partial class Launcher 
{
    private bool MenuButtons_errorPopup;
    private string MenuButtons_error = "";

    public void WidgetMenuButtons() 
    {
        const float ButtonPanelWidth = 450;

        ImGui.SetNextWindowPos(new System.Numerics.Vector2((Width * 0.5f) - (ButtonPanelWidth / 2), 150));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(ButtonPanelWidth, 300));

        ImGui.Begin("Buttons", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar);
        ImGui.SetCursorPosX((((Width * 0.5f) - (ButtonPanelWidth / 2) * 0.6f) - ImGui.CalcTextSize("Clients").X) * 0.5f);
        ImGui.Text("FortRise Launcher");
        var largeButtonSize = new System.Numerics.Vector2(150, 30);
        ImGui.SetCursorPosX((ButtonPanelWidth * 0.32f) - (largeButtonSize.X * 0.5f));
        ImGui.SetCursorPosY((ButtonPanelWidth * 0.4f) - (largeButtonSize.X * 0.5f));
        MenuButtons_BeginDisableWithFortRiseIfNeeded();
        if (ImGui.Button("Launch FortRise", largeButtonSize)) 
        {
            MenuButtons_LaunchFortRise();            
        }
        MenuButtons_EndDisableWithFortRiseIfNeeded();
        ImGui.SameLine();

        MenuButtons_BeginDisableIfNeeded();
        if (ImGui.Button("Launch TowerFall", largeButtonSize)) 
        {
            MenuButtons_LaunchTowerFall();
        }
        MenuButtons_EndDisableIfNeeded();
        var smallButtonSize = largeButtonSize with { X = 100};
        ImGui.SetCursorPosX((ButtonPanelWidth * 0.26f) - (smallButtonSize.X * 0.5f));
        ImGui.Button("Settings", smallButtonSize);
        ImGui.SameLine();

        if (ImGui.Button("Mods", smallButtonSize)) 
        {
            Mods_popup = true;
            Task.Run(() => NoRefreshMods());
        }

        ImGui.SameLine();
        if (ImGui.Button("Quit", smallButtonSize)) 
        {
            Quit();
        }
        ImGui.End();

        if (MenuButtons_errorPopup) 
        {
            ImGui.OpenPopup("Error");
            WidgetMenuButtonsErrorPopup();
        }
    }

    public void WidgetMenuButtonsErrorPopup() 
    {
        const float ErrorWidth = 200;
        const float ErrorHeight = 120;
        ImGui.SetNextWindowPos(new System.Numerics.Vector2((Width * 0.5f) - (ErrorWidth / 2), (Height * 0.5f) - (ErrorHeight / 2)));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(ErrorWidth, ErrorHeight));
        ImGui.BeginPopupModal("Error", ref MenuButtons_errorPopup, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);

        ImGui.SetCursorPosX((ErrorWidth * 0.5f) - (ImGui.CalcTextSize(MenuButtons_error).X) / 2);
        ImGui.Text(MenuButtons_error);

        ImGui.SetCursorPosX((ErrorWidth * 0.43f));
        ImGui.SetCursorPosY(90);

        if (ImGui.Button("OK")) 
        {
            MenuButtons_errorPopup = false;
        }

        ImGui.EndPopup();
    }

    private void MenuButtons_LaunchFortRise() 
    {
        bool wentFromVanilla = false;

        var path = SelectedClient.Path;
        var origPath = Path.Combine(path, "fortOrig/TowerFall.exe");
        var towerFallPath = Path.Combine(path, "TowerFall.exe");
        var origFortPath = Path.Combine(path, "fortOrig/FortRise.exe");
        var vanillaTxt = Path.Combine(path, "vanilla.txt");

        MenuButtons_errorPopup = true;

        try 
        {
            var process = new Process();

            if (File.Exists(vanillaTxt))  
            {
                File.Copy(origFortPath, towerFallPath, true);
                wentFromVanilla = true;
            }

            process.StartInfo.FileName = towerFallPath;
            process.StartInfo.WorkingDirectory = path;
            if (process.Start() && wentFromVanilla) 
            {
                File.Delete(vanillaTxt);
                return;
            }
            MenuButtons_errorPopup = true;
            MenuButtons_error = "Process is still running.";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.WriteLine(ex.StackTrace);

            MenuButtons_errorPopup = true;
            MenuButtons_error = "Process is still running.";
            if (!wentFromVanilla)
                return;
            
            if (!File.Exists(origPath))
                return;

            using var _ = File.Create(vanillaTxt);
        }
    }

    private void MenuButtons_LaunchTowerFall() 
    {
        try 
        {
            var process = new Process();
            var path = SelectedClient.Path;
            var origPath = Path.Combine(path, "fortOrig/TowerFall.exe");
            var towerFallPath = Path.Combine(path, "TowerFall.exe");
            var vanillaTxt = Path.Combine(path, "vanilla.txt");

            if (!File.Exists(vanillaTxt) && File.Exists(origPath)) 
            {
                var origFortPath = Path.Combine(path, "fortOrig/FortRise.exe");

                using var _ = File.Create(vanillaTxt);
                File.Copy(towerFallPath, origFortPath, true);
                File.Copy(origPath, towerFallPath, true);
            }

            process.StartInfo.FileName = towerFallPath;
            process.StartInfo.WorkingDirectory = path;
            process.Start();
        }
        catch 
        {
            // TODO Error Handling
        }
    }

    private void MenuButtons_BeginDisableWithFortRiseIfNeeded() 
    {
        if (SelectedClient is not null && SelectedClient.ClientType.HasFlag(ClientType.FortRise)) 
            return;
        
        ImGui.BeginDisabled();
    }

    private void MenuButtons_EndDisableWithFortRiseIfNeeded() 
    {
        if (SelectedClient is not null && SelectedClient.ClientType.HasFlag(ClientType.FortRise))
            return;
        ImGui.EndDisabled();
    }

    private void MenuButtons_BeginDisableIfNeeded() 
    {
        if (SelectedClient != null) 
            return;
        
        ImGui.BeginDisabled();
    }

    private void MenuButtons_EndDisableIfNeeded() 
    {
        if (SelectedClient != null)
            return;
        ImGui.EndDisabled();
    }
}