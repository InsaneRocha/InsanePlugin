using GameReaderCommon;
using SimHub.Plugins;
using System.ComponentModel;

namespace InsanePlugin
{
    public class WindSettings : ModuleSettings
    {
        public bool RotateWithCar { get; set; } = true;

        public int BackgroundOpacity { get; set; } = 0;
    }

    public class WindModule : PluginModuleBase
    {
        public WindSettings Settings { get; set; }

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            Settings = plugin.ReadCommonSettings<WindSettings>("WindSettings", () => new WindSettings());
            plugin.AttachDelegate(name: "Wind.RotateWithCar", valueProvider: () => Settings.RotateWithCar);
            plugin.AttachDelegate(name: "Wind.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {

        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
            plugin.SaveCommonSettings("WindSettings", Settings);
        }
    }
}
