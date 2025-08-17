using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace InsanePlugin
{
    using OpponentsWithDrivers = List<(Opponent, Driver)>;

    public class StandingsSettings : ModuleSettings
    {
        public int HeaderWidth { get; set; } = 10;
        public int Width { get; set; } = 80;
        public bool HeaderVisible { get; set; } = true;
        public bool CarClassHeaderVisible { get; set; } = true;
        public int LeadFocusedRows { get; set; } = 3;
        public int MaxRowsOtherClasses { get; set; } = 3;
        public int MaxRowsPlayerClass { get; set; } = 10;
        public bool PositionChangeVisible { get; set; } = true;
        public bool CountryFlagVisible { get; set; } = true;
        public bool CarLogoVisible { get; set; } = true;
        public bool IRatingVisible { get; set; } = true;
        public bool IRatingChangeVisible { get; set; } = true;
        public bool CarLogoVisibleInRace { get; set; } = true;
        public bool GapVisibleInRace { get; set; } = true;
        public bool BestVisibleInRace { get; set; } = true;
        public bool LastVisibleInRace { get; set; } = true;
        public bool DeltaVisibleInRace { get; set; } = true;
        public bool BestVisible { get; set; } = true;
        public bool LastVisible { get; set; } = true;
        public bool DeltaVisible { get; set; } = true;
        public bool UseDeltaToPlayer { get; set; } = false;
        public bool InvertDeltaToPlayer { get; set; } = false;
        public bool ShowStintLapInRace { get; set; } = true;
        public int AlternateRowBackgroundColor { get; set; } = 5;
        public int HeaderOpacity { get; set; } = 90;
        public int BackgroundOpacity { get; set; } = 7;
    }

    public class StandingRow
    {
        public bool RowVisible { get; set; } = false;
        public bool IsPlayer { get; set; } = false;
        public bool IsHighlighted { get; set; } = false;
        public string PlayerID { get; set; } = string.Empty;
        public bool Connected { get; set; } = false;
        public int LivePositionInClass { get; set; } = 0;
        public int PositionChange { get; set; } = 0;
        public string Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CarId { get; set; } = string.Empty;
        public string CarBrand { get; set; } = string.Empty;
        public string CarClassColor { get; set; } = string.Empty;
        public string CarClassTextColor { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public bool InPitLane { get; set; } = false;
        public bool Towing { get; set; } = false;
        public bool OutLap { get; set; } = false;
        public int EnterPitLap { get; set; } = 0;
        public TimeSpan LastPitStopDuration { get; set; } = TimeSpan.Zero;
        public int iRating { get; set; } = 0;
        public float iRatingChange { get; set; } = 0;
        public string License {  get; set; } = string.Empty;
        public double SafetyRating { get; set; } = 0;
        public int CurrentLap {  get; set; } = 0;
        public int StintLap { get; set; } = 0;
        public int LapsToClassLeader { get; set; } = 0;
        public double GapToClassLeader { get; set; } = 0;
        public TimeSpan? DeltaToClassLeader { get; set; } = null;
        public TimeSpan? DeltaToPlayer { get; set; } = null;
        public string TireCompound {  get; set; } = string.Empty;
        public bool TireCompoundVisible { get; set; } = false;
        public TimeSpan BestLapTime { get; set; } = TimeSpan.Zero;
        public TimeSpan LastLapTime { get; set; } = TimeSpan.Zero;
        public int JokerLapsComplete { get; set; } = 0;
    }

    public class StandingCarClass
    {
        public const int MaxRows = 25;
        public string Name { get; set; } = string.Empty;
        public float NameSize { get; set; } = 0;
        public int VisibleRowCount { get; set; } = 0;
        public List<StandingRow> Rows { get; internal set; }
        public bool LeadFocusedDividerVisible { get; set; } = false;
        public string Color { get; set; } = string.Empty;
        public string TextColor { get; set; } = string.Empty;
        public int Sof { get; set; } = 0;
        public int DriverCount { get; set; } = 0;
        public TimeSpan BestLapTime { get; set; } = TimeSpan.Zero;
        public TimeSpan BestQualLapTime { get; set; } = TimeSpan.Zero;
        public TimeSpan LeaderLastLapTime { get; set; } = TimeSpan.Zero;
        public TimeSpan LeaderAvgLapTime { get; set; } = TimeSpan.Zero;
        public int EstimatedTotalLaps { get; set; } = 0;
        public bool EstimatedTotalLapsConfirmed { get; set; } = false;

        public StandingCarClass()
        {
            Rows = new List<StandingRow>(Enumerable.Range(0, MaxRows).Select(x => new StandingRow()));
        }
    }

    public class StandingsModule : PluginModuleBase
    {
        private DriverModule _driverModule = null;
        private CarModule _carModule = null;
        private SessionModule _sessionModule = null;
        private FlairModule _flairModule = null;

        private DateTime _lastUpdateTime = DateTime.MinValue;
        private TimeSpan _updateInterval = TimeSpan.FromMilliseconds(500);

        private Font _font = null;
        private Bitmap _fontBitmap = null;
        private Graphics _fontGraphics = null;

        public StandingsSettings Settings { get; set; }

        public const int MaxCarClasses = 4;
        public List<StandingCarClass> CarClasses { get; internal set; }
        public int VisibleClassCount { get; internal set; } = 0;
        public int TotalDriverCount { get; internal set; } = 0;
        public int TotalSoF { get; internal set; } = 0;
        public int HighlightedCarClassIdx { get; internal set; } = 0;
        public bool CarLogoVisible { get; internal set; } = true;
        public bool GapVisible { get; internal set; } = true;
        public bool BestVisible { get; internal set; } = true;
        public bool LastVisible { get; internal set; } = true;
        public bool DeltaVisible { get; internal set; } = true;

        public override int UpdatePriority => 80;

        public StandingsModule()
        {
            CarClasses = new List<StandingCarClass>(Enumerable.Range(0, MaxCarClasses).Select(x => new StandingCarClass()));
        }

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            _driverModule = plugin.GetModule<DriverModule>();
            _carModule = plugin.GetModule<CarModule>();
            _sessionModule = plugin.GetModule<SessionModule>();
            _flairModule = plugin.GetModule<FlairModule>();

            _font = new Font("Roboto", 16);
            _fontBitmap = new Bitmap(1, 1);
            _fontGraphics = Graphics.FromImage(_fontBitmap);

            Settings = plugin.ReadCommonSettings<StandingsSettings>("StandingsSettings", () => new StandingsSettings());
            plugin.AttachDelegate(name: $"Standings.HeaderVisible", valueProvider: () => Settings.HeaderVisible);
            plugin.AttachDelegate(name: $"Standings.HeaderWidth", valueProvider: () => Settings.HeaderWidth);
            plugin.AttachDelegate(name: $"Standings.Width", valueProvider: () => Settings.Width);
            plugin.AttachDelegate(name: $"Standings.CarClassHeaderVisible", valueProvider: () => Settings.CarClassHeaderVisible);
            plugin.AttachDelegate(name: $"Standings.VisibleClassCount", valueProvider: () => VisibleClassCount);
            plugin.AttachDelegate(name: $"Standings.TotalDriverCount", valueProvider: () => TotalDriverCount);
            plugin.AttachDelegate(name: $"Standings.TotalSoF", valueProvider: () => TotalSoF);
            plugin.AttachDelegate(name: $"Standings.PlayerCarClassIdx", valueProvider: () => HighlightedCarClassIdx);
            plugin.AttachDelegate(name: $"Standings.LeadFocusedRows", valueProvider: () => Settings.LeadFocusedRows);
            plugin.AttachDelegate(name: $"Standings.MaxRowsPlayerClass", valueProvider: () => Settings.MaxRowsPlayerClass);
            plugin.AttachDelegate(name: $"Standings.MaxRowsOtherClasses", valueProvider: () => Settings.MaxRowsOtherClasses);
            plugin.AttachDelegate(name: $"Standings.PositionChangeVisible", valueProvider: () => Settings.PositionChangeVisible);
            plugin.AttachDelegate(name: $"Standings.CountryFlagVisible", valueProvider: () => Settings.CountryFlagVisible);
            plugin.AttachDelegate(name: $"Standings.CarLogoVisible", valueProvider: () => CarLogoVisible);
            plugin.AttachDelegate(name: $"Standings.iRatingVisible", valueProvider: () => Settings.IRatingVisible);
            plugin.AttachDelegate(name: $"Standings.iRatingChangeVisible", valueProvider: () => Settings.IRatingChangeVisible);
            plugin.AttachDelegate(name: $"Standings.GapVisible", valueProvider: () => GapVisible);
            plugin.AttachDelegate(name: $"Standings.BestVisible", valueProvider: () => BestVisible);
            plugin.AttachDelegate(name: $"Standings.LastVisible", valueProvider: () => LastVisible);
            plugin.AttachDelegate(name: $"Standings.DeltaVisible", valueProvider: () => DeltaVisible);
            plugin.AttachDelegate(name: $"Standings.UseDeltaToPlayer", valueProvider: () => Settings.UseDeltaToPlayer);
            plugin.AttachDelegate(name: $"Standings.ShowStintLapInRace", valueProvider: () => Settings.ShowStintLapInRace);
            plugin.AttachDelegate(name: "Standings.AlternateRowBackgroundColor", valueProvider: () => Settings.AlternateRowBackgroundColor);
            plugin.AttachDelegate(name: "Standings.HeaderOpacity", valueProvider: () => Settings.HeaderOpacity);
            plugin.AttachDelegate(name: "Standings.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);

            for (int carClassIdx = 0; carClassIdx < MaxCarClasses; carClassIdx++)
            {
                StandingCarClass carClass = CarClasses[carClassIdx];
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Name", valueProvider: () => carClass.Name);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.NameSize", valueProvider: () => carClass.NameSize);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.VisibleRowCount", valueProvider: () => carClass.VisibleRowCount);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.LeadFocusedDividerVisible", valueProvider: () => carClass.LeadFocusedDividerVisible);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Color", valueProvider: () => carClass.Color);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.TextColor", valueProvider: () => carClass.TextColor);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Sof", valueProvider: () => carClass.Sof);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.DriverCount", valueProvider: () => carClass.DriverCount);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.BestLapTime", valueProvider: () => carClass.BestLapTime);
                plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.EstimatedTotalLaps", valueProvider: () => carClass.EstimatedTotalLaps);

                for (int rowIdx = 0; rowIdx < StandingCarClass.MaxRows; rowIdx++)
                {
                    StandingRow row = carClass.Rows[rowIdx];
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.RowVisible", valueProvider: () => row.RowVisible);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.IsPlayer", valueProvider: () => row.IsPlayer);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.IsHighlighted", valueProvider: () => row.IsHighlighted);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.PlayerID", valueProvider: () => row.PlayerID);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.Connected", valueProvider: () => row.Connected);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.LivePositionInClass", valueProvider: () => row.LivePositionInClass);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.PositionChange", valueProvider: () => row.PositionChange);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.Number", valueProvider: () => row.Number);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.Name", valueProvider: () => row.Name);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.CarId", valueProvider: () => row.CarId);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.CarBrand", valueProvider: () => row.CarBrand);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.CarClassColor", valueProvider: () => row.CarClassColor);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.CarClassTextColor", valueProvider: () => row.CarClassTextColor);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.CountryCode", valueProvider: () => row.CountryCode);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.InPitLane", valueProvider: () => row.InPitLane);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.Towing", valueProvider: () => row.Towing);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.OutLap", valueProvider: () => row.OutLap);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.EnterPitLap", valueProvider: () => row.EnterPitLap);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.LastPitStopDuration", valueProvider: () => row.LastPitStopDuration);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.iRating", valueProvider: () => row.iRating);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.iRatingChange", valueProvider: () => row.iRatingChange);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.License", valueProvider: () => row.License);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.SafetyRating", valueProvider: () => row.SafetyRating);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.CurrentLap", valueProvider: () => row.CurrentLap);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.StintLap", valueProvider: () => row.StintLap);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.LapsToClassLeader", valueProvider: () => row.LapsToClassLeader);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.GapToClassLeader", valueProvider: () => row.GapToClassLeader);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.DeltaToClassLeader", valueProvider: () => row.DeltaToClassLeader);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.DeltaToPlayer", valueProvider: () => row.DeltaToPlayer);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.TireCompound", valueProvider: () => row.TireCompound);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.TireCompoundVisible", valueProvider: () => row.TireCompoundVisible);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.BestLapTime", valueProvider: () => row.BestLapTime);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.LastLapTime", valueProvider: () => row.LastLapTime);
                    plugin.AttachDelegate(name: $"Standings.Class{carClassIdx:00}.Row{rowIdx:00}.JokerLapsComplete", valueProvider: () => row.JokerLapsComplete);
                }
            }
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {
            if (data.FrameTime - _lastUpdateTime < _updateInterval) return;
            _lastUpdateTime = data.FrameTime;

            Driver highlightedDriver = null;
            (highlightedDriver, HighlightedCarClassIdx) = FindHighlightedDriver(ref data);

            if (_sessionModule.Race)
            {
                CarLogoVisible = Settings.CarLogoVisibleInRace;
                GapVisible = Settings.GapVisibleInRace;
                BestVisible = Settings.BestVisibleInRace;
                LastVisible = Settings.LastVisibleInRace;
                DeltaVisible = Settings.DeltaVisibleInRace;
            }
            else
            {
                CarLogoVisible = Settings.CarLogoVisible;
                GapVisible = false;
                BestVisible = Settings.BestVisible;
                LastVisible = Settings.LastVisible;
                DeltaVisible = Settings.DeltaVisible;
            }

            int visibleClassCount = 0;

            for (int carClassIdx = 0; carClassIdx < MaxCarClasses; carClassIdx++)
            {
                StandingCarClass carClass = CarClasses[carClassIdx];
                if (carClassIdx < _driverModule.LiveClassLeaderboards.Count)
                {
                    LeaderboardCarClassDescription opponentClass = _driverModule.LiveClassLeaderboards[carClassIdx].CarClassDescription;
                    OpponentsWithDrivers opponentsWithDrivers = _driverModule.LiveClassLeaderboards[carClassIdx].Drivers;

                    carClass.Color = opponentClass.ClassColor;
                    carClass.TextColor = opponentClass.ClassTextColor;

                    if ((opponentClass.ClassName == null || opponentClass.ClassName.Length == 0 || opponentClass.ClassName == "Hosted All Cars"))
                    {
                        if (opponentClass.CarModels.Count == 1)
                        {
                            // Fallback to the car model name when we don't have a class name and there's only 1 car model.
                            carClass.Name = opponentClass.CarModels[0];
                        }
                        else
                        {
                            // There's an issue with AI races where iRacing does not provide a class name, and SimHub fails to create the classes.
                            carClass.Name = "All Cars";
                            carClass.Color = "#FFFFFF";
                            carClass.TextColor = "#000000";
                        }
                    }
                    else
                    {
                        carClass.Name = _carModule.GetCarClassName(opponentClass.ClassName);
                    }

                    carClass.NameSize = MeasureTextInPixels(carClass.Name);
                    carClass.Sof = CalculateSof(opponentsWithDrivers);
                    carClass.DriverCount = opponentsWithDrivers.Count;
                    carClass.BestLapTime = FindBestLapTime(opponentsWithDrivers);
                    carClass.BestQualLapTime = FindBestQualLapTime(opponentsWithDrivers);

                    if (opponentsWithDrivers.Count > 0)
                    {
                        carClass.LeaderLastLapTime = opponentsWithDrivers[0].Item2.LastLapTime;
                        carClass.LeaderAvgLapTime = opponentsWithDrivers[0].Item2.AvgLapTime.GetAverageLapTime();
                    }
                    else
                    {
                        carClass.LeaderLastLapTime = TimeSpan.Zero;
                        carClass.LeaderAvgLapTime = TimeSpan.Zero;
                    }

                    UpdateEstimatedTotalLaps(ref data, carClass, opponentsWithDrivers);

                    int skipRowCount = 0;
                    int maxRowCount;
                    if (highlightedDriver != null && HighlightedCarClassIdx == carClassIdx)
                    {
                        maxRowCount = Settings.MaxRowsPlayerClass;

                        // How many rows to skip to have a lead-focused leaderboard
                        skipRowCount = GetLeadFocusedSkipRowCount(highlightedDriver.CarIdx, opponentsWithDrivers);
                    }
                    else
                    {
                        maxRowCount = Settings.MaxRowsOtherClasses;
                    }

                    carClass.LeadFocusedDividerVisible = skipRowCount > 0;

                    int visibleRowCount = 0;
                    for (int rowIdx = 0; rowIdx < StandingCarClass.MaxRows; rowIdx++)
                    {
                        StandingRow row = carClass.Rows[rowIdx];
                        if (visibleRowCount >= maxRowCount)
                        {
                            // Reached row count limit
                            BlankRow(row);
                            continue;
                        }

                        int actualDriverIdx = rowIdx;
                        if (rowIdx > Settings.LeadFocusedRows - 1)
                        {
                            actualDriverIdx += skipRowCount;
                        }

                        if (actualDriverIdx >= opponentsWithDrivers.Count)
                        {
                            BlankRow(row);
                            continue;
                        }

                        Opponent opponent = opponentsWithDrivers[actualDriverIdx].Item1;
                        Driver driver = opponentsWithDrivers[actualDriverIdx].Item2;
                        if (!IsValidRow(opponent))
                        {
                            BlankRow(row);
                            continue;
                        }

                        row.RowVisible = true;
                        row.IsPlayer = opponent.IsPlayer;
                        row.IsHighlighted = ((highlightedDriver?.CarIdx ?? -1) == driver.CarIdx);
                        row.PlayerID = opponent.Id;
                        row.Connected = opponent.IsConnected;
                        row.LivePositionInClass = driver.LivePositionInClass;

                        if (_sessionModule.Race && driver.QualPositionInClass > 0 && driver.LivePositionInClass > 0)
                        {
                            row.PositionChange = driver.QualPositionInClass - driver.LivePositionInClass;
                        }

                        row.Number = opponent.CarNumber;
                        if (_sessionModule.TeamRacing)
                        {
                            row.Name = opponent.TeamName;
                        }
                        else
                        {
                            row.Name = opponent.Name;
                        }
                        row.CarId = driver.CarId;
                        row.CarBrand = _carModule.GetCarBrand(driver.CarId, opponent.CarName);
                        row.CarClassColor = opponent.CarClassColor;
                        row.CarClassTextColor = opponent.CarClassTextColor;
                        row.CountryCode = _flairModule.GetCountryCode(driver.FlairId);
                        row.InPitLane = opponent.IsCarInPitLane;
                        row.Towing = driver.Towing;
                        row.OutLap = driver.OutLap;
                        row.EnterPitLap = driver.EnterPitLap;
                        row.LastPitStopDuration = driver.LastPitStopDuration;
                        row.iRating = (int)(opponent.IRacing_IRating ?? 0);
                        row.iRatingChange = driver.IRatingChange;
                        (row.License, row.SafetyRating) = DriverModule.ParseLicenseString(opponent.LicenceString);
                        row.CurrentLap = opponent.CurrentLap ?? 0;
                        row.StintLap = driver.StintLap;
                        row.LapsToClassLeader = opponent.LapsToClassLeader ?? 0;
                        row.GapToClassLeader = opponent.GaptoClassLeader ?? 0;
                        (row.TireCompound, row.TireCompoundVisible) = GetTireCompound(ref data, driver.CarIdx);
                        row.BestLapTime = driver.BestLapTime;
                        row.LastLapTime = driver.LastLapTime;
                        row.JokerLapsComplete = driver.JokerLapsComplete;

                        if (_sessionModule.Race)
                        {
                            if (row.LastLapTime > TimeSpan.Zero && carClass.LeaderLastLapTime > TimeSpan.Zero)
                            {
                                row.DeltaToClassLeader = row.LastLapTime - carClass.LeaderLastLapTime;
                            }
                            else
                            {
                                row.DeltaToClassLeader = null;
                            }

                            if (highlightedDriver != null && row.LastLapTime > TimeSpan.Zero && highlightedDriver.LastLapTime > TimeSpan.Zero)
                            {
                                if (Settings.InvertDeltaToPlayer)
                                {
                                    row.DeltaToPlayer = highlightedDriver.LastLapTime - row.LastLapTime;
                                }
                                else
                                {
                                    row.DeltaToPlayer = row.LastLapTime - highlightedDriver.LastLapTime;
                                }
                            }
                            else
                            {
                                row.DeltaToPlayer = null;
                            }
                        }
                        else
                        {
                            if (row.BestLapTime > TimeSpan.Zero && carClass.BestLapTime > TimeSpan.Zero)
                            {
                                row.DeltaToClassLeader = row.BestLapTime - carClass.BestLapTime;
                            }
                            else
                            {
                                row.DeltaToClassLeader = null;
                            }

                            if (highlightedDriver != null && row.BestLapTime > TimeSpan.Zero && highlightedDriver.BestLapTime > TimeSpan.Zero)
                            {
                                if (Settings.InvertDeltaToPlayer)
                                {
                                    row.DeltaToPlayer = highlightedDriver.BestLapTime - row.BestLapTime;
                                }
                                else
                                {
                                    row.DeltaToPlayer = row.BestLapTime - highlightedDriver.BestLapTime;
                                }
                            }
                            else
                            {
                                row.DeltaToPlayer = null;
                            }
                        }

                        // Make sure we have a best lap time for the first lap of a race
                        // iRacing often doesn't provide a valid best lap time for lap 1
                        if (_sessionModule.Race && row.BestLapTime <= TimeSpan.Zero && row.LastLapTime > TimeSpan.Zero)
                        {
                            row.BestLapTime = row.LastLapTime;
                        }

                        visibleRowCount++;
                    }

                    carClass.VisibleRowCount = visibleRowCount;
                    visibleClassCount++;
                }
                else
                {
                    BlankCarClass(carClass);
                }
            }

            VisibleClassCount = visibleClassCount;
            TotalDriverCount = data.NewData.OpponentsCount;
            TotalSoF = CalculateTotalSof(data.NewData.Opponents);
        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
            plugin.SaveCommonSettings("StandingsSettings", Settings);
        }

        public void BlankCarClass(StandingCarClass carClass)
        {
            carClass.Name = string.Empty;
            carClass.NameSize = 0;
            carClass.VisibleRowCount = 0;
            carClass.LeadFocusedDividerVisible = false;
            carClass.Color = string.Empty;
            carClass.TextColor = string.Empty;
            carClass.Sof = 0;
            carClass.DriverCount = 0;
            carClass.BestLapTime = TimeSpan.Zero;
            carClass.BestQualLapTime = TimeSpan.Zero;
            carClass.LeaderLastLapTime = TimeSpan.Zero;
            carClass.LeaderAvgLapTime = TimeSpan.Zero;
            carClass.EstimatedTotalLaps = 0;
            carClass.EstimatedTotalLapsConfirmed = false;

            for (int driverIdx = 0; driverIdx < StandingCarClass.MaxRows; driverIdx++)
            {
                StandingRow driver = carClass.Rows[driverIdx];
                BlankRow(driver);
            }
        }

        public void BlankRow(StandingRow row)
        {
            row.RowVisible = false;
            row.IsPlayer = false;
            row.IsHighlighted = false;
            row.PlayerID = string.Empty;
            row.Connected = false;
            row.LivePositionInClass = 0;
            row.PositionChange = 0;
            row.Number = string.Empty;
            row.Name = string.Empty;
            row.CarId = string.Empty;
            row.CarBrand = string.Empty;
            row.CarClassColor = string.Empty;
            row.CarClassTextColor = string.Empty;
            row.CountryCode = string.Empty;
            row.InPitLane = false;
            row.Towing = false;
            row.OutLap = false;
            row.EnterPitLap = 0;
            row.LastPitStopDuration = TimeSpan.Zero;
            row.iRating = 0;
            row.iRatingChange = 0;
            row.License = string.Empty;
            row.SafetyRating = 0;
            row.CurrentLap = 0;
            row.StintLap = 0;
            row.LapsToClassLeader = 0;
            row.GapToClassLeader = 0;
            row.DeltaToClassLeader = null;
            row.DeltaToPlayer = null;
            row.TireCompound = string.Empty;
            row.TireCompoundVisible = false;
            row.BestLapTime = TimeSpan.Zero;
            row.LastLapTime = TimeSpan.Zero;
            row.JokerLapsComplete = 0;
        }

        public (Driver, int) FindHighlightedDriver(ref GameData data)
        {
            int highlightedCarIdx = _driverModule.PlayerCarIdx;
            Driver highlightedDriver = null;

            _driverModule.DriversByCarIdx.TryGetValue(highlightedCarIdx, out highlightedDriver);           

            if (highlightedDriver != null)
            {
                for (int carClassIdx = 0; carClassIdx < data.NewData.OpponentsClassses.Count; carClassIdx++)
                {
                    var carClass = data.NewData.OpponentsClassses[carClassIdx];
                    List<Opponent> opponents = carClass.Opponents;
                    if (opponents.Count > 0)
                    {
                        if (opponents[0].CarClassID == highlightedDriver.CarClassId.ToString())
                        {
                            return (highlightedDriver, carClassIdx);
                        }
                    }
                }
            }

            return (null, -1);
        }

        public (string compound, bool visible) GetTireCompound(ref GameData data, int carIdx)
        {
            if (_carModule.TireCompounds == null)
                return ("", false);

            if (!RawDataHelper.TryGetTelemetryData<int>(ref data, out int tireCompoundIdx, "CarIdxTireCompound", carIdx))
                return ("", false);

            if (!_carModule.TireCompounds.TryGetValue(tireCompoundIdx, out string tireCompoundName))
                return ("", false);

            if (tireCompoundName == null || tireCompoundName.Length == 0)
                return ("", false);

            // Return the first letter of the compound name as a short representation
            return (tireCompoundName[0].ToString(), true);
        }

        public bool IsValidRow(Opponent opponent)
        {
            return opponent.PositionInClass > 0;
        }

        public int GetValidRowCount(OpponentsWithDrivers opponentsWithDrivers)
        {
            int validRowCount = 0;
            for (int opponentIdx = 0; opponentIdx < opponentsWithDrivers.Count; opponentIdx++)
            {
                if (IsValidRow(opponentsWithDrivers[opponentIdx].Item1)) { validRowCount++; }
            }
            return validRowCount;
        }

        public int GetLeadFocusedSkipRowCount(int highlightedCarIdx, OpponentsWithDrivers opponentsWithDrivers)
        {
            // Find the highlighted car in the opponent list
            int highlightedOpponentIdx = -1;
            for (int opponentIdx = 0; opponentIdx < opponentsWithDrivers.Count; opponentIdx++)
            {
                if (opponentsWithDrivers[opponentIdx].Item2.CarIdx == highlightedCarIdx)
                {
                    highlightedOpponentIdx = opponentIdx;
                    break;
                }
            }

            if (highlightedOpponentIdx < 0 || !IsValidRow(opponentsWithDrivers[highlightedOpponentIdx].Item1))
            {
                // Highlighted car not in opponent list
                return 0;
            }

            int validRowCount = GetValidRowCount(opponentsWithDrivers);
            if (validRowCount <= Settings.MaxRowsPlayerClass)
            {
                // They all fit in
                return 0;
            }

            int nonLeadFocusedRowCount = Math.Max(0, Settings.MaxRowsPlayerClass - Settings.LeadFocusedRows);
            if (highlightedOpponentIdx <= Settings.LeadFocusedRows + nonLeadFocusedRowCount / 2)
            {
                // Player already centered in top rows
                return 0;
            }

            // Center the player in the view by trying to keep an equal amount of rows before as after 
            int shown = Math.Max(0, validRowCount - Settings.LeadFocusedRows);
            int before = Math.Max(0, highlightedOpponentIdx - Settings.LeadFocusedRows);
            int after = Math.Max(0, validRowCount - highlightedOpponentIdx - 1);
            int skipRowCount = 0;

            // TODO make this O(1)
            while (shown > nonLeadFocusedRowCount)
            {
                if (before > after)
                {
                    before--;
                    shown--;
                    skipRowCount++;
                }
                else
                {
                    after--;
                    shown--;
                }
            }

            return skipRowCount;
        }

        public TimeSpan FindBestLapTime(OpponentsWithDrivers opponentsWithDrivers)
        {
            TimeSpan bestLapTime = TimeSpan.MaxValue;
            for (int opponentIdx = 0; opponentIdx < opponentsWithDrivers.Count; opponentIdx++)
            {
                Driver driver = opponentsWithDrivers[opponentIdx].Item2;
                if (driver.BestLapTime > TimeSpan.Zero && driver.BestLapTime < bestLapTime)
                {
                    bestLapTime = driver.BestLapTime;
                }
            }
            return bestLapTime < TimeSpan.MaxValue ? bestLapTime : TimeSpan.Zero;
        }

        public TimeSpan FindBestQualLapTime(OpponentsWithDrivers opponentsWithDrivers)
        {
            TimeSpan bestQualLapTime = TimeSpan.MaxValue;
            for (int opponentIdx = 0; opponentIdx < opponentsWithDrivers.Count; opponentIdx++)
            {
                Driver driver = opponentsWithDrivers[opponentIdx].Item2;
                if (driver.QualLapTime > TimeSpan.Zero && driver.QualLapTime < bestQualLapTime)
                {
                    bestQualLapTime = driver.QualLapTime;
                }
            }
            return bestQualLapTime < TimeSpan.MaxValue ? bestQualLapTime : TimeSpan.Zero;
        }

        public int CalculateSof(OpponentsWithDrivers opponentsWithDrivers)
        {
            if (opponentsWithDrivers.Count <= 0) 
                return 0;

            double sum = 0.0;
            foreach (var (opponent, driver) in opponentsWithDrivers)
            {
                sum += Math.Pow(2.0, -(opponent.IRacing_IRating ?? 0.0) / 1600.0);
            }

            return (int)((1600.0 / Math.Log(2.0)) * Math.Log(opponentsWithDrivers.Count / sum));
        }

        public int CalculateTotalSof(List<Opponent> opponents)
        {
            if (opponents.Count <= 0) 
                return 0;

            double sum = 0.0;
            foreach (var opponent in opponents)
            {
                sum += Math.Pow(2.0, -(opponent.IRacing_IRating ?? 0.0) / 1600.0);
            }

            return (int)((1600.0 / Math.Log(2.0)) * Math.Log(opponents.Count / sum));
        }

        public float MeasureTextInPixels(string text)
        {
            if (_fontBitmap == null || _fontGraphics == null || _font == null)
                return 0.0f;

            SizeF textSize;
            textSize = _fontGraphics.MeasureString(text, _font);
            return textSize.Width;
        }

        public void UpdateEstimatedTotalLaps(ref GameData data, StandingCarClass carClass, OpponentsWithDrivers opponentsWithDrivers)
        {
            if (_sessionModule.Race)
            {
                if (!_sessionModule.RaceStarted)
                    carClass.EstimatedTotalLapsConfirmed = false;

                if (carClass.EstimatedTotalLapsConfirmed)
                    return;

                double leaderCurrentLapHighPrecision = opponentsWithDrivers.Count > 0 ? opponentsWithDrivers[0].Item1.CurrentLapHighPrecision ?? 1.0 : 0.0;

                // Confirm the total laps when the player gets the white flag.
                // Because we don't have the data for other cars in the iRacing SDK.
                // TODO: Missing edge cases such as when the player gets lapped on the leader's last lap.
                // Or when the player tows before starting the last lap. Etc.
                bool playerStartingLastLap = _driverModule.PlayerHadWhiteFlag && _driverModule.PlayerCurrentLapHighPrecision % 1.0 < 0.50;
                if (playerStartingLastLap)
                {
                    carClass.EstimatedTotalLaps = (int)Math.Max(1, Math.Ceiling(leaderCurrentLapHighPrecision));
                    carClass.EstimatedTotalLapsConfirmed = true;
                    return;
                }

                double sessionTimeRemain = Math.Max(0.0, _sessionModule.SessionTimeTotal.TotalSeconds - _sessionModule.RaceTimer);
                TimeSpan avgLapTime = carClass.LeaderAvgLapTime;
                if (avgLapTime <= TimeSpan.Zero)
                    avgLapTime = carClass.BestLapTime;

                if (avgLapTime <= TimeSpan.Zero)
                    avgLapTime = carClass.BestQualLapTime;

                carClass.EstimatedTotalLaps = EstimateTotalLaps(leaderCurrentLapHighPrecision, _sessionModule.SessionLapsTotal, sessionTimeRemain, avgLapTime.TotalSeconds);
                return;
            }

            carClass.EstimatedTotalLaps = EstimateTotalLaps(_driverModule.PlayerCurrentLapHighPrecision, 
                _sessionModule.SessionLapsTotal,
                data.NewData.SessionTimeLeft.TotalSeconds, 
                _driverModule.PlayerBestLapTime.TotalSeconds);
        }

        public int EstimateTotalLaps(double currentLapHighPrecision, int sessionTotalLaps, double sessionTimeRemain, double avgLapTime)
        {
            // Even if it is a lapped race, check if there's enough time to complete the remaining laps.
            if (sessionTotalLaps > 0)
            {
                double lapsRemaining = Math.Max(0.0, sessionTotalLaps - currentLapHighPrecision);
                TimeSpan minTimeForLaps = TimeSpan.FromSeconds(lapsRemaining * avgLapTime);
                if (minTimeForLaps <= TimeSpan.FromSeconds(sessionTimeRemain))
                {
                    // We have enough time left to complete the remaining laps.
                    return sessionTotalLaps;
                }
            }

            if (avgLapTime <= 0)
                return 0;

            double estimatedTotalLaps = Math.Max(0.0, sessionTimeRemain / avgLapTime);
            estimatedTotalLaps += currentLapHighPrecision;

            // Add an extra lap if we would cross the line with more than 65% of a lap time remaining on the timer.
            // It is unknown what is the exact white flag rule used by iRacing. Best guess is 65% of avg time from last 3 laps.
            if (sessionTimeRemain > 0 && estimatedTotalLaps % 1.0 > 0.65)
            {
                estimatedTotalLaps++;
            }

            return (int)Math.Max(0, Math.Ceiling(estimatedTotalLaps));
        }
    }
}
