using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace dontforget
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Scholar { get; set; } = true;
        public bool Summoner { get; set; } = true;
        [Obsolete("Renamed to SummonInCombatAfterDeath")]
        public bool SummonInCombat { get; set; } = true;
        public bool SummonInCombatAfterDeath { get; set; } = true;
        public bool Peloton { get; set; } = true;
        public bool AutoSprint { get; set; } = true;
        public bool DebugLogging { get; set; } = false;

        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;

            // Migrate old setting to new setting on first load
            if (this.Version == 0)
            {
                this.SummonInCombatAfterDeath = this.SummonInCombat;
                this.Version = 1;
                this.Save();
            }
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
