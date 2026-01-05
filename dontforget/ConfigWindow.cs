using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace dontforget;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    public ConfigWindow(Plugin plugin) : base(
        "Don't Forget",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(270, 235);
        this.SizeCondition = ImGuiCond.Always;
        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var scholarConfig = this.Configuration.Scholar;
        var summonerConfig = this.Configuration.Summoner;
        var pelotonConfig = this.Configuration.Peloton;

        ImGui.TextWrapped("Enable for automation of these actions");
        ImGui.Spacing();

        if (ImGui.Checkbox("Phys Ranged - Auto Peloton", ref pelotonConfig))
        {
            this.Configuration.Peloton = pelotonConfig;
            this.Configuration.Save();
        }

        var sprintConfig = this.Configuration.AutoSprint;
        if (ImGui.Checkbox("Auto Sprint", ref sprintConfig))
        {
            this.Configuration.AutoSprint = sprintConfig;
            this.Configuration.Save();
        }

        if (ImGui.Checkbox("Scholar - Summon Fairy", ref scholarConfig))
        {
            this.Configuration.Scholar = scholarConfig;
            this.Configuration.Save();
        }

        if (ImGui.Checkbox("Summoner - Summon Carbuncle", ref summonerConfig))
        {
            this.Configuration.Summoner = summonerConfig;
            this.Configuration.Save();
        }

        var summonInCombatConfig = this.Configuration.SummonInCombat;
        if (ImGui.Checkbox("Allow Summon in Combat", ref summonInCombatConfig))
        {
            this.Configuration.SummonInCombat = summonInCombatConfig;
            this.Configuration.Save();
        }

        ImGui.Spacing();
        var debugConfig = this.Configuration.DebugLogging;
        if (ImGui.Checkbox("Debug Logging", ref debugConfig))
        {
            this.Configuration.DebugLogging = debugConfig;
            this.Configuration.Save();
        }
    }
}
