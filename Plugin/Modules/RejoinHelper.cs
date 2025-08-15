using GameReaderCommon;
using SimHub.Plugins;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace InsanePlugin
{
    public class RejoinHelperSettings : ModuleSettings
    {
        public bool Enabled { get; set; } = true;
        public ModuleSettingFloat MinimumClearGap { get; set; } = new ModuleSettingFloat(3.5f);
        public ModuleSettingFloat MinimumCareGap { get; set; } = new ModuleSettingFloat(1.5f);
        public int MinSpeed { get; set; } = 35;

        // Legacy properties for backwards compatibility (saved pre 3.0)
        public string MinClearGapString { get => MinimumClearGap.ValueString; set => MinimumClearGap.ValueString = value; }
        public string MinCareGapString { get => MinimumCareGap.ValueString; set => MinimumCareGap.ValueString = value; }
    }

    public class RejoinHelperModule : PluginModuleBase
    {
        private SessionModule _sessionModule = null;

        public RejoinHelperSettings Settings { get; set; }
        public bool Visible { get; set; } = false;
        public double Gap { get; set; } = 0;
        public string State { get; set; } = string.Empty;
        public double ColorPct { get; set; } = 0;

        public const string StateClear = "Clear";
        public const string StateCare = "Care";
        public const string StateYield = "Yield";

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            _sessionModule = plugin.GetModule<SessionModule>();

            Settings = plugin.ReadCommonSettings<RejoinHelperSettings>("RejoinHelperSettings", () => new RejoinHelperSettings());
            plugin.AttachDelegate(name: "RejoinHelper.Enabled", valueProvider: () => Settings.Enabled);
            plugin.AttachDelegate(name: "RejoinHelper.MinClearGap", valueProvider: () => Settings.MinimumClearGap.Value);
            plugin.AttachDelegate(name: "RejoinHelper.MinCareGap", valueProvider: () => Settings.MinimumCareGap.Value);
            plugin.AttachDelegate(name: "RejoinHelper.MinSpeed", valueProvider: () => Settings.MinSpeed);
            plugin.AttachDelegate(name: "RejoinHelper.Visible", valueProvider: () => Visible);
            plugin.AttachDelegate(name: "RejoinHelper.Gap", valueProvider: () => Gap);
            plugin.AttachDelegate(name: "RejoinHelper.State", valueProvider: () => State);
            plugin.AttachDelegate(name: "RejoinHelper.ColorPct", valueProvider: () => ColorPct);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {
            dynamic raw = data.NewData.GetRawDataObject();
            if (raw == null) return;

            // Wait for race to be started for a few seconds not to trigger on a standing start
            if (!Settings.Enabled || (_sessionModule.Race && (_sessionModule.RaceFinished || _sessionModule.RaceTimer < 3)))
            {
                Visible = false;
                Gap = 0;
                State = StateClear;
                ColorPct = 100;
            }
            else
            {
                int trackSurface = 0;
                try { trackSurface = (int)raw.Telemetry["PlayerTrackSurface"]; } catch { }

                bool isSlow = data.NewData.SpeedKmh < Settings.MinSpeed;
                Visible = isSlow || trackSurface == 0;

                List<Opponent> opponents = data.NewData.OpponentsBehindOnTrack;
                if (opponents.Count > 0)
                {
                    Gap = opponents[0].RelativeGapToPlayer ?? 0;
                }
                else
                {
                    Gap = 0;
                }

                if (Gap <= 0)
                {
                    State = StateClear;
                    ColorPct = 100;
                }
                else
                {
                    if (Gap >= Settings.MinimumClearGap.Value)
                    {
                        State = StateClear;
                        ColorPct = 100;
                    }
                    else if (Gap >= Settings.MinimumCareGap.Value)
                    {
                        State = StateCare;
                        double ratio = (Gap - Settings.MinimumCareGap.Value) / (Settings.MinimumClearGap.Value - Settings.MinimumCareGap.Value);
                        ColorPct = ((100 - 50) * ratio) + 50;
                    }
                    else
                    {
                        State = StateYield;
                        double ratio = Gap / Settings.MinimumClearGap.Value;
                        ColorPct = 50 * ratio;
                    }
                }
            }
        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
            plugin.SaveCommonSettings("RejoinHelperSettings", Settings);
        }
    }
}
