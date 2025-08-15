using GameReaderCommon;
using SimHub.Plugins;
using System.ComponentModel;

namespace InsanePlugin
{
    public class LaunchAssistSettings : ModuleSettings
    {
        public int BackgroundOpacity { get; set; } = 60;
    }

    public class LaunchAssistModule : PluginModuleBase
    {
        public LaunchAssistSettings Settings { get; set; }

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            Settings = plugin.ReadCommonSettings<LaunchAssistSettings>("LaunchAssistSettings", () => new LaunchAssistSettings());
            plugin.AttachDelegate(name: "LaunchAssist.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {

        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
            plugin.SaveCommonSettings("LaunchAssistSettings", Settings);
        }
    }
}
