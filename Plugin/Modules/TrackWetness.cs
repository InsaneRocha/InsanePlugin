using GameReaderCommon;
using SimHub.Plugins;
using System.ComponentModel;

namespace InsanePlugin
{
    public class TrackWetnessSettings : ModuleSettings
    {
        public bool HideWhenZero { get; set; } = true;

        public int BackgroundOpacity { get; set; } = 0;
    }

    public class TrackWetnessModule : PluginModuleBase
    {
        public TrackWetnessSettings Settings { get; set; }

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            Settings = plugin.ReadCommonSettings<TrackWetnessSettings>("TrackWetnessSettings", () => new TrackWetnessSettings());
            plugin.AttachDelegate(name: "TrackWetness.HideWhenZero", valueProvider: () => Settings.HideWhenZero);
            plugin.AttachDelegate(name: "TrackWetness.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {

        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
            plugin.SaveCommonSettings("TrackWetnessSettings", Settings);
        }
    }
}
