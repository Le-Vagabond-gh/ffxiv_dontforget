using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;

namespace dontforget
{
    public class ChocoboStanceKeeper
    {
        private readonly Configuration configuration;
        private float previousTimeLeft = 0;
        private byte previousActiveCommand = 0;
        private DateTime chocoboAppearedAt = DateTime.MinValue;
        private bool stanceRestored = false;

        public ChocoboStanceKeeper(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public unsafe void Update()
        {
            try
            {
                var companionInfo = &UIState.Instance()->Buddy.CompanionInfo;
                var timeLeft = companionInfo->TimeLeft;
                var activeCommand = companionInfo->ActiveCommand;

                if (timeLeft > 0)
                {
                    // Chocobo just appeared (was gone, now present)
                    if (previousTimeLeft <= 0)
                    {
                        chocoboAppearedAt = DateTime.Now;
                        stanceRestored = false;
                        if (configuration.DebugLogging)
                        {
                            Service.PluginLog.Info($"[ChocoboStance] Chocobo appeared, current stance: {activeCommand}, saved: {configuration.SavedChocoboStance}");
                        }
                    }

                    // Restore saved stance after a short delay to let the summon settle
                    if (!stanceRestored && configuration.SavedChocoboStance != 0
                        && chocoboAppearedAt != DateTime.MinValue
                        && (DateTime.Now - chocoboAppearedAt).TotalSeconds >= 1.5)
                    {
                        if (activeCommand != configuration.SavedChocoboStance)
                        {
                            var am = ActionManager.Instance();
                            if (am->GetActionStatus(ActionType.BuddyAction, configuration.SavedChocoboStance) == 0)
                            {
                                am->UseAction(ActionType.BuddyAction, configuration.SavedChocoboStance);
                                if (configuration.DebugLogging)
                                {
                                    Service.PluginLog.Info($"[ChocoboStance] Restored stance to {configuration.SavedChocoboStance}");
                                }
                            }
                        }
                        stanceRestored = true;
                    }

                    // Detect user-initiated stance changes (after we've already restored or if no restore was needed)
                    if (stanceRestored && activeCommand != previousActiveCommand && activeCommand != 0 && previousActiveCommand != 0)
                    {
                        configuration.SavedChocoboStance = activeCommand;
                        configuration.Save();
                        if (configuration.DebugLogging)
                        {
                            Service.PluginLog.Info($"[ChocoboStance] User changed stance, saved: {activeCommand}");
                        }
                    }
                }
                else
                {
                    // Chocobo not active - reset tracking
                    chocoboAppearedAt = DateTime.MinValue;
                    stanceRestored = false;
                }

                previousTimeLeft = timeLeft;
                previousActiveCommand = activeCommand;
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
