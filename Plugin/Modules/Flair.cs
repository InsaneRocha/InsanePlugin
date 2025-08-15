﻿using GameReaderCommon;
using Newtonsoft.Json.Linq;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime;
using System.Threading.Tasks;

namespace InsanePlugin
{
    public class FlairLookup
    {
        private readonly Dictionary<int, string> flairIdToCountryCode;

        public FlairLookup(JObject json)
        {
            flairIdToCountryCode = new Dictionary<int, string>();

            var flairs = json["flairs"] as JArray;
            if (flairs != null)
            {
                foreach (var flair in flairs)
                {
                    int id = (int)flair["flair_id"];
                    string countryCode = flair["country_code"]?.ToString();
                    if (countryCode != null)
                    {
                        flairIdToCountryCode[id] = countryCode;
                    }
                }
            }

            // Special handling for flair_id 1 and 2
            flairIdToCountryCode[1] = "iracing";
            flairIdToCountryCode[2] = "iracing";
        }

        public string GetCountryCode(int flairId)
        {
            return flairIdToCountryCode.TryGetValue(flairId, out var code) ? code : string.Empty;
        }
    }

    public class FlairModule : PluginModuleBase
    {
        private RemoteJsonFile _flairs = new RemoteJsonFile("https://raw.githubusercontent.com/fixfactory/bo2-official-overlays/main/Data/Flairs.json");
        private FlairLookup _flairLookup = null;

        public override int UpdatePriority => 20;

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            _flairs.LoadAsync();
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {
            if (_flairs.Json != null && _flairLookup == null)
            {
                _flairLookup = new FlairLookup(_flairs.Json);
            }
        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
        }

        public string GetCountryCode(int flairId)
        {
            if (_flairLookup == null)
            {
                return string.Empty;
            }
            return _flairLookup.GetCountryCode(flairId);
        }
    }
}
