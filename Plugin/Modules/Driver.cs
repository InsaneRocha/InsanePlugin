using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;

namespace InsanePlugin
{
    using OpponentsWithDrivers = List<(Opponent, Driver)>;

    public class HighlightedDriverSettings : ModuleSettings
    {
        public bool HideWhenInCar { get; set; } = true;
        public bool CarBrandVisible { get; set; } = true;
        public int Width { get; set; } = 30;
        public int BackgroundOpacity { get; set; } = 60;
    }

    public class AverageLapTime
    {
        private readonly Queue<TimeSpan> _lapTimes = new Queue<TimeSpan>();
        private readonly int _maxLapCount;
        private int _currentLap = -1;

        public AverageLapTime(int lapCount)
        {
            if (lapCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lapCount), "Lap count must be greater than zero.");
            }
            _maxLapCount = lapCount;
        }

        public void AddLapTime(int currentLap, TimeSpan lapTime)
        {
            if (currentLap == _currentLap)
                return;

            if (lapTime <= TimeSpan.Zero)
                return;

            if (_lapTimes.Count == _maxLapCount)
            {
                _lapTimes.Dequeue();
            }
            _lapTimes.Enqueue(lapTime);
            _currentLap = currentLap;
        }

        public TimeSpan GetAverageLapTime()
        {
            if (_lapTimes.Count == 0)
            {
                return TimeSpan.Zero;
            }

            long averageTicks = (long)_lapTimes.Average(ts => ts.Ticks);
            return new TimeSpan(averageTicks);
        }
    }

    public class Driver
    {
        // Index in the array AllSessionData["DriverInfo"]["Drivers"]
        public int DriverInfoIdx { get; set; } = -1;
        public int CarIdx { get; set; } = -1;
        public string CarId { get; set; } = "";
        public int FlairId { get; set; } = 0;
        public int CarClassId { get; set; } = 0;
        public int EnterPitLapUnconfirmed { get; set; } = -1;
        public int EnterPitLap { get; set; } = -1;
        public int ExitPitLap { get; set; } = -1;
        public bool OutLap { get; set; } = false;
        public DateTime InPitSince { get; set; } = DateTime.MinValue;
        public DateTime InPitBoxSince { get; set; } = DateTime.MinValue;
        public TimeSpan LastPitStopDuration { get; set; } = TimeSpan.Zero;
        public int StintLap { get; set; } = 0;
        public int PositionInClass { get; set; } = 0;
        public int QualPositionInClass { get; set; } = 0;
        public int LivePositionInClass { get; set; } = 0;
        public double LastCurrentLapHighPrecision { get; set; } = -1;
        public double CurrentLapHighPrecision { get; set; } = -1;
        public bool Towing { get; set; } = false;
        public DateTime TowingEndTime { get; set; } = DateTime.MinValue;
        public TimeSpan LastLapTime { get; set; } = TimeSpan.Zero;
        public TimeSpan BestLapTime { get; set; } = TimeSpan.Zero;
        public TimeSpan QualLapTime { get; set; } = TimeSpan.Zero;
        public AverageLapTime AvgLapTime { get; set; } = new AverageLapTime(3);
        public int LapsComplete { get; set; } = 0;
        public int JokerLapsComplete { get; set; } = 0;
        public int SessionFlags { get; set; } = 0;
        public int TeamIncidentCount { get; set; } = 0;
        public int IRating { get; set; } = 0;
        public int IRatingChange { get; set; } = 0;
    }

    public class ClassLeaderboard
    {
        public LeaderboardCarClassDescription CarClassDescription { get; set; } = null;
        public OpponentsWithDrivers Drivers { get; set; } = new OpponentsWithDrivers();
    }

    public class HighlightedDriver
    {
        public int CarIdx { get; set; } = -1;
        public string Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CarBrand { get; set; } = string.Empty;
        public string CarName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public int IRating { get; set; } = 0;
        public string License { get; set; } = string.Empty;
        public double SafetyRating { get; set; } = 0.0;
        public int LivePositionInClass { get; set; } = 0;
        public int CurrentLap { get; set; } = 0;
        public double CurrentLapHighPrecision { get; set; } = 0.0;
        public int TeamIncidentCount { get; set; } = 0;
        public string CarClassColor { get; set; } = string.Empty;
        public string CarClassTextColor { get; set; } = string.Empty;
        public TimeSpan LastLapTime { get; internal set; } = TimeSpan.Zero;
        public TimeSpan BestLapTime { get; internal set; } = TimeSpan.Zero;
    }

    public class DriverModule : PluginModuleBase
    {
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private TimeSpan _updateInterval = TimeSpan.FromMilliseconds(500);
        private TimeSpan _minTimeInPit = TimeSpan.FromMilliseconds(2500);

        private SessionModule _sessionModule = null;
        private CarModule _carModule = null;
        private FlairModule _flairModule = null;
        private StandingsModule _standingsModule = null;
        private RelativeModule _relativeModule = null;

        private SessionState _sessionState = new SessionState();
        private bool _qualResultsUpdated = false;

        public const int MaxDrivers = 64;

        // Key is car number
        public Dictionary<string, Driver> Drivers { get; private set; } = new Dictionary<string, Driver>();

        // Key is CarIdx
        public Dictionary<int, Driver> DriversByCarIdx { get; private set; } = new Dictionary<int, Driver>();

        public HighlightedDriverSettings HighlightedDriverSettings { get; set; }

        public int PlayerCarIdx { get; set; } = -1;
        public bool PlayerOutLap { get; internal set; } = false;
        public int PlayerStintLap { get; internal set; } = 0;
        public string PlayerNumber { get; internal set; } = "";
        public string PlayerCarBrand { get; internal set; } = "";
        public string PlayerCountryCode { get; internal set; } = "";
        public int PlayerPositionInClass { get; internal set; } = 0;
        public int PlayerLivePositionInClass { get; internal set; } = 0;
        public bool PlayerHadWhiteFlag { get; internal set; } = false;
        public bool PlayerHadCheckeredFlag { get; internal set; } = false;
        public TimeSpan PlayerLastLapTime { get; internal set; } = TimeSpan.Zero;
        public TimeSpan PlayerBestLapTime { get; internal set; } = TimeSpan.Zero;
        public double PlayerCurrentLapHighPrecision { get; set; } = -1;
        public int PlayerCurrentLap { get; set; } = 0;
        public int PlayerTeamIncidentCount { get; set; } = 0;
        public int PlayerIRatingChange { get; set; } = 0;

        public List<ClassLeaderboard> LiveClassLeaderboards { get; private set; } = new List<ClassLeaderboard>();

        public HighlightedDriver HighlightedDriver { get; private set; } = new HighlightedDriver();

        public override int UpdatePriority => 30;

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            _sessionModule = plugin.GetModule<SessionModule>();
            _carModule = plugin.GetModule<CarModule>();
            _flairModule = plugin.GetModule<FlairModule>();
            _standingsModule = plugin.GetModule<StandingsModule>();
            _relativeModule = plugin.GetModule<RelativeModule>();

            HighlightedDriverSettings = plugin.ReadCommonSettings<HighlightedDriverSettings>("HighlightedDriverSettings", () => new HighlightedDriverSettings());

            plugin.AttachDelegate(name: "Player.OutLap", valueProvider: () => PlayerOutLap);
            plugin.AttachDelegate(name: "Player.StintLap", valueProvider: () => PlayerStintLap);
            plugin.AttachDelegate(name: "Player.Number", valueProvider: () => PlayerNumber);
            plugin.AttachDelegate(name: "Player.CarBrand", valueProvider: () => PlayerCarBrand);
            plugin.AttachDelegate(name: "Player.CountryCode", valueProvider: () => PlayerCountryCode);
            plugin.AttachDelegate(name: "Player.PositionInClass", valueProvider: () => PlayerPositionInClass);
            plugin.AttachDelegate(name: "Player.LivePositionInClass", valueProvider: () => PlayerLivePositionInClass);
            plugin.AttachDelegate(name: "Player.LastLapTime", valueProvider: () => PlayerLastLapTime);
            plugin.AttachDelegate(name: "Player.BestLapTime", valueProvider: () => PlayerBestLapTime);
            plugin.AttachDelegate(name: "Player.CurrentLap", valueProvider: () => PlayerCurrentLap);
            plugin.AttachDelegate(name: "Player.TeamIncidentCount", valueProvider: () => PlayerTeamIncidentCount);
            plugin.AttachDelegate(name: "Player.iRatingChange", valueProvider: () => PlayerIRatingChange);
            plugin.AttachDelegate(name: "Highlighted.HideWhenInCar", valueProvider: () => HighlightedDriverSettings.HideWhenInCar);
            plugin.AttachDelegate(name: "Highlighted.CarBrandVisible", valueProvider: () => HighlightedDriverSettings.CarBrandVisible);
            plugin.AttachDelegate(name: "Highlighted.Width", valueProvider: () => HighlightedDriverSettings.Width);
            plugin.AttachDelegate(name: "Highlighted.BackgroundOpacity", valueProvider: () => HighlightedDriverSettings.BackgroundOpacity);
            plugin.AttachDelegate(name: "Highlighted.CarIdx", valueProvider: () => HighlightedDriver.CarIdx);
            plugin.AttachDelegate(name: "Highlighted.Number", valueProvider: () => HighlightedDriver.Number);
            plugin.AttachDelegate(name: "Highlighted.Name", valueProvider: () => HighlightedDriver.Name);
            plugin.AttachDelegate(name: "Highlighted.CarBrand", valueProvider: () => HighlightedDriver.CarBrand);
            plugin.AttachDelegate(name: "Highlighted.CarName", valueProvider: () => HighlightedDriver.CarName);
            plugin.AttachDelegate(name: "Highlighted.CountryCode", valueProvider: () => HighlightedDriver.CountryCode);
            plugin.AttachDelegate(name: "Highlighted.IRating", valueProvider: () => HighlightedDriver.IRating);
            plugin.AttachDelegate(name: "Highlighted.License", valueProvider: () => HighlightedDriver.License);
            plugin.AttachDelegate(name: "Highlighted.SafetyRating", valueProvider: () => HighlightedDriver.SafetyRating);
            plugin.AttachDelegate(name: "Highlighted.LivePositionInClass", valueProvider: () => HighlightedDriver.LivePositionInClass);
            plugin.AttachDelegate(name: "Highlighted.CurrentLap", valueProvider: () => HighlightedDriver.CurrentLap);
            plugin.AttachDelegate(name: "Highlighted.TeamIncidentCount", valueProvider: () => HighlightedDriver.TeamIncidentCount);
            plugin.AttachDelegate(name: "Highlighted.CarClassColor", valueProvider: () => HighlightedDriver.CarClassColor);
            plugin.AttachDelegate(name: "Highlighted.CarClassTextColor", valueProvider: () => HighlightedDriver.CarClassTextColor);
            plugin.AttachDelegate(name: "Highlighted.LastLapTime", valueProvider: () => HighlightedDriver.LastLapTime);
            plugin.AttachDelegate(name: "Highlighted.BestLapTime", valueProvider: () => HighlightedDriver.BestLapTime);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {
            if (data.FrameTime - _lastUpdateTime < _updateInterval) return;
            _lastUpdateTime = data.FrameTime;

            dynamic raw = data.NewData.GetRawDataObject();
            if (raw == null) return;

            _sessionState.Update(ref data);

            // Reset when changing/restarting session
            if (_sessionState.SessionChanged)
            {
                Drivers = new Dictionary<string, Driver>();
                DriversByCarIdx = new Dictionary<int, Driver>();
                PlayerCarIdx = -1;
                PlayerOutLap = false;
                PlayerStintLap = 0;
                PlayerNumber = "";
                PlayerCarBrand = "";
                PlayerCountryCode = "";
                PlayerPositionInClass = 0;
                PlayerLivePositionInClass = 0;
                PlayerHadWhiteFlag = false;
                PlayerHadCheckeredFlag = false;
                LiveClassLeaderboards = new List<ClassLeaderboard>();
                PlayerLastLapTime = TimeSpan.Zero;
                PlayerBestLapTime = TimeSpan.Zero;
                PlayerCurrentLap = 0;
                PlayerTeamIncidentCount = 0;
                PlayerIRatingChange = 0;
                BlankHighlightedDriver();
                _qualResultsUpdated = false;
            }

            UpdateDrivers(ref data);

            // Update lap times for all drivers based on the session results.
            // Do this after first trying to get the times from telemetry. 
            // Because lap times will be invalid in telemetry after the driver diconnected or exited the car.
            UpdateLapTimesFromSessionResults(ref data);

            // Update the highlighted car index
            RawDataHelper.TryGetTelemetryData<int>(ref data, out int highlightedCarIdx, "CamCarIdx");
            RawDataHelper.TryGetTelemetryData<int>(ref data, out int camCameraState, "CamCameraState");
            bool camCameraIsScenic = (camCameraState & 0x0002) != 0;
            if (highlightedCarIdx >= 0 && !camCameraIsScenic)
            {
                HighlightedDriver.CarIdx = highlightedCarIdx;
            }
            else
            {
                BlankHighlightedDriver();
            }

            for (int i = 0; i < data.NewData.Opponents.Count; i++)
            {
                Opponent opponent = data.NewData.Opponents[i];
                if (!Drivers.TryGetValue(opponent.CarNumber, out Driver driver))
                {
                    // Can happen when spectating a race and driving, the player car has no car number.
                    continue;
                }

                // Evaluate the lap when they entered the pit lane
                if (opponent.IsCarInPitLane)
                {
                    // Remember when they entered the pit.
                    if (driver.InPitSince == DateTime.MinValue)
                    {
                        driver.InPitSince = DateTime.Now;
                        driver.EnterPitLapUnconfirmed = opponent.CurrentLap ?? -1;
                    }

                    // If they are in the pit for a very short time then we consider that a glitch in telemetry and ignore it.
                    if (driver.InPitSince > DateTime.MinValue &&
                        driver.InPitSince + _minTimeInPit < DateTime.Now)
                    {
                        driver.EnterPitLap = driver.EnterPitLapUnconfirmed;
                        driver.OutLap = false;
                        driver.ExitPitLap = -1;
                        driver.StintLap = 0;

                        if (opponent.IsCarInPit)
                        {
                            if (driver.InPitBoxSince == DateTime.MinValue)
                            {
                                driver.InPitBoxSince = DateTime.Now;
                            }
                            driver.LastPitStopDuration = DateTime.Now - driver.InPitBoxSince;
                        }
                        else
                        {
                            driver.InPitBoxSince = DateTime.MinValue;
                        }
                    }                    
                }
                else
                {
                    // If they are in the pit for a very short time then we consider that a glitch in telemetry and ignore it.
                    // Ignore pit exit before the race start.
                    if (opponent.IsConnected &&
                        driver.InPitSince > DateTime.MinValue &&
                        !(_sessionModule.Race && !_sessionModule.RaceStarted) &&
                        driver.InPitSince + _minTimeInPit < DateTime.Now)
                    {
                        driver.ExitPitLap = opponent.CurrentLap ?? -1;

                        // Edge case when the pit exit is before the finish line.
                        // The currentLap will increment, so consider the next lap an out lap too.
                        if (opponent.TrackPositionPercent > 0.5)
                        {
                            driver.ExitPitLap++;
                        }
                    }

                    driver.OutLap = opponent.IsConnected && driver.ExitPitLap >= opponent.CurrentLap;
                    driver.InPitSince = DateTime.MinValue;
                    driver.InPitBoxSince = DateTime.MinValue;

                    if (driver.ExitPitLap >= 0)
                    {
                        driver.StintLap = (opponent.CurrentLap ?? 0) - driver.ExitPitLap + 1;
                    }
                    else if (_sessionModule.Race && !_sessionModule.JoinedRaceInProgress)
                    {
                        // When we join a race session in progress, we cannot know when the driver exited the pit, so StintLap should stay 0.
                        driver.StintLap = opponent.CurrentLap ?? 0;
                    }
                }

                if (_sessionModule.Race)
                {
                    double playerCarTowTime = 0;
                    try { playerCarTowTime = (double)raw.Telemetry["PlayerCarTowTime"]; } catch { }

                    if (!driver.Towing)
                    {
                        // Check for a jump in continuity, this means the driver teleported (towed) back to the pit.
                        if (driver.CurrentLapHighPrecision > -1 && 
                            opponent.CurrentLapHighPrecision.HasValue && opponent.CurrentLapHighPrecision.Value > -1)
                        {
                            // Use avg speed because in SimHub we can step forward in time in a recorded replay.
                            double avgSpeedKph = ComputeAvgSpeedKph(data.NewData.TrackLength, driver.CurrentLapHighPrecision, opponent.CurrentLapHighPrecision.Value, _sessionState.DeltaTime);
                            bool teleportingToPit = avgSpeedKph > 500 && opponent.IsCarInPit;
                            bool playerTowing = opponent.IsPlayer && playerCarTowTime > 0;

                            if (playerTowing || teleportingToPit)
                            {
                                driver.Towing = true;

                                if (opponent.IsPlayer)
                                {
                                    driver.TowingEndTime = DateTime.Now + TimeSpan.FromSeconds(playerCarTowTime);
                                }
                                else
                                {
                                    (double towLength, TimeSpan towTime) = ComputeTowLengthAndTime(data.NewData.TrackLength, driver.CurrentLapHighPrecision, opponent.CurrentLapHighPrecision.Value);
                                    driver.TowingEndTime = DateTime.Now + towTime;
                                }
                            }
                        }
                    }
                    else
                    {
                        // iRacing doesn't provide a tow time for other drivers, so we have to estimate it.
                        // Consider towing done if the car starts moving forward from a valid position
                        double smallDistancePct = 0.05 / data.NewData.TrackLength; // 0.05m is roughly the distance you cover at 10km/h in 16ms.

                        bool movingForward = opponent.CurrentLapHighPrecision.HasValue &&
                            opponent.CurrentLapHighPrecision.Value > -1 &&
                            driver.LastCurrentLapHighPrecision > -1 &&
                            opponent.CurrentLapHighPrecision > driver.LastCurrentLapHighPrecision + smallDistancePct;

                        bool done = opponent.CurrentLapHighPrecision == -1;
                        bool towEnded = !opponent.IsPlayer && DateTime.Now > driver.TowingEndTime;
                        bool playerNotTowing = opponent.IsPlayer && playerCarTowTime <= 0;
                        if (playerNotTowing || towEnded || movingForward || done)
                        {
                            driver.Towing = false;
                            driver.TowingEndTime = DateTime.MinValue;
                        }
                    }

                    // Pause updating the current lap if the driver is towing, so they stay at their last "on-track" position in the live standings.
                    // Otherwide they would leapfrog the leaders as they teleport in the pit.
                    if (!driver.Towing)
                    {
                        // Stop updating the current lap if the driver is done (-1), so they stay at their last known position in the live standings.
                        // Happens at the end of the race when they get out of the car.
                        if (opponent.CurrentLapHighPrecision.HasValue && opponent.CurrentLapHighPrecision.Value > -1)
                        {
                            driver.CurrentLapHighPrecision = opponent.CurrentLapHighPrecision.Value;
                        }
                    }

                    driver.LastCurrentLapHighPrecision = opponent.CurrentLapHighPrecision ?? -1;
                }
                else
                {
                    driver.CurrentLapHighPrecision = opponent.CurrentLapHighPrecision ?? -1;
                }

                // Update the average lap time for the driver
                int currentLap = opponent.CurrentLap ?? -1;
                driver.AvgLapTime.AddLapTime(currentLap, driver.LastLapTime);

                if (opponent.IsPlayer)
                {
                    PlayerOutLap = driver.OutLap;
                    PlayerStintLap = driver.StintLap;
                    PlayerNumber = opponent.CarNumber;
                    PlayerCarBrand = _carModule.GetCarBrand(driver.CarId, opponent.CarName);
                    PlayerCountryCode = _flairModule.GetCountryCode(driver.FlairId);
                    PlayerPositionInClass = opponent.Position > 0 ? opponent.PositionInClass : 0;
                    PlayerLastLapTime = driver.LastLapTime;
                    PlayerBestLapTime = driver.BestLapTime;
                    PlayerCurrentLapHighPrecision = driver.CurrentLapHighPrecision;
                    PlayerCurrentLap = opponent.CurrentLap ?? 0;
                    PlayerTeamIncidentCount = driver.TeamIncidentCount;

                    if (_sessionModule.Race)
                    {
                        PlayerHadWhiteFlag = PlayerHadWhiteFlag || data.NewData.Flag_White == 1;
                        PlayerHadCheckeredFlag = PlayerHadCheckeredFlag || data.NewData.Flag_Checkered == 1;
                    }
                }

                if (driver.CarIdx == HighlightedDriver.CarIdx)
                {
                    HighlightedDriver.Name = opponent.Name;
                    HighlightedDriver.Number = opponent.CarNumber;
                    HighlightedDriver.CarBrand = _carModule.GetCarBrand(driver.CarId, opponent.CarName);
                    HighlightedDriver.CarName = opponent.CarName;
                    HighlightedDriver.CountryCode = _flairModule.GetCountryCode(driver.FlairId);
                    HighlightedDriver.IRating = (int)(opponent.IRacing_IRating ?? 0);
                    (HighlightedDriver.License, HighlightedDriver.SafetyRating) = ParseLicenseString(opponent.LicenceString);
                    HighlightedDriver.CurrentLap = opponent.CurrentLap ?? 0;
                    HighlightedDriver.CurrentLapHighPrecision = driver.CurrentLapHighPrecision;
                    HighlightedDriver.TeamIncidentCount = driver.TeamIncidentCount;
                    HighlightedDriver.LastLapTime = driver.LastLapTime;
                    HighlightedDriver.BestLapTime = driver.BestLapTime;
                }
            }

            UpdateQualResult(ref data);
            UpdateLivePositionInClass(ref data);
            UpdateIRatingChange(ref data);
        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
            plugin.SaveCommonSettings("HighlightedDriverSettings", HighlightedDriverSettings);
        }

        private double ComputeAvgSpeedKph(double trackLength, double fromPos, double toPos, TimeSpan deltaTime)
        {
            if (deltaTime <= TimeSpan.Zero) return 0;
            double deltaPos = Math.Abs(toPos - fromPos);
            double length = deltaPos * trackLength;
            return (length / 1000) / (deltaTime.TotalSeconds / 3600);
        }

        private (double, TimeSpan) ComputeTowLengthAndTime(double trackLength, double fromPos, double toPos)
        {
            double deltaPos;
            if (toPos < fromPos)
            {
                // Must drive around the track
                deltaPos = 1.0 - fromPos + toPos;
            }
            else
            {
                deltaPos = toPos - fromPos;
            }
                
            double length = deltaPos * trackLength;
            const double towSpeedMs = 30;
            const double towTimeFixed = 50;
            return (length, TimeSpan.FromSeconds(length / towSpeedMs + towTimeFixed));
        }

        public static (string license, double rating) ParseLicenseString(string licenseString)
        {
            var parts = licenseString.Split(' ');
            string license = parts[0].Substring(0, 1); // take only the first letter, Pro is 'PWC'
            double rating = double.Parse(parts[1]);
            return (license, rating);
        }

        private void UpdateQualResult(ref GameData data)
        {
            // Optimization: Only update the qualifying results once before the race starts.
            if (_qualResultsUpdated || !_sessionModule.Race)
                return;

            RawDataHelper.TryGetSessionData<List<object>>(ref data, out List<object> qualResults, "QualifyResultsInfo", "Results");
            if (qualResults != null)
            {
                for (int i = 0; i < qualResults.Count; i++)
                {
                    RawDataHelper.TryGetSessionData<int>(ref data, out int carIdx, "QualifyResultsInfo", "Results", i, "CarIdx");
                    if (!DriversByCarIdx.TryGetValue(carIdx, out Driver driver))
                    {
                        Debug.Assert(false);
                        continue;
                    }

                    RawDataHelper.TryGetSessionData<int>(ref data, out int positionInClass, "QualifyResultsInfo", "Results", i, "ClassPosition");
                    RawDataHelper.TryGetSessionData<double>(ref data, out double fastestTime, "QualifyResultsInfo", "Results", i, "FastestTime");

                    driver.QualPositionInClass = positionInClass + 1;
                    driver.QualLapTime = fastestTime > 0 ? TimeSpan.FromSeconds(fastestTime) : TimeSpan.Zero;
                }

                _qualResultsUpdated = true;
                return;
            }

            RawDataHelper.TryGetSessionData<int>(ref data, out int currentSessionIdx, "SessionInfo", "CurrentSessionNum");
            if (currentSessionIdx < 0)
                return;

            RawDataHelper.TryGetSessionData<List<object>>(ref data, out List<object> qualPositions, "SessionInfo", "Sessions", currentSessionIdx, "QualifyPositions");
            if (qualPositions != null)
            {
                for (int i = 0; i < qualPositions.Count; i++)
                {
                    RawDataHelper.TryGetSessionData<int>(ref data, out int carIdx, "SessionInfo", "Sessions", currentSessionIdx, "QualifyPositions", i, "CarIdx");
                    if (!DriversByCarIdx.TryGetValue(carIdx, out Driver driver))
                    {
                        Debug.Assert(false);
                        continue;
                    }

                    RawDataHelper.TryGetSessionData<int>(ref data, out int positionInClass, "SessionInfo", "Sessions", currentSessionIdx, "QualifyPositions", i, "ClassPosition");
                    RawDataHelper.TryGetSessionData<double>(ref data, out double fastestTime, "SessionInfo", "Sessions", currentSessionIdx, "QualifyPositions", i, "FastestTime");

                    driver.QualPositionInClass = positionInClass + 1;
                    driver.QualLapTime = fastestTime > 0 ? TimeSpan.FromSeconds(fastestTime) : TimeSpan.Zero;
                }

                _qualResultsUpdated = true;
            }
        }

        private void UpdateDrivers(ref GameData data)
        {
            dynamic raw = data.NewData.GetRawDataObject();
            if (raw == null) 
                return;

            RawDataHelper.TryGetSessionData<int>(ref data, out int playerCarIdx, "DriverInfo", "DriverCarIdx");
            PlayerCarIdx = playerCarIdx;

            int driverCount = 0;
            try { driverCount = (int)raw.AllSessionData["DriverInfo"]["Drivers"].Count; } catch { Debug.Assert(false); }

            for (int i = 0; i < driverCount; i++)
            {
                int carIdx = -1;
                try { carIdx = int.Parse(raw.AllSessionData["DriverInfo"]["Drivers"][i]["CarIdx"]); } catch { Debug.Assert(false); }

                string carNumber = string.Empty;
                try { carNumber = raw.AllSessionData["DriverInfo"]["Drivers"][i]["CarNumber"]; } catch { Debug.Assert(false); }

                string carPath = string.Empty;
                try { carPath = raw.AllSessionData["DriverInfo"]["Drivers"][i]["CarPath"]; } catch { Debug.Assert(false); }

                RawDataHelper.TryGetSessionData<int>(ref data, out int flairId, "DriverInfo", "Drivers", i, "FlairID");
                RawDataHelper.TryGetSessionData<int>(ref data, out int carClassId, "DriverInfo", "Drivers", i, "CarClassID");
                RawDataHelper.TryGetSessionData<int>(ref data, out int teamIncidentCount, "DriverInfo", "Drivers", i, "TeamIncidentCount");
                RawDataHelper.TryGetSessionData<int>(ref data, out int iRating, "DriverInfo", "Drivers", i, "IRating");

                double lastLapTime = 0;
                try { lastLapTime = Math.Max(0, (double)raw.Telemetry["CarIdxLastLapTime"][carIdx]); } catch { Debug.Assert(false); }

                double bestLapTime = 0;
                try { bestLapTime = Math.Max(0, (double)raw.Telemetry["CarIdxBestLapTime"][carIdx]); } catch { Debug.Assert(false); }

                RawDataHelper.TryGetTelemetryData<int>(ref data, out int sessionFlags, "CarIdxSessionFlags", carIdx);
                RawDataHelper.TryGetTelemetryData<int>(ref data, out int classPosition, "CarIdxClassPosition", carIdx);

                if (carIdx >= 0 && carNumber.Length > 0)
                {
                    if (!Drivers.TryGetValue(carNumber, out Driver driver))
                    {
                        driver = new Driver();
                        Drivers[carNumber] = driver;
                        DriversByCarIdx[carIdx] = driver;
                    }

                    driver.DriverInfoIdx = i;
                    driver.CarIdx = carIdx;
                    driver.CarId = carPath;
                    driver.FlairId = flairId;
                    driver.CarClassId = carClassId;
                    driver.TeamIncidentCount = teamIncidentCount;
                    driver.IRating = iRating;
                    driver.LastLapTime = lastLapTime > 0 ? TimeSpan.FromSeconds(lastLapTime) : TimeSpan.Zero;
                    driver.BestLapTime = bestLapTime > 0 ? TimeSpan.FromSeconds(bestLapTime) : TimeSpan.Zero;
                    driver.SessionFlags = sessionFlags;
                    driver.PositionInClass = classPosition;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        private void UpdateLivePositionInClass(ref GameData data)
        {
            LiveClassLeaderboards = new List<ClassLeaderboard>();

            for (int carClassIdx = 0; carClassIdx < data.NewData.OpponentsClassses.Count; carClassIdx++)
            {
                ClassLeaderboard leaderboard = new ClassLeaderboard();
                LiveClassLeaderboards.Add(leaderboard);

                leaderboard.CarClassDescription = data.NewData.OpponentsClassses[carClassIdx];
                List<Opponent> opponents = leaderboard.CarClassDescription.Opponents;
                for (int i = 0; i < opponents.Count; i++)
                {
                    Opponent opponent = opponents[i];
                    if (!Drivers.TryGetValue(opponent.CarNumber, out Driver driver))
                    {
                        // Can happen when spectating a race and driving, the player car has no car number.
                        continue;
                    }

                    leaderboard.Drivers.Add((opponent, driver));
                }

                if (_sessionModule.Race)
                {
                    if (!_sessionModule.RaceStarted)
                    {
                        // Before the start keep the leaderboard sorted by qual position
                        leaderboard.Drivers = leaderboard.Drivers.OrderBy(p => p.Item2.QualPositionInClass).ToList();
                    }
                    else if (!_sessionModule.RaceFinished)
                    {
                        // During the race sort on position on track for a live leaderboard.
                        // Except for ovals under caution, show the official position.
                        if (!(_sessionModule.Oval && data.NewData.Flag_Yellow == 1))
                        {
                            leaderboard.Drivers = leaderboard.Drivers.OrderByDescending(p => p.Item2.CurrentLapHighPrecision).ToList();
                        }
                    }
                    else
                    {
                        // After the race don't sort to show the official race result
                    }
                }

                for (int i = 0; i < leaderboard.Drivers.Count; i++)
                {
                    Opponent opponent = leaderboard.Drivers[i].Item1;
                    Driver driver = leaderboard.Drivers[i].Item2;

                    if (_sessionModule.Race)
                    {
                        if (!_sessionModule.RaceStarted)
                        {
                            driver.LivePositionInClass = driver.QualPositionInClass;
                        }
                        else
                        {
                            driver.LivePositionInClass = i + 1;
                        }
                    }
                    else
                    {
                        driver.LivePositionInClass = opponent.Position > 0 ? i + 1 : 0;
                    }

                    if (opponent.IsPlayer)
                    {
                        PlayerLivePositionInClass = driver.LivePositionInClass;
                    }

                    if (driver.CarIdx == HighlightedDriver.CarIdx)
                    {
                        HighlightedDriver.LivePositionInClass = driver.LivePositionInClass;
                        HighlightedDriver.CarClassColor = leaderboard.CarClassDescription.ClassColor;
                        HighlightedDriver.CarClassTextColor = leaderboard.CarClassDescription.ClassTextColor;
                    }
                }
            }
        }

        private void UpdateLapTimesFromSessionResults(ref GameData data)
        {
            dynamic raw = data.NewData.GetRawDataObject();
            if (raw == null) 
                return;

            // It can happen that CurrentSessionNum is missing on SessionInfo. We can't tell which session to use in that case.
            int sessionInfoCount = -1;
            try { sessionInfoCount = raw.AllSessionData["SessionInfo"].Count; } catch { Debug.Assert(false); }
            if (sessionInfoCount <= 1) 
                return;

            int sessionIdx = -1;
            try { sessionIdx = int.Parse(raw.AllSessionData["SessionInfo"]["CurrentSessionNum"]); } catch { Debug.Assert(false); }
            if (sessionIdx < 0) 
                return;

            List<object> positions = null;
            try { positions = raw.AllSessionData["SessionInfo"]["Sessions"][sessionIdx]["ResultsPositions"]; } catch { Debug.Assert(false); }
            if (positions == null) 
                return;

            for (int posIdx = 0; posIdx < positions.Count; posIdx++)
            {
                int carIdx = -1;
                try { carIdx = int.Parse(raw.AllSessionData["SessionInfo"]["Sessions"][sessionIdx]["ResultsPositions"][posIdx]["CarIdx"]); } catch { Debug.Assert(false); }
                if (carIdx < 0) 
                    continue;

                if (!DriversByCarIdx.TryGetValue(carIdx, out Driver driver))
                {
                    Debug.Assert(false);
                    continue;
                }

                double bestLapTime = 0;
                try { bestLapTime = double.Parse(raw.AllSessionData["SessionInfo"]["Sessions"][sessionIdx]["ResultsPositions"][posIdx]["FastestTime"]); } catch { Debug.Assert(false); }

                if (driver.BestLapTime == TimeSpan.Zero && bestLapTime > 0)
                {
                    driver.BestLapTime = TimeSpan.FromSeconds(bestLapTime);
                }

                double lastLapTime = 0;
                try { lastLapTime = double.Parse(raw.AllSessionData["SessionInfo"]["Sessions"][sessionIdx]["ResultsPositions"][posIdx]["LastTime"]); } catch { Debug.Assert(false); }

                if (driver.LastLapTime == TimeSpan.Zero && lastLapTime > 0)
                {
                    driver.LastLapTime = TimeSpan.FromSeconds(lastLapTime);
                }

                int lapsComplete = 0;
                RawDataHelper.TryGetSessionData<int>(ref data, out lapsComplete, "SessionInfo", "Sessions", sessionIdx, "ResultsPositions", posIdx, "LapsComplete");
                driver.LapsComplete = lapsComplete;

                int jokerLapsComplete = 0;
                try { jokerLapsComplete = int.Parse(raw.AllSessionData["SessionInfo"]["Sessions"][sessionIdx]["ResultsPositions"][posIdx]["JokerLapsComplete"]); } catch { Debug.Assert(false); }
                driver.JokerLapsComplete = jokerLapsComplete;
            }
        }

        private void UpdateIRatingChange(ref GameData data)
        {
            if (!(_sessionModule.Race))
                return;

            // Optim: Don't calculate if the iRating change is not visible.
            if (!_standingsModule.Settings.IRatingChangeVisible && !_relativeModule.Settings.IRatingChangeVisible)
                return;

            foreach (var group in Drivers.Values.GroupBy(d => d.CarClassId))
            {
                int carClassId = group.Key;
                int countInClass = group.Count();
                var raceResults = new List<RaceResult<Driver>>();

                if (!_sessionModule.RaceStarted)
                {
                    // Consider all drivers as if they finished in their qualifying position.
                    foreach (var driver in group)
                    {
                        raceResults.Add(new RaceResult<Driver>(
                         driver,
                         (uint)driver.QualPositionInClass,
                         (uint)driver.IRating,
                         true));
                    }
                }
                else
                {
                    // Consider drivers with an official position first. They are considered as started.
                    // TODO: Should DQ drivers be considered as not started?
                    // TODO: How much of the first lap should be completed to be considered started?
                    var withPosition = group.Where(d => d.PositionInClass != 0).ToList();
                    foreach (var driver in withPosition)
                    {
                        int positionInClass = driver.LivePositionInClass;
                        if (positionInClass <= 0)
                        {
                            // Fallback to the official position if the live position is not available.
                            positionInClass = driver.PositionInClass;
                        }

                        raceResults.Add(new RaceResult<Driver>(
                         driver,
                         (uint)positionInClass,
                         (uint)driver.IRating,
                         true));
                    }

                    // Then consider drivers without an official position. They are considered as not started.
                    // Assign them a position by sorting them by IRating.
                    var noPosition = group.Where(d => d.PositionInClass == 0).OrderByDescending(d => d.IRating).ToList();
                    int nextPosition = withPosition.Count + 1;
                    foreach (var driver in noPosition)
                    {
                        raceResults.Add(new RaceResult<Driver>(
                         driver,
                         (uint)nextPosition++,
                         (uint)driver.IRating,
                         false));
                    }
                }

                var results = IRatingCalculator.Calculate(raceResults);
                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    int change = (int)result.NewIRating - (int)result.RaceResult.StartIRating;
                    result.RaceResult.Driver.IRatingChange = change;

                    if (result.RaceResult.Driver.CarIdx == PlayerCarIdx)
                    {
                        PlayerIRatingChange = change;
                    }
                }
            }
        }

        private void BlankHighlightedDriver()
        {
            HighlightedDriver.CarIdx = -1;
            HighlightedDriver.Number = string.Empty;
            HighlightedDriver.Name = string.Empty;
            HighlightedDriver.CarBrand = string.Empty;
            HighlightedDriver.CarName = string.Empty;
            HighlightedDriver.CountryCode = string.Empty;
            HighlightedDriver.IRating = 0;
            HighlightedDriver.License = string.Empty;
            HighlightedDriver.SafetyRating = 0.0;
            HighlightedDriver.LivePositionInClass = 0;
            HighlightedDriver.CurrentLap = 0;
            HighlightedDriver.TeamIncidentCount = 0;
            HighlightedDriver.CarClassColor = string.Empty;
            HighlightedDriver.CarClassTextColor = string.Empty;
            HighlightedDriver.LastLapTime = TimeSpan.Zero;
            HighlightedDriver.BestLapTime = TimeSpan.Zero;
        }

        public Driver GetPlayerDriver()
        {
            DriversByCarIdx.TryGetValue(PlayerCarIdx, out Driver driver);
            return driver;
        }
    }
}
