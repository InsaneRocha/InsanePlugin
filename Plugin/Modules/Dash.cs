using GameReaderCommon;
using SimHub.Plugins;
using System.ComponentModel;

namespace InsanePlugin
{
    public class DashSettings : ModuleSettings
    {
        public int BackgroundOpacity { get; set; } = 60;
    }

    public class DashModule : PluginModuleBase
    {
        public DashSettings Settings { get; set; }

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            Settings = plugin.ReadCommonSettings<DashSettings>("DashSettings", () => new DashSettings());
            plugin.AttachDelegate(name: "Dash.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {

        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
            plugin.SaveCommonSettings("DashSettings", Settings);
        }
    }
}
