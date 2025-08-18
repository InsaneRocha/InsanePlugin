//using GameReaderCommon;
//using SimHub.Plugins;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Globalization;

//namespace InsanePlugin
//{
//    public class SpotterSettings : ModuleSettings
//    {
//        public bool Enabled { get; set; } = true;
//        public ModuleSettingFloat DistanceThreshold { get; set; } = new ModuleSettingFloat(5.5f);
//        public int Height { get; set; } = 129;
//        public int MinHeight { get; set; } = 15;
//        public int Width { get; set; } = 12;
//        public int Border { get; set; } = 3;
//        public int Spacing { get; set; } = 100;

//        public string ThresholdString { get => DistanceThreshold.ValueString; set => DistanceThreshold.ValueString = value; }
//    }

//    public class SpotterModule : PluginModuleBase
//    {
//        public SpotterSettings Settings { get; set; }

//        public double OverlapAhead { get; internal set; } = 0;
//        public double OverlapBehind { get; internal set; } = 0;

//        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
//        {
//            Settings = plugin.ReadCommonSettings<SpotterSettings>("SpotterSettings", () => new SpotterSettings());
//            plugin.AttachDelegate(name: "Spotter.Enabled", valueProvider: () => Settings.Enabled);
//            plugin.AttachDelegate(name: "Spotter.Threshold", valueProvider: () => Settings.DistanceThreshold.Value);
//            plugin.AttachDelegate(name: "Spotter.Height", valueProvider: () => Settings.Height);
//            plugin.AttachDelegate(name: "Spotter.MinHeight", valueProvider: () => Settings.MinHeight);
//            plugin.AttachDelegate(name: "Spotter.Width", valueProvider: () => Settings.Width);
//            plugin.AttachDelegate(name: "Spotter.Border", valueProvider: () => Settings.Border);
//            plugin.AttachDelegate(name: "Spotter.Spacing", valueProvider: () => Settings.Spacing);
//            plugin.AttachDelegate(name: "Spotter.OverlapAhead", valueProvider: () => OverlapAhead);
//            plugin.AttachDelegate(name: "Spotter.OverlapBehind", valueProvider: () => OverlapBehind);
//        }

//        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
//        {
//            UpdateOverlapAhead(ref data);
//            UpdateOverlapBehind(ref data);
//        }

//        public void UpdateOverlapAhead(ref GameData data)
//        {
//            (double dist0, double dist1) = GetNearestDistances(data.NewData.OpponentsAheadOnTrack);
//            double overlap = 0;
//            if (dist0 < 0 && dist0 >= -Settings.DistanceThreshold.Value)
//            {
//                overlap = dist0;
//            }

//            if (dist1 < 0 && dist1 >= -Settings.DistanceThreshold.Value)
//            {
//                overlap = Math.Min(overlap, dist1);
//            }

//            OverlapAhead = overlap;
//        }

//        public void UpdateOverlapBehind(ref GameData data)
//        {
//            (double dist0, double dist1) = GetNearestDistances(data.NewData.OpponentsBehindOnTrack);
//            double overlap = 0;
//            if (dist0 > 0 && dist0 <= Settings.DistanceThreshold.Value)
//            {
//                overlap = dist0;
//            }

//            if (dist1 > 0 && dist1 <= Settings.DistanceThreshold.Value)
//            {
//                overlap = Math.Max(overlap, dist1);
//            }

//            OverlapBehind = overlap;
//        }

//        public override void End(PluginManager pluginManager, InsanePlugin plugin)
//        {
//            plugin.SaveCommonSettings("SpotterSettings", Settings);
//        }

//        public (double dist0, double dist1) GetNearestDistances(List<Opponent> opponents)
//        {
//            double dist0 = 0, dist1 = 0;
//            if (opponents.Count > 0) dist0 = opponents[0].RelativeDistanceToPlayer ?? 0;
//            if (opponents.Count > 1) dist1 = opponents[1].RelativeDistanceToPlayer ?? 0;
//            return (dist0, dist1);
//        }
//    }
//}
