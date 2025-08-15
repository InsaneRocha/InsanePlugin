using GameReaderCommon;
using SimHub.Plugins;
using System.ComponentModel;

namespace InsanePlugin
{
    public class TrackMapSettings : ModuleSettings
    {
        public bool HideInReplay { get; set; } = true;
        public int DotRadius { get; set; } = 20;
        public int FontSize { get; set; } = 20;
        public int LineThickness { get; set; } = 0;
        public int BackgroundOpacity { get; set; } = 0;
    }

    public class TrackMapModule : PluginModuleBase
    {
        public TrackMapSettings Settings { get; set; }

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            Settings = plugin.ReadCommonSettings<TrackMapSettings>("TrackMapSettings", () => new TrackMapSettings());
            plugin.AttachDelegate(name: "TrackMap.HideInReplay", valueProvider: () => Settings.HideInReplay);
            plugin.AttachDelegate(name: "TrackMap.DotRadius", valueProvider: () => Settings.DotRadius);
            plugin.AttachDelegate(name: "TrackMap.FontSize", valueProvider: () => Settings.FontSize);
            plugin.AttachDelegate(name: "TrackMap.LineThickness", valueProvider: () => Settings.LineThickness);
            plugin.AttachDelegate(name: "TrackMap.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {

        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
            plugin.SaveCommonSettings("TrackMapSettings", Settings);
        }
    }
}
