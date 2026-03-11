using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;

namespace dontforget
{
    public enum ChocoboStanceOption : byte
    {
        Disabled = 0,
        FreeStance = 4,
        AttackerStance = 5,
        DefenderStance = 6,
        HealerStance = 7,
    }

    public class ChocoboStanceKeeper
    {
        public static readonly string[] StanceLabels = new[]
        {
            "Disabled",
            "Free Stance",
            "Attacker Stance",
            "Defender Stance",
            "Healer Stance",
        };

        // Maps dropdown index (0-4) to BuddyAction ID
        public static readonly byte[] StanceIds = new byte[] { 0, 4, 5, 6, 7 };

        public static int StanceIdToIndex(byte id)
        {
            for (int i = 0; i < StanceIds.Length; i++)
                if (StanceIds[i] == id) return i;
            return 0;
        }

        private readonly Configuration configuration;
        private DateTime lastStanceAction = DateTime.MinValue;

        public ChocoboStanceKeeper(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public unsafe void Update()
        {
            try
            {
                if (configuration.ChocoboStanceOption == 0) return;

                var companionInfo = &UIState.Instance()->Buddy.CompanionInfo;
                var timeLeft = companionInfo->TimeLeft;
                if (timeLeft <= 0) return;

                var activeCommand = companionInfo->ActiveCommand;
                var desired = configuration.ChocoboStanceOption;

                if (activeCommand == desired) return;

                // Cooldown to prevent spam
                if ((DateTime.Now - lastStanceAction).TotalSeconds < 2) return;

                var am = ActionManager.Instance();
                var actionStatus = am->GetActionStatus(ActionType.BuddyAction, desired);
                if (actionStatus == 0)
                {
                    am->UseAction(ActionType.BuddyAction, desired);
                    lastStanceAction = DateTime.Now;
                    if (configuration.DebugLogging)
                    {
                        Service.PluginLog.Info($"[ChocoboStance] Applied stance {desired} (was {activeCommand})");
                    }
                }
            }
            catch (Exception ex)
            {
                if (configuration.DebugLogging)
                {
                    Service.PluginLog.Error($"[ChocoboStance] Error: {ex.Message}");
                }
            }
        }
    }
}
