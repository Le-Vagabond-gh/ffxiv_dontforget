using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Linq;

namespace dontforget
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Don't Forget";
        private const string CommandName = "/dontforget";
        private const string ShortCommandName = "/df";
        private IDalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Don't Forget");
        private ConfigWindow ConfigWindow { get; init; }
        private unsafe static ActionManager* AM;
        private uint summonFairy = 17215;
        private uint summonCarbuncle = 25798;
        private uint peloton = 7557;
        private uint sprint = 4;
        private uint gysahlGreens = 4868;
        // Tank stance actions
        private uint ironWill = 28;      // Paladin
        private uint defiance = 48;      // Warrior
        private uint grit = 3629;        // Dark Knight
        private uint royalGuard = 16142; // Gunbreaker
        // Tank stance status IDs
        private uint ironWillStatus = 79;
        private uint defianceStatus = 91;
        private uint gritStatus = 743;
        private uint royalGuardStatus = 1833;
        private DateTime lastDebugLog = DateTime.MinValue;
        private DateTime lastSummonDebugLog = DateTime.MinValue;
        private DateTime lastGysahlUse = DateTime.MinValue;
        private DateTime demiSummonLastSeen = DateTime.MinValue;
        private bool wasUnconscious = false;
        private bool wasInCombat = false;
        private DateTime playerRaisedTimestamp = DateTime.MinValue;

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.PluginInterface.Create<Service>(this);
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the config window!"
            });

            this.CommandManager.AddHandler(ShortCommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open config or toggle settings. Usage: /df [tankstance]"
            });

            unsafe { LoadUnsafe(); }

            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            Service.Framework.Update += onFrameworkUpdate;
        }

        private unsafe void LoadUnsafe()
        {
            AM = ActionManager.Instance();
        }

        private unsafe void onFrameworkUpdate(IFramework framework)
        {
            if (Service.ClientState == null || Service.ObjectTable.LocalPlayer == null) return;

            var isInCombat = Service.Condition[ConditionFlag.InCombat];
            var isCurrentlyUnconscious = Service.Condition[ConditionFlag.Unconscious];

            // Reset timer when exiting combat
            if (wasInCombat && !isInCombat)
            {
                playerRaisedTimestamp = DateTime.MinValue;
                if (this.Configuration.DebugLogging)
                {
                    Service.PluginLog.Info("Exited combat - reset raise timer");
                }
            }

            // Detect transition from unconscious to conscious (being raised)
            if (wasUnconscious && !isCurrentlyUnconscious)
            {
                playerRaisedTimestamp = DateTime.Now;
                if (this.Configuration.DebugLogging)
                {
                    Service.PluginLog.Info("Player raised - combat summon allowed for 15 seconds");
                }
            }

            wasInCombat = isInCombat;
            wasUnconscious = isCurrentlyUnconscious;

            // Handle combat summoning restrictions
            if (isInCombat)
            {
                if (!this.Configuration.SummonInCombatAfterDeath)
                {
                    return; // Config disabled - never summon in combat
                }

                // Only summon if raised within last 15 seconds
                if (playerRaisedTimestamp == DateTime.MinValue)
                {
                    return; // Never been raised - don't summon
                }

                var timeSinceRaise = (DateTime.Now - playerRaisedTimestamp).TotalSeconds;
                if (timeSinceRaise > 15)
                {
                    return; // More than 15 seconds since raise
                }
            }

            var isPelotonReady = AM->GetActionStatus(ActionType.Action, peloton) == 0;
            var isSprintReady = AM->GetActionStatus(ActionType.GeneralAction, sprint) == 0;
            var hasPelotonBuff = Service.ObjectTable.LocalPlayer.StatusList.Any(x => x.StatusId == 1199);
            var hasSprintBuff = Service.ObjectTable.LocalPlayer.StatusList.Any(x => x.StatusId == 50);

            if (this.Configuration.DebugLogging && (DateTime.Now - lastDebugLog).TotalSeconds >= 2)
            {
                lastDebugLog = DateTime.Now;
                var sprintStatus = AM->GetActionStatus(ActionType.GeneralAction, sprint);
                var sprintRecast = AM->GetRecastTime(ActionType.GeneralAction, sprint);
                var isMovingValue = AgentMap.Instance()->IsPlayerMoving;
                Service.PluginLog.Info($"Peloton ready: {isPelotonReady}, Sprint ready: {isSprintReady} (status:{sprintStatus}), HasPeloton: {hasPelotonBuff}, HasSprint: {hasSprintBuff}, Moving: {isMovingValue}, Recast: {sprintRecast}");
            }

            var isMoving = AgentMap.Instance()->IsPlayerMoving;

            // Auto Peloton when moving (Phys Ranged only)
            if (this.Configuration.Peloton && isMoving && isPelotonReady && !hasPelotonBuff)
            {
                AM->UseAction(ActionType.Action, peloton);
            }

            // Auto Sprint when moving (any class)
            if (this.Configuration.AutoSprint && isMoving && isSprintReady && !hasPelotonBuff && !hasSprintBuff)
            {
                AM->UseAction(ActionType.GeneralAction, sprint);
            }

            // Auto summon pet when not moving and not in combat
            if (!isMoving)
            {
                var classJobID = Service.ObjectTable.LocalPlayer.ClassJob.RowId;
                var playerGameObjectId = Service.ObjectTable.LocalPlayer.GameObjectId;
                
                // Check if pet is already summoned by looking for pet in object table
                var petNames = new[] { "Carbuncle", "Eos", "Selene" };
                var demiSummonNames = new[] { "Demi-Bahamut", "Demi-Phoenix", "Ifrit-Egi", "Titan-Egi", "Garuda-Egi", "Solar Bahamut", "Seraph" };

                var hasPet = Service.ObjectTable.Any(obj =>
                    obj.OwnerId == playerGameObjectId &&
                    obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
                    obj.IsValid() &&
                    petNames.Contains(obj.Name.ToString()));

                var hasDemiSummon = Service.ObjectTable.Any(obj =>
                    obj.OwnerId == playerGameObjectId &&
                    obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
                    obj.IsValid() &&
                    demiSummonNames.Contains(obj.Name.ToString()));

                // Track when demi-summons were last active
                if (hasDemiSummon)
                {
                    demiSummonLastSeen = DateTime.Now;
                }

                if (this.Configuration.DebugLogging && (DateTime.Now - lastSummonDebugLog).TotalSeconds >= 2)
                {
                    lastSummonDebugLog = DateTime.Now;
                    var isFairySummonReady = AM->GetActionStatus(ActionType.Action, summonFairy) == 0;
                    var isCarbuncleSummonReady = AM->GetActionStatus(ActionType.Action, summonCarbuncle) == 0;
                    Service.PluginLog.Info($"Summon check - ClassJobID: {classJobID}, HasPet: {hasPet}, FairyReady: {isFairySummonReady}, CarbuncleReady: {isCarbuncleSummonReady}, ScholarEnabled: {this.Configuration.Scholar}, SummonerEnabled: {this.Configuration.Summoner}");
                    
                    // Debug: Log all objects owned by player
                    var petObjects = Service.ObjectTable.Where(obj => obj.OwnerId == playerGameObjectId).ToList();
                    Service.PluginLog.Info($"Objects owned by player: {petObjects.Count}");
                    foreach (var obj in petObjects)
                    {
                        Service.PluginLog.Info($"  - Kind: {obj.ObjectKind}, IsValid: {obj.IsValid()}, Name: {obj.Name}, DataId: {obj.DataId}, EntityId: {obj.EntityId}");
                    }
                }

                if (!hasPet)
                {
                    var isFairySummonReady = AM->GetActionStatus(ActionType.Action, summonFairy) == 0;
                    var isCarbuncleSummonReady = AM->GetActionStatus(ActionType.Action, summonCarbuncle) == 0;

                    if (classJobID == 28 && this.Configuration.Scholar && isFairySummonReady)
                    {
                        AM->UseAction(ActionType.Action, summonFairy);
                    }
                    else if (classJobID == 27 && this.Configuration.Summoner && isCarbuncleSummonReady)
                    {
                        AM->UseAction(ActionType.Action, summonCarbuncle);
                    }
                }

                // Auto tank stance when not moving
                if (this.Configuration.TankStance)
                {
                    var statusList = Service.ObjectTable.LocalPlayer.StatusList;

                    // Paladin (19) - Iron Will
                    if (classJobID == 19 && !statusList.Any(x => x.StatusId == ironWillStatus))
                    {
                        if (AM->GetActionStatus(ActionType.Action, ironWill) == 0)
                        {
                            AM->UseAction(ActionType.Action, ironWill);
                        }
                    }
                    // Warrior (21) - Defiance
                    else if (classJobID == 21 && !statusList.Any(x => x.StatusId == defianceStatus))
                    {
                        if (AM->GetActionStatus(ActionType.Action, defiance) == 0)
                        {
                            AM->UseAction(ActionType.Action, defiance);
                        }
                    }
                    // Dark Knight (32) - Grit
                    else if (classJobID == 32 && !statusList.Any(x => x.StatusId == gritStatus))
                    {
                        if (AM->GetActionStatus(ActionType.Action, grit) == 0)
                        {
                            AM->UseAction(ActionType.Action, grit);
                        }
                    }
                    // Gunbreaker (37) - Royal Guard
                    else if (classJobID == 37 && !statusList.Any(x => x.StatusId == royalGuardStatus))
                    {
                        if (AM->GetActionStatus(ActionType.Action, royalGuard) == 0)
                        {
                            AM->UseAction(ActionType.Action, royalGuard);
                        }
                    }
                }
            }

            // Auto Gysahl Greens when chocobo timer is low (after pet summon to prioritize pets)
            // Only check every 2 seconds to allow cast to complete
            if (this.Configuration.AutoGysahlGreens && !isInCombat && (DateTime.Now - lastGysahlUse).TotalSeconds >= 5)
            {
                var companionTimeLeft = UIState.Instance()->Buddy.CompanionInfo.TimeLeft;
                // Only use if chocobo is present (timer > 0) and under 15 minutes (900 seconds)
                if (companionTimeLeft > 0 && companionTimeLeft < 900)
                {
                    var canUseGysahl = AM->GetActionStatus(ActionType.Item, gysahlGreens) == 0;
                    if (canUseGysahl)
                    {
                        AM->UseAction(ActionType.Item, gysahlGreens, 0xE0000000, 0xFFFF);
                        lastGysahlUse = DateTime.Now;
                        if (this.Configuration.DebugLogging)
                        {
                            Service.PluginLog.Info($"Used Gysahl Greens - Chocobo timer was {companionTimeLeft:F0} seconds");
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            this.CommandManager.RemoveHandler(ShortCommandName);
            Service.Framework.Update -= onFrameworkUpdate;
        }

        private void OnCommand(string command, string args)
        {
            var trimmedArgs = args.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(trimmedArgs))
            {
                ConfigWindow.IsOpen = true;
                return;
            }

            if (trimmedArgs == "tankstance")
            {
                this.Configuration.TankStance = !this.Configuration.TankStance;
                this.Configuration.Save();
                var status = this.Configuration.TankStance ? "enabled" : "disabled";
                Service.ChatGui.Print($"[Don't Forget] Auto Tank Stance {status}");
                return;
            }

            // Unknown argument, show help
            Service.ChatGui.Print("[Don't Forget] Usage: /df [tankstance]");
        }

        public void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            this.ConfigWindow.IsOpen = true;
        }

    }
}
