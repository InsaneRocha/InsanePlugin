using GameReaderCommon;
using Newtonsoft.Json.Linq;
using SimHub.Plugins;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime;
using System.Threading.Tasks;

namespace InsanePlugin
{
    public class TrackModule : PluginModuleBase
    {
        private RemoteJsonFile _trackInfo = new RemoteJsonFile("https://raw.githubusercontent.com/fixfactory/bo2-official-overlays/main/Data/TrackInfo.json");
        private string _lastTrackId = string.Empty;

        public int PushToPassCooldown { get; set; } = 0;
        public float QualStartTrackPct { get; set; } = 0.0f;
        public float RaceStartTrackPct { get; set; } = 0.0f;
        public string TrackType { get; set; } = string.Empty;
        public override int UpdatePriority => 20;

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            _trackInfo.LoadAsync();

            plugin.AttachDelegate(name: "Track.QualStartTrackPct", valueProvider: () => QualStartTrackPct);
            plugin.AttachDelegate(name: "Track.RaceStartTrackPct", valueProvider: () => RaceStartTrackPct);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {
            if (_trackInfo.Json == null) return;
            if (data.NewData.TrackId == _lastTrackId) return;
            _lastTrackId = data.NewData.TrackId;
            
            dynamic raw = data.NewData.GetRawDataObject();
            if (raw == null) return;

            if (data.NewData.TrackId.Length == 0)
            {
                PushToPassCooldown = 0;
                QualStartTrackPct = 0.0f;
                RaceStartTrackPct = 0.0f;
                TrackType = string.Empty;
                return;
            }

            JToken track = _trackInfo.Json[data.NewData.TrackId];

            if (data.NewData.CarId == "superformulasf23 toyota" || data.NewData.CarId == "superformulasf23 honda")
            {
                PushToPassCooldown = track?["pushToPassCooldown_SF23"]?.Value<int>() ?? 100;
            }
            else
            {
                PushToPassCooldown = 0;
            }

            QualStartTrackPct = track?["qualStartTrackPct"]?.Value<float>() ?? 0.0f;
            RaceStartTrackPct = track?["raceStartTrackPct"]?.Value<float>() ?? 0.0f;

            try { TrackType = raw.AllSessionData["WeekendInfo"]["TrackType"]; } catch { Debug.Assert(false); }
        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
        }
    }
}
