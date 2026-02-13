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
        this.Size = new Vector2(290, 335);
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

        var gysahlConfig = this.Configuration.AutoGysahlGreens;
        if (ImGui.Checkbox("Auto Gysahl Greens (< 15 min)", ref gysahlConfig))
        {
            this.Configuration.AutoGysahlGreens = gysahlConfig;
            this.Configuration.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Automatically use Gysahl Greens when your chocobo\ncompanion's timer falls below 15 minutes.");
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

        var tankStanceConfig = this.Configuration.TankStance;
        if (ImGui.Checkbox("Tank - Auto Tank Stance", ref tankStanceConfig))
        {
            this.Configuration.TankStance = tankStanceConfig;
            this.Configuration.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Automatically enables tank stance when standing still.\nToggle with: /df tankstance");
        }

        var gatheringBuffsConfig = this.Configuration.GatheringBuffs;
        if (ImGui.Checkbox("Gatherer - Auto Buffs", ref gatheringBuffsConfig))
        {
            this.Configuration.GatheringBuffs = gatheringBuffsConfig;
            this.Configuration.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Automatically enables Prospect/Triangulate, Sneak, and\nTruth of Mountains/Forests/Oceans when standing still\non Miner, Botanist, or Fisher.");
        }

        var autoSwitchGathererConfig = this.Configuration.AutoSwitchGatherer;
        if (ImGui.Checkbox("Gatherer - Auto Switch Class", ref autoSwitchGathererConfig))
        {
            this.Configuration.AutoSwitchGatherer = autoSwitchGathererConfig;
            this.Configuration.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Automatically switches to Miner or Botanist when you\ntry to gather from the wrong node type.\nRequires a gearset saved for each gatherer class.");
        }

        var summonInCombatAfterDeathConfig = this.Configuration.SummonInCombatAfterDeath;
        if (ImGui.Checkbox("Summon in Combat (After Death Only)", ref summonInCombatAfterDeathConfig))
        {
            this.Configuration.SummonInCombatAfterDeath = summonInCombatAfterDeathConfig;
            this.Configuration.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("When enabled, pets will auto-summon in combat only if you died\nand were raised within the last 15 seconds.");
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
