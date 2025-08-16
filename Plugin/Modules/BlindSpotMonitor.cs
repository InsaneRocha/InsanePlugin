using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.ComponentModel;

namespace InsanePlugin
{
    public class BlindSpotMonitorSettings : ModuleSettings
    {
        public bool Enabled { get; set; } = false;
    }

    public class BlindSpotMonitorModule : PluginModuleBase
    {
        private SpotterModule _spotterModule = null;

        public BlindSpotMonitorSettings Settings { get; set; }
        public bool Visible { get; set; } = false;

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            _spotterModule = plugin.GetModule<SpotterModule>();

            Settings = plugin.ReadCommonSettings<BlindSpotMonitorSettings>("BlindSpotMonitorSettings", () => new BlindSpotMonitorSettings());
            plugin.AttachDelegate(name: "BlindSpotMonitor.Enabled", valueProvider: () => Settings.Enabled);
            plugin.AttachDelegate(name: "BlindSpotMonitor.Visible", valueProvider: () => Visible);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {
            if (!Settings.Enabled)
            {
                Visible = false;
            }
            else
            {
                Visible = _spotterModule.OverlapAhead < 0 || _spotterModule.OverlapBehind > 0;
            }
        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
            plugin.SaveCommonSettings("BlindSpotMonitorSettings", Settings);
        }
    }
}
