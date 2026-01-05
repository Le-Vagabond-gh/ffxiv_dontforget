using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Linq;

namespace dontforget
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Don't Forget";
        private const string CommandName = "/dontforget";
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
        private DateTime lastDebugLog = DateTime.MinValue;
        private DateTime lastSummonDebugLog = DateTime.MinValue;

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

            // Skip if in combat and summon in combat is disabled
            if (Service.Condition[ConditionFlag.InCombat] && !this.Configuration.SummonInCombat) return;

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
                var hasPet = Service.ObjectTable.Any(obj => 
                    obj.OwnerId == playerGameObjectId && 
                    obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc &&
                    obj.IsValid() &&
                    petNames.Contains(obj.Name.ToString()));

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
            }
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            Service.Framework.Update -= onFrameworkUpdate;
        }

        private void OnCommand(string command, string args)
        {
            ConfigWindow.IsOpen = true;
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
