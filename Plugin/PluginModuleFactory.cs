using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InsanePlugin
{
    public abstract class PluginModuleBase
    {
        public virtual int UpdatePriority => 100;
        public abstract void Init(PluginManager pluginManager, InsanePluginMain plugin);
        public abstract void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data);
        public abstract void End(PluginManager pluginManager, InsanePluginMain plugin);
    }

    public static class PluginModuleFactory
    {
        public static Dictionary<string, PluginModuleBase> CreateAllPluginModules()
        {
            var moduleTypes = Assembly.GetExecutingAssembly()
                                      .GetTypes()
                                      .Where(t => typeof(PluginModuleBase).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                                      .ToList();

            var modules = new Dictionary<string, PluginModuleBase>();
            foreach (var type in moduleTypes)
            {
                var instance = Activator.CreateInstance(type) as PluginModuleBase;
                if (instance != null)
                {
                    modules[type.Name] = instance;
                }
            }

            return modules;
        }
    }
}