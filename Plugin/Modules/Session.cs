using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Diagnostics;

namespace InsanePlugin
{
    public class SessionState
    {
        private double _lastSessionTime = double.MaxValue;
        private Guid _lastSessionId = Guid.Empty;

        public TimeSpan SessionTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan DeltaTime { get; private set; } = TimeSpan.Zero;
        public bool SessionChanged { get; private set; } = true;
        
        public void Update(ref GameData data)
        {
            RawDataHelper.TryGetTelemetryData<double>(ref data, out double sessionTime, "SessionTime");
            SessionTime = TimeSpan.FromSeconds(sessionTime);
            DeltaTime = TimeSpan.FromSeconds(Math.Max(sessionTime - _lastSessionTime, 0));

            SessionChanged = (sessionTime < _lastSessionTime || data.SessionId != _lastSessionId);
            
            _lastSessionTime = sessionTime;
            _lastSessionId = data.SessionId;
        }
    }

    public class SessionModule : PluginModuleBase
    {
        private string _lastSessionTypeName = string.Empty;
        private bool _raceFinishedForPlayer = false;
        private double? _lastTrackPct = null;
        private TimeSpan _raceStartedTime = TimeSpan.Zero;

        public SessionState State { get; internal set; } = new SessionState();
        public bool Race { get; internal set; } = false;
        public bool Qual { get; internal set; } = false;
        public bool Practice { get; internal set; } = false;
        public bool Offline { get; internal set; } = false;
        public bool ReplayPlaying { get; internal set; } = false;
        public bool SessionScreen { get; internal set; } = false;
        public bool RaceStarted { get; internal set; } = false;
        public bool RaceFinished { get; internal set; } = false;
        public TimeSpan SessionTimeTotal { get; internal set; } = TimeSpan.Zero;
        public int SessionLapsTotal { get; internal set; } = 0;
        public double RaceTimer { get; internal set; } = 0;
        public bool JoinedRaceInProgress { get; internal set; } = false;
        public bool Oval { get; internal set; } = false;
        public bool StandingStart { get; internal set; } = false;
        public bool ShortParadeLap { get; internal set; } = false;
        public double MaxFuelPct { get; internal set; } = 1.0;
        public bool TeamRacing { get; internal set; } = false;

