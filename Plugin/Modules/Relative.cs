using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;

namespace InsanePlugin
{
    public class RelativeSettings : ModuleSettings
    {
        public bool HideInReplay { get; set; } = true;
        public int Width { get; set; } = 80;
        public int MaxRows { get; set; } = 4;
        public bool HeaderVisible { get; set; } = true;
        public int HeaderOpacity { get; set; } = 90;
        public bool FooterVisible { get; set; } = false;
        public bool CarLogoVisible { get; set; } = true;
        public bool CountryFlagVisible { get; set; } = true;
        public bool SafetyRatingVisible { get; set; } = true;
        public bool IRatingVisible { get; set; } = true;
        public bool IRatingChangeVisible { get; set; } = false;
        public int AlternateRowBackgroundColor { get; set; } = 5;
        public bool HighlightPlayerRow { get; set; } = true;
        public int BackgroundOpacity { get; set; } = 60;
    }

    public class RelativeRow
    {
        public bool RowVisible { get; set; } = false;
        public int LivePositionInClass { get; set; } = 0;
        public string ClassColor { get; set; } = string.Empty;
        public string ClassTextColor { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CarBrand { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public bool OutLap { get; set; } = false;
        public int iRating { get; set; } = 0;
        public float iRatingChange { get; set; } = 0;
        public string License { get; set; } = string.Empty;
        public double SafetyRating { get; set; } = 0;
        public double GapToPlayer { get; set; } = 0;
        public string GapToPlayerCombined { get; set; } = string.Empty;
        public double CurrentLapHighPrecision { get; set; } = 0;
        public TimeSpan LastLapTime { get; set; } = TimeSpan.Zero;
        public int SessionFlags { get; set; } = 0;
    }

    public class RelativeAhead
    {
        public const int MaxRows = 5;
        public List<RelativeRow> Rows { get; internal set; }

        public RelativeAhead()
        {
            Rows = new List<RelativeRow>(Enumerable.Range(0, MaxRows).Select(x => new RelativeRow()));
        }
    }
    public class RelativeBehind
    {
        public const int MaxRows = 5;
        public List<RelativeRow> Rows { get; internal set; }

        public RelativeBehind()
        {
            Rows = new List<RelativeRow>(Enumerable.Range(0, MaxRows).Select(x => new RelativeRow()));
        }
    }

    public class RelativeModule : PluginModuleBase
    {
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private TimeSpan _updateInterval = TimeSpan.FromMilliseconds(500);

        private DriverModule _driverModule = null;
        private CarModule _carModule = null;
        private FlairModule _flairModule = null;

        public RelativeSettings Settings { get; set; }

        public RelativeAhead Ahead = new RelativeAhead();
        public RelativeBehind Behind = new RelativeBehind();

        public override void Init(PluginManager pluginManager, InsanePluginMain plugin)
        {
            _driverModule = plugin.GetModule<DriverModule>();
            _carModule = plugin.GetModule<CarModule>();
            _flairModule = plugin.GetModule<FlairModule>();

            Settings = plugin.ReadCommonSettings<RelativeSettings>("RelativeSettings", () => new RelativeSettings());
            plugin.AttachDelegate(name: "Relative.HideInReplay", valueProvider: () => Settings.HideInReplay);
            plugin.AttachDelegate(name: "Relative.Width", valueProvider: () => Settings.Width);
            plugin.AttachDelegate(name: "Relative.MaxRows", valueProvider: () => Settings.MaxRows);
            plugin.AttachDelegate(name: "Relative.HeaderVisible", valueProvider: () => Settings.HeaderVisible);
            plugin.AttachDelegate(name: "Relative.HeaderOpacity", valueProvider: () => Settings.HeaderOpacity);
            plugin.AttachDelegate(name: "Relative.FooterVisible", valueProvider: () => Settings.FooterVisible);
            plugin.AttachDelegate(name: "Relative.CarLogoVisible", valueProvider: () => Settings.CarLogoVisible);
            plugin.AttachDelegate(name: "Relative.CountryFlagVisible", valueProvider: () => Settings.CountryFlagVisible);
            plugin.AttachDelegate(name: "Relative.SafetyRatingVisible", valueProvider: () => Settings.SafetyRatingVisible);
            plugin.AttachDelegate(name: "Relative.iRatingVisible", valueProvider: () => Settings.IRatingVisible);
            plugin.AttachDelegate(name: "Relative.iRatingChangeVisible", valueProvider: () => Settings.IRatingChangeVisible);
            plugin.AttachDelegate(name: "Relative.AlternateRowBackgroundColor", valueProvider: () => Settings.AlternateRowBackgroundColor);
            plugin.AttachDelegate(name: "Relative.HighlightPlayerRow", valueProvider: () => Settings.HighlightPlayerRow);
            plugin.AttachDelegate(name: "Relative.BackgroundOpacity", valueProvider: () => Settings.BackgroundOpacity);

            InitRelative(plugin, "Ahead", Ahead.Rows, RelativeAhead.MaxRows);
            InitRelative(plugin, "Behind", Behind.Rows, RelativeBehind.MaxRows);
        }

        private void InitRelative(InsanePluginMain plugin, string aheadBehind, List<RelativeRow> rows, int maxRows)
        {
            for (int rowIdx = 0; rowIdx < maxRows; rowIdx++)
            {
                RelativeRow row = rows[rowIdx];
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.RowVisible", valueProvider: () => row.RowVisible);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.LivePositionInClass", valueProvider: () => row.LivePositionInClass);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.ClassColor", valueProvider: () => row.ClassColor);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.ClassTextColor", valueProvider: () => row.ClassTextColor);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.Number", valueProvider: () => row.Number);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.Name", valueProvider: () => row.Name);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.CarBrand", valueProvider: () => row.CarBrand);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.CountryCode", valueProvider: () => row.CountryCode);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.OutLap", valueProvider: () => row.OutLap);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.iRating", valueProvider: () => row.iRating);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.iRatingChange", valueProvider: () => row.iRatingChange);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.License", valueProvider: () => row.License);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.SafetyRating", valueProvider: () => row.SafetyRating);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.GapToPlayer", valueProvider: () => row.GapToPlayer);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.GapToPlayerCombined", valueProvider: () => row.GapToPlayerCombined);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.CurrentLapHighPrecision", valueProvider: () => row.CurrentLapHighPrecision);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.LastLapTime", valueProvider: () => row.LastLapTime);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}{rowIdx:00}.SessionFlags", valueProvider: () => row.SessionFlags);
            }
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePluginMain plugin, ref GameData data)
        {
            if (data.FrameTime - _lastUpdateTime < _updateInterval) return;
            _lastUpdateTime = data.FrameTime;

            UpdateRelative(ref data, Ahead.Rows, RelativeAhead.MaxRows, data.NewData.OpponentsAheadOnTrack);
            UpdateRelative(ref data, Behind.Rows, RelativeBehind.MaxRows, data.NewData.OpponentsBehindOnTrack);
        }

        public void UpdateRelative(ref GameData data, List<RelativeRow> rows, int maxRows, List<Opponent> opponents)
        {
            for (int rowIdx = 0; rowIdx < maxRows; rowIdx++)
            {
                RelativeRow row = rows[rowIdx];

                if (rowIdx >= opponents.Count)
                {
                    BlankRow(row);
                    continue;
                }

                Opponent opponent = opponents[rowIdx];
                Driver driver = null;
                if (_driverModule.Drivers != null) _driverModule.Drivers.TryGetValue(opponent.CarNumber, out driver);

                if (driver == null || !IsValidRow(opponent))
                {
                    BlankRow(row);
                    continue;
                }

                row.RowVisible = true;
                row.LivePositionInClass = driver.LivePositionInClass;
                row.ClassColor = opponent.CarClassColor;
                row.ClassTextColor = opponent.CarClassTextColor;
                row.Number = opponent.CarNumber;
                row.Name = opponent.Name;
                row.CarBrand = _carModule.GetCarBrand(driver.CarId, opponent.CarName); ;
                row.CountryCode = _flairModule.GetCountryCode(driver.FlairId);
                row.OutLap = driver.OutLap;
                row.iRating = (int)(opponent.IRacing_IRating ?? 0);
                row.iRatingChange = driver.IRatingChange;
                (row.License, row.SafetyRating) = DriverModule.ParseLicenseString(opponent.LicenceString);
                row.GapToPlayer = opponent.RelativeGapToPlayer ?? 0;
                row.GapToPlayerCombined = opponent.GapToPlayerCombined;
                row.CurrentLapHighPrecision = opponent.CurrentLapHighPrecision ?? 0;
                row.LastLapTime = driver.LastLapTime;
                row.SessionFlags = driver.SessionFlags;
            }
        }

        public override void End(PluginManager pluginManager, InsanePluginMain plugin)
        {
            plugin.SaveCommonSettings("RelativeSettings", Settings);
        }

        public void BlankRow(RelativeRow row)
        {
            row.RowVisible = false;
            row.LivePositionInClass = 0;
            row.ClassColor = string.Empty;
            row.ClassTextColor = string.Empty;
            row.Number = string.Empty;
            row.Name = string.Empty;
            row.CarBrand = string.Empty;
            row.CountryCode = string.Empty;
            row.OutLap = false;
            row.iRating = 0;
            row.iRatingChange = 0;
            row.License = string.Empty;
            row.SafetyRating = 0;
            row.GapToPlayer = 0;
            row.GapToPlayerCombined = string.Empty;
            row.CurrentLapHighPrecision = 0;
            row.LastLapTime = TimeSpan.Zero;
            row.SessionFlags = 0;
        }
        public bool IsValidRow(Opponent opponent)
        {
            return true;
        }
    }
}
