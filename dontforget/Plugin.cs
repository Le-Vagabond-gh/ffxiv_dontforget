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
            if (Service.ClientState == null || Service.ObjectTable.LocalPlayer == null || !Service.ClientState.IsLoggedIn || Service.Condition[ConditionFlag.InCombat]) return;

            var isPelotonReady = AM->GetActionStatus(ActionType.Action, peloton) == 0;
            var isSprintReady = AM->GetActionStatus(ActionType.GeneralAction, sprint) == 0;
            var hasPelotonBuff = Service.ObjectTable.LocalPlayer.StatusList.Any(x => x.StatusId == 1199);
            var hasSprintBuff = Service.ObjectTable.LocalPlayer.StatusList.Any(x => x.StatusId == 50);

            if (this.Configuration.DebugLogging && (DateTime.Now - lastDebugLog).TotalSeconds >= 2)
            {
                lastDebugLog = DateTime.Now;
                var sprintStatus = AM->GetActionStatus(ActionType.GeneralAction, sprint);
                var sprintRecast = AM->GetRecastTime(ActionType.GeneralAction, sprint);
                Service.PluginLog.Info($"Peloton ready: {isPelotonReady}, Sprint ready: {isSprintReady} (status:{sprintStatus}), HasPeloton: {hasPelotonBuff}, HasSprint: {hasSprintBuff}, Moving: {AgentMap.Instance()->IsPlayerMoving}, Recast: {sprintRecast}");
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
                
                // Check if pet is already summoned by looking for pet in object table
                var hasPet = Service.ObjectTable.Any(obj => obj.OwnerId == Service.ObjectTable.LocalPlayer.GameObjectId && 
                    (obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc));

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