        public override int UpdatePriority => 10;

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            plugin.AttachDelegate(name: "Session.Race", valueProvider: () => Race);
            plugin.AttachDelegate(name: "Session.Qual", valueProvider: () => Qual);
            plugin.AttachDelegate(name: "Session.Practice", valueProvider: () => Practice);
            plugin.AttachDelegate(name: "Session.Offline", valueProvider: () => Offline);
            plugin.AttachDelegate(name: "Session.ReplayPlaying", valueProvider: () => ReplayPlaying);
            plugin.AttachDelegate(name: "Session.SessionScreen", valueProvider: () => SessionScreen);
            plugin.AttachDelegate(name: "Session.RaceStarted", valueProvider: () => RaceStarted);
            plugin.AttachDelegate(name: "Session.RaceFinished", valueProvider: () => RaceFinished);
            plugin.AttachDelegate(name: "Session.RaceTimer", valueProvider: () => RaceTimer);
            plugin.AttachDelegate(name: "Session.Oval", valueProvider: () => Oval);
            plugin.AttachDelegate(name: "Session.StandingStart", valueProvider: () => StandingStart);
            plugin.AttachDelegate(name: "Session.TeamRacing", valueProvider: () => TeamRacing);
            plugin.AttachDelegate(name: "Session.LapsTotal", valueProvider: () => SessionLapsTotal);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {
            dynamic raw = data.NewData.GetRawDataObject();
            if (raw == null) return;

            State.Update(ref data);

            bool sessionChanged = State.SessionChanged || data.NewData.SessionTypeName != _lastSessionTypeName;
            if (sessionChanged)
            {
                Race = data.NewData.SessionTypeName.IndexOf("Race") != -1;
                Qual = data.NewData.SessionTypeName.IndexOf("Qual") != -1;

                Practice = data.NewData.SessionTypeName.IndexOf("Practice") != -1 ||
                    data.NewData.SessionTypeName.IndexOf("Warmup") != -1 ||
                    data.NewData.SessionTypeName.IndexOf("Testing") != -1;

                Offline = data.NewData.SessionTypeName.IndexOf("Offline") != -1;

                string category = string.Empty;
                try { category = raw.AllSessionData["WeekendInfo"]["Category"]; } catch { Debug.Assert(false);  }
                Oval = category == "Oval" || category == "DirtOval";

                int standingStart = 0;
                try { standingStart = int.Parse(raw.AllSessionData["WeekendInfo"]["WeekendOptions"]["StandingStart"]); } catch { Debug.Assert(false); }
                StandingStart = standingStart == 1;

                int shortParadeLap = 0;
                try { shortParadeLap = int.Parse(raw.AllSessionData["WeekendInfo"]["WeekendOptions"]["ShortParadeLap"]); } catch { Debug.Assert(false); }
                ShortParadeLap = shortParadeLap == 1;

                try { MaxFuelPct = double.Parse(raw.AllSessionData["DriverInfo"]["DriverCarMaxFuelPct"]); } catch { Debug.Assert(false); }

                RawDataHelper.TryGetSessionData<string>(ref data, out string teamRacing, "WeekendInfo", "TeamRacing");
                TeamRacing = teamRacing == "1";

                RawDataHelper.TryGetTelemetryData<int>(ref data, out int sessionTimeTotal, "SessionTimeTotal");
                SessionTimeTotal = TimeSpan.FromSeconds(sessionTimeTotal);

                RawDataHelper.TryGetTelemetryData<int>(ref data, out int totalLaps, "SessionLapsTotal");
                SessionLapsTotal = (totalLaps > 0) && (totalLaps < 20000) ? totalLaps : 0;

                _lastSessionTypeName = data.NewData.SessionTypeName;
            }

            // Determine if replay is playing.
            // There's a short moment when loading into a session when isReplayPlaying is false
            // but position or trackSurface is -1.
            // IsReplayPlaying is false when spotting.
            bool isReplayPLaying = false;
            try { isReplayPLaying = (bool)raw.Telemetry["IsReplayPlaying"]; } catch { }

            int position = -1;
            try { position = (int)raw.Telemetry["PlayerCarPosition"]; } catch { }

            int trackSurface = -1;
            try { trackSurface = (int)raw.Telemetry["PlayerTrackSurface"]; } catch { }

            ReplayPlaying = isReplayPLaying || position < 0 || trackSurface < 0;

            // Determine if Session Screen is active (out of car)
            // Remains true when spotting.
            RawDataHelper.TryGetTelemetryData<int>(ref data, out int sessionScreen, "CamCameraState");
            SessionScreen = (sessionScreen & 0x0001) != 0; // irsdk_IsSessionScreen

            // Determine if race started
            int sessionState = 0;
            try { sessionState = (int)raw.Telemetry["SessionState"]; } catch { }
            RaceStarted = Race && sessionState >= 4;

            // Determine if we joined a race session in progress.
            // This will also be true when stepping backwards in a SimHub replay.
            if (RaceStarted)
            {
                if (sessionChanged)
                {
                    JoinedRaceInProgress = true;
                }
            }
            else
            {
                JoinedRaceInProgress = false;
            }

            // Determine if race finished for the player
            if (!Race || State.SessionChanged)
            {
                // Reset when changing/restarting session
                _lastTrackPct = null;
                _raceFinishedForPlayer = false;
                RaceFinished = false;
            }
            else
            {
                if (_raceFinishedForPlayer)
                {
                    // Race finished
                    RaceFinished = true;
                }
                else if (data.NewData.Flag_Checkered != 1)
                {
                    // Checkered flag is not shown
                    RaceFinished = false;
                }
                else if (!_lastTrackPct.HasValue || _lastTrackPct.Value <= data.NewData.TrackPositionPercent)
                {
                    // Heading toward the checkered flag
                    _lastTrackPct = data.NewData.TrackPositionPercent;
                    RaceFinished = false;
                }
                else
                {
                    // Crossed the line with the checkered flag
                    _raceFinishedForPlayer = true;
                    RaceFinished = true;
                }
            }

            // Update race timer
            if (RaceStarted)
            {
                // Freeze timer when race is finished
                if (!RaceFinished)
                {
                    if (_raceStartedTime <= TimeSpan.Zero)
                    {
                        _raceStartedTime = State.SessionTime;
                    }

                    RaceTimer = (State.SessionTime - _raceStartedTime).TotalSeconds;
                }
            }
            else
            {
                RaceTimer = 0;
                _raceStartedTime = TimeSpan.Zero;
            }
        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
        }
    }
}
