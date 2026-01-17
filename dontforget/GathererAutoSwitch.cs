using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System;

namespace dontforget
{
    public class GathererAutoSwitch : IDisposable
    {
        private readonly Configuration configuration;

        public GathererAutoSwitch(Configuration configuration)
        {
            this.configuration = configuration;
            Service.ChatGui.ChatMessage += OnChatMessage;
        }

        private unsafe void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (!this.configuration.AutoSwitchGatherer) return;

            // Check for "Unable to X. Current class not set to X" error messages (type 2108)
            if ((int)type != 2108) return;

            var text = message.TextValue.ToLowerInvariant();

            if (this.configuration.DebugLogging)
            {
                Service.PluginLog.Info($"[GathererAutoSwitch] Error message: {text}");
            }

            // Check for miner-related error (ClassJob 16)
            if (text.Contains("miner"))
            {
                var gearsetId = FindGearsetForClassJob(16);
                if (gearsetId >= 0)
                {
                    Service.PluginLog.Info($"[GathererAutoSwitch] Switching to Miner gearset {gearsetId + 1}");
                    var gearsetModule = RaptureGearsetModule.Instance();
                    gearsetModule->EquipGearset(gearsetId);
                }
                return;
            }

            // Check for botanist-related error (ClassJob 17)
            if (text.Contains("botanist"))
            {
                var gearsetId = FindGearsetForClassJob(17);
                if (gearsetId >= 0)
                {
                    Service.PluginLog.Info($"[GathererAutoSwitch] Switching to Botanist gearset {gearsetId + 1}");
                    var gearsetModule = RaptureGearsetModule.Instance();
                    gearsetModule->EquipGearset(gearsetId);
                }
                return;
            }
        }

        private unsafe int FindGearsetForClassJob(byte classJobId)
        {
            var gearsetModule = RaptureGearsetModule.Instance();
            for (int i = 0; i < 100; i++)
            {
                var gearset = gearsetModule->GetGearset(i);
                if (gearset != null && gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) && gearset->ClassJob == classJobId)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Dispose()
        {
            Service.ChatGui.ChatMessage -= OnChatMessage;
        }
    }
}
