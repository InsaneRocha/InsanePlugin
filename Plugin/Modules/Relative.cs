using GameReaderCommon;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;
using SimHub.Plugins.OutputPlugins.Nextion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using Opponent = GameReaderCommon.Opponent;

namespace InsanePlugin
{
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
        public double InsaneGapToPlayer { get; set; } = 0;
        public int PlayerGainTime { get; set; } = 0;
        public string GapToPlayerCombined { get; set; } = string.Empty;
        public double CurrentLapHighPrecision { get; set; } = 0;
        public TimeSpan LastLapTime { get; set; } = TimeSpan.Zero;
        public int SessionFlags { get; set; } = 0;
        public bool IsInPit { get; set; } = false;
    }

    public class RelativeAhead
    {
        public List<RelativeRow> Rows { get; internal set; }

        public RelativeAhead()
        {
            Rows = new List<RelativeRow>(Enumerable.Range(0, 3).Select(x => new RelativeRow()));
        }
    }
    public class RelativeBehind
    {
        public List<RelativeRow> Rows { get; internal set; }

        public RelativeBehind()
        {
            Rows = new List<RelativeRow>(Enumerable.Range(0, 3).Select(x => new RelativeRow()));
        }
    }

    public class RelativeModule : PluginModuleBase
    {
        private DateTime mLastUpdateTime = DateTime.MinValue;
        private TimeSpan mUpdateInterval = TimeSpan.FromMilliseconds(100);

        private DriverModule mDriverModule = null;
        private CarModule mCarModule = null;
        private FlairModule mFlairModule = null;

        public RelativeAhead Ahead = new RelativeAhead();
        public RelativeBehind Behind = new RelativeBehind();

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            mDriverModule = plugin.GetModule<DriverModule>();
            mCarModule = plugin.GetModule<CarModule>();
            mFlairModule = plugin.GetModule<FlairModule>();

            InitRelative(plugin, "Ahead", Ahead.Rows);
            InitRelative(plugin, "Behind", Behind.Rows);
        }

        private void InitRelative(InsanePlugin plugin, string aheadBehind, List<RelativeRow> rows)
        {
            for (int rowIdx = 0; rowIdx < 3; rowIdx++)
            {
                RelativeRow row = rows[rowIdx];
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.RowVisible.{rowIdx:0}", valueProvider: () => row.RowVisible);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.LivePositionInClass.{rowIdx:0}", valueProvider: () => row.LivePositionInClass);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.ClassColor.{rowIdx:0}", valueProvider: () => row.ClassColor);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.ClassTextColor.{rowIdx:0}", valueProvider: () => row.ClassTextColor);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.Number.{rowIdx:0}", valueProvider: () => row.Number);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.Name.{rowIdx:0}", valueProvider: () => row.Name);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.CarBrand.{rowIdx:0}", valueProvider: () => row.CarBrand);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.CountryCode.{rowIdx:0}", valueProvider: () => row.CountryCode);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.OutLap.{rowIdx:0}", valueProvider: () => row.OutLap);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.iRating.{rowIdx:0}", valueProvider: () => row.iRating);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.iRatingChange.{rowIdx:0}", valueProvider: () => row.iRatingChange);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.License.{rowIdx:0}", valueProvider: () => row.License);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.SafetyRating.{rowIdx:0}", valueProvider: () => row.SafetyRating);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.GapToPlayer.{rowIdx:0}", valueProvider: () => row.GapToPlayer);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.InsaneGapToPlayer.{rowIdx:0}", valueProvider: () => row.InsaneGapToPlayer);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.PlayerGainTime.{rowIdx:0}", valueProvider: () => row.PlayerGainTime);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.GapToPlayerCombined.{rowIdx:0}", valueProvider: () => row.GapToPlayerCombined);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.CurrentLapHighPrecision.{rowIdx:0}", valueProvider: () => row.CurrentLapHighPrecision);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.LastLapTime.{rowIdx:0}", valueProvider: () => row.LastLapTime);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.SessionFlags.{rowIdx:0}", valueProvider: () => row.SessionFlags);
                plugin.AttachDelegate(name: $"Relative.{aheadBehind}.IsInPit.{rowIdx:0}", valueProvider: () => row.IsInPit);
            }
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {
            if (data.FrameTime - mLastUpdateTime < mUpdateInterval) return;
            mLastUpdateTime = data.FrameTime;

            if (mDriverModule.Drivers != null)
            {
                List<Driver> driversAhead = mDriverModule.Drivers
                    .Values
                    .Where(d => d.DistanceToPlayer > 0 && d.CarClassId != 11)
                    .OrderBy(d => d.DistanceToPlayer)
                    .Take(3)
                    .ToList();


                List<Driver> driversbehind = mDriverModule.Drivers
                    .Values
                    .Where(d => d.DistanceToPlayer < 0 && d.CarClassId != 11)
                    .OrderByDescending(d => d.DistanceToPlayer)
                    .Take(3)
                    .ToList();

                if (driversAhead.Count < 3)
                {
                    do {
                        driversAhead.Add(null);
                    } while (driversAhead.Count != 3);
                }

                if (driversbehind.Count < 3)
                {
                    do
                    {
                        driversbehind.Add(null);
                    } while (driversbehind.Count != 3);
                }

                UpdateRelative(ref data, Ahead.Rows, driversAhead, data.NewData.Opponents);
                UpdateRelative(ref data, Behind.Rows, driversbehind, data.NewData.Opponents);
            }
        }

        public void UpdateRelative(ref GameData data, List<RelativeRow> rows, List<Driver> drivers, List<Opponent> opponents)
        {
            for (int rowIdx = 0; rowIdx < 3; rowIdx++)
            {
                RelativeRow row = rows[rowIdx];

                if (rowIdx >= drivers.Count)
                {
                    BlankRow(row);
                    continue;
                }

                Driver driver = drivers[rowIdx];
                if (driver == null)
                {
                    BlankRow(row);
                    continue;
                }

                Opponent opponent = opponents.First(d => d.CarNumber == driver.CarNumber);
                if (opponent == null)
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
                row.CarBrand = mCarModule.GetCarBrand(driver.CarId, opponent.CarName); ;
                row.iRating = (int)(opponent.IRacing_IRating ?? 0);
                (row.License, row.SafetyRating) = DriverModule.ParseLicenseString(opponent.LicenceString);
                row.CountryCode = mFlairModule.GetCountryCode(driver.FlairId);
                row.GapToPlayerCombined = opponent.GapToPlayerCombined;
                row.CurrentLapHighPrecision = opponent.CurrentLapHighPrecision ?? 0;
                row.GapToPlayer = opponent.RelativeGapToPlayer ?? 0;
                row.OutLap = driver.OutLap;
                row.iRatingChange = driver.IRatingChange;
                row.InsaneGapToPlayer = driver.GapToPlayer;
                row.PlayerGainTime = driver.PlayerGainTime;
                row.LastLapTime = driver.LastLapTime;
                row.SessionFlags = driver.SessionFlags;
                row.IsInPit = driver.IsInPit;
            }
        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
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
            row.InsaneGapToPlayer = 0;
            row.GapToPlayerCombined = string.Empty;
            row.CurrentLapHighPrecision = 0;
            row.LastLapTime = TimeSpan.Zero;
            row.SessionFlags = 0;
            row.IsInPit = false;
        }
    }
}
