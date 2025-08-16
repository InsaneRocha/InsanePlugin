using GameReaderCommon;
using SimHub.Plugins;
using System.ComponentModel;

namespace InsanePlugin
{
    public class PrecipitationSettings : ModuleSettings
    {
        public bool HideWhenZero { get; set; } = true;

        public int BackgroundOpacity { get; set; } = 0;
    }

    public class PrecipitationModule : PluginModuleBase
    {
        public PrecipitationSettings Settings { get; set; }

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            Settings = plugin.ReadCommonSettings<PrecipitationSettings>("PrecipitationSettings", () => new PrecipitationSettings());
            plugin.AttachDelegate(name: "Precipitation.HideWhenZero", valueProvider: () => Settings.HideWhenZero);
            plugin.AttachDelegate(name: "Precipitation.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {

        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
            plugin.SaveCommonSettings("PrecipitationSettings", Settings);
        }
    }
}
