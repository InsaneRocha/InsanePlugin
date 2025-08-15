using GameReaderCommon;
using SimHub.Plugins;
using System;

namespace InsanePlugin
{
    public class PushToPass : PluginModuleBase
    {
        public bool Enabled { get; set; } = false;
        public bool Activated { get; set; } = false;
        public int Count { get; set; } = 0;
        public float TimeLeft { get; set; } = 0.0f;
        public float Cooldown { get; set; } = 0.0f;
        public float TotalCooldown { get; set; } = 0.0f;

        private TrackModule _trackModule = null;
        private CarModule _carModule = null;
        private bool _wasActivated = false;
        private DateTime _activatedTime = DateTime.MinValue;
        private DateTime _deactivatedTime = DateTime.MinValue;

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            _trackModule = plugin.GetModule<TrackModule>();
            _carModule = plugin.GetModule<CarModule>();

            plugin.AttachDelegate(name: "PushToPass.Enabled", valueProvider: () => Enabled);
            plugin.AttachDelegate(name: "PushToPass.Activated", valueProvider: () => Activated);
            plugin.AttachDelegate(name: "PushToPass.Count", valueProvider: () => Count);
            plugin.AttachDelegate(name: "PushToPass.TimeLeft", valueProvider: () => TimeLeft);
            plugin.AttachDelegate(name: "PushToPass.Cooldown", valueProvider: () => Cooldown);
            plugin.AttachDelegate(name: "PushToPass.TotalCooldown", valueProvider: () => TotalCooldown);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {
            if (_carModule?.HasPushToPassCount ?? false)
            {
                Enabled = true;
                Activated = (bool)data.NewData.PushToPassActive;
                RawDataHelper.TryGetTelemetryData<int>(ref data, out int p2pCount, "P2P_Count");
                Count = p2pCount;
                TimeLeft = 0;
                Cooldown = 0;
                TotalCooldown = 0;
            }
            else if (_carModule?.HasPushToPassTimer ?? false)
            {
                // For Super Formula, the telemetry only exposes 'activated' and 'timeLeft'.
                // We have to generate the other values.
                Enabled = true;
                Activated = (bool)data.NewData.PushToPassActive;
                RawDataHelper.TryGetTelemetryData<int>(ref data, out int p2pCount, "P2P_Count");
                TimeLeft = p2pCount;

                // Total cooldown time at that track in seconds
                TotalCooldown = _trackModule?.PushToPassCooldown ?? 0.0f;

                // Check if Push-to-Pass was toggled
                if (Activated != _wasActivated)
                {
                    _wasActivated = Activated;

                    if (Activated)
                    {
                        _activatedTime = DateTime.Now;
                        _deactivatedTime = DateTime.MinValue;
                    }
                    else
                    {
                        _activatedTime = DateTime.MinValue;
                        _deactivatedTime = DateTime.Now;
                    }
                }

                // Update Cooldown timer
                if (!Activated)
                {
                    // Cooldown is only used in race
                    bool isRace = data.NewData.SessionTypeName.IndexOf("Race") != -1;
                    if (isRace)
                    {
                        RawDataHelper.TryGetTelemetryData<int>(ref data, out int enterExitReset, "EnterExitReset");
                        RawDataHelper.TryGetTelemetryData<int>(ref data, out int sessionState, "SessionState");

                        bool cancelCooldown = enterExitReset != 2 && sessionState == 4;
                        if (cancelCooldown)
                        {
                            _deactivatedTime = DateTime.MinValue;
                            Cooldown = 0;
                        }

                        if (_deactivatedTime != DateTime.MinValue)
                        {
                            TimeSpan deactivatedDuration = DateTime.Now - _deactivatedTime;
                            if (deactivatedDuration.TotalSeconds < TotalCooldown)
                            {
                                Cooldown = (float)(TotalCooldown - deactivatedDuration.TotalSeconds);
                            }
                            else
                            {
                                Cooldown = 0;
                                _deactivatedTime = DateTime.MinValue;
                            }
                        }
                    }
                    else
                    {
                        Cooldown = 0;
                    }
                }
            }
            else
            {
                // Car does not support PushToPass
                Enabled = false;
                Activated = false;
                Count = 0;
                TimeLeft = 0;
                Cooldown = 0;
                TotalCooldown = 0;
            }
        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {

        }
    }
}
