using ImGuiNET;

namespace FortLauncher;

public partial class Launcher 
{
    public void WidgetSettings() 
    {

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(40, 40));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(Width - 80, Height - 80));
        ImGui.Begin("Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

        WidgetSettingsCheckboxFlags("Debug Mode", "--debug");
        ImGui.SameLine();
        WidgetSettingsCheckboxFlags("No Quit", "-noquit");
        ImGui.SameLine();
        ImGui.SetCursorPosX(270);
        WidgetSettingsCheckboxFlags("Load Log", "-loadlog");

        WidgetSettingsCheckboxFlags("Verbose", "--verbose");
        ImGui.SameLine();
        ImGui.SetCursorPosX(109);
        WidgetSettingsCheckboxFlags("No Gamepads", "-nogamepads");
        WidgetSettingsCheckboxFlags("No Intro", "-nointro");
        ImGui.SameLine();
        ImGui.SetCursorPosX(109);
        WidgetSettingsCheckboxFlags("No Gamepads Update", "-nogamepadupdates");

        ImGui.SetCursorPosY(112);
        ImGui.SetCursorPosX(720);
        ImGui.BeginChild("graphics-child", new System.Numerics.Vector2(200, 40));
        ImGui.Text("Current Device: " + Settings_GetDeviceName());
        if (ImGui.BeginMenu("Graphics Device")) 
        {
            WidgetSettingsSelectDevice("Auto");
            WidgetSettingsSelectDevice("D3D11");
            WidgetSettingsSelectDevice("OpenGL");
            WidgetSettingsSelectDevice("Vulkan", "(WARNING: INCOMPLETE DEVICE, FLASHING LIGHTS)");
            ImGui.EndMenu();
        }
        ImGui.EndChild();


        ImGui.SetCursorPosY(160);
        ImGui.Separator();

        ImGui.InputText("Arguments", ref Data.LaunchArguments, 150);
        if (Data.LaunchArguments.Contains("--vanilla")) 
        {
            ImGui.Text("Oh yes, I know you will do that eventually.. but please can you remove that?");
            ImGui.Text("It will just launch the vanilla version of TowerFall once you tried to launch FortRise.");
            ImGui.Text("This argument is meant to be used if you are not using the launcher.");
        }
        if (ImGui.Button("Back")) 
        {
            State = LauncherState.Main;
            Save();
        }

        ImGui.End();
    }

    private void WidgetSettingsSelectDevice(string device, string notice = "") 
    {
        if (ImGui.MenuItem(device + " " + notice)) 
        {
            ClearAllDevice();
            if (device == "Auto")
                return;

            Data.LaunchArguments += "/gldevice:" + device + " ";
            Save();
        }

        void ClearAllDevice() 
        {
            Data.LaunchArguments = Data.LaunchArguments.Replace("/gldevice:OpenGL ", "");
            Data.LaunchArguments = Data.LaunchArguments.Replace("/gldevice:Vulkan ", "");
            Data.LaunchArguments = Data.LaunchArguments.Replace("/gldevice:D3D11 ", "");
        }
    }

    private string Settings_GetDeviceName() 
    {
        if (Data.LaunchArguments.Contains("/gldevice:OpenGL"))
            return "OpenGL";
        else if (Data.LaunchArguments.Contains("/gldevice:D3D11"))
            return "D3D11";
        else if (Data.LaunchArguments.Contains("/gldevice:Vulkan"))
            return "Vulkan (!!)";
        else
            return "Auto";
    }

    public void WidgetSettingsCheckboxFlags(string title, string argument) 
    {
        bool flags = Data.LaunchArguments.Contains(argument);
        if (ImGui.Checkbox(title, ref flags)) 
        {
            flags = Data.LaunchArguments.Contains(argument);
            if (flags) 
                Data.LaunchArguments = Data.LaunchArguments.Replace($"{argument} ", "");
            else
                Data.LaunchArguments += $"{argument} ";

            Save();
        }
    }
}