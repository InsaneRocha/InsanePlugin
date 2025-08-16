using GameReaderCommon;
using SimHub.Plugins;
using System.ComponentModel;

namespace InsanePlugin
{
    public class GeneralSettings : ModuleSettings
    {
        public bool ClockFormat24h { get; set; } = true;
    }

    public class GeneralModule : PluginModuleBase
    {
        public GeneralSettings Settings { get; set; }

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            // There was an issue with loading settings stored pre-3.0, so we need to specify a new settings key
            Settings = plugin.ReadCommonSettings<GeneralSettings>("GeneralSettings_3.1", () => new GeneralSettings());
            plugin.AttachDelegate(name: "ClockFormat24h", valueProvider: () => Settings.ClockFormat24h);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {

        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
            plugin.SaveCommonSettings("GeneralSettings_3.1", Settings);
        }
    }
}
