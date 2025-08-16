using GameReaderCommon;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using System;
using System.ComponentModel;

namespace InsanePlugin
{
    public class InputModule : PluginModuleBase
    {
        private const double mSteeringWheelAngle = 0.0;
        private const int mBrake = 0;
        private const int mThrottle = 0;
        private const int mClutch = 0;
        private const int mWaterTemperature = 0;
        private const int mOilTemperature = 0;
        private const int mRPM = 0;
        private const int mSpeed = 0;
        private const string mGear = "N";

        public double SteeringWheelAngle { get; set; } = mSteeringWheelAngle;
        public int Brake { get; set; } = mBrake;
        public int Throttle { get; set; } = mThrottle;
        public int Clutch { get; set; } = mClutch;
        public int WaterTemperature { get; set; } = mWaterTemperature;
        public int OilTemperature { get; set; } = mOilTemperature;
        public int RPM { get; set; } = mRPM;
        public int Speed { get; set; } = mSpeed;
        public string Gear { get; set; } = mGear;

        public override void Init(PluginManager pluginManager, InsanePlugin plugin)
        {
            plugin.AttachDelegate(name: "Input.SteeringWheelAngle", valueProvider: () => SteeringWheelAngle);
            plugin.AttachDelegate(name: "Input.Brake", valueProvider: () => Brake);
            plugin.AttachDelegate(name: "Input.Throttle", valueProvider: () => Throttle);
            plugin.AttachDelegate(name: "Input.Clutch", valueProvider: () => Clutch);
            plugin.AttachDelegate(name: "Input.WaterTemperature", valueProvider: () => WaterTemperature);
            plugin.AttachDelegate(name: "Input.OilTemperature", valueProvider: () => OilTemperature);
            plugin.AttachDelegate(name: "Input.RPM", valueProvider: () => RPM);
            plugin.AttachDelegate(name: "Input.Speed", valueProvider: () => Speed);
            plugin.AttachDelegate(name: "Input.Gear", valueProvider: () => Gear);
        }

        public override void DataUpdate(PluginManager pluginManager, InsanePlugin plugin, ref GameData data)
        {
            dynamic raw = data.NewData.GetRawDataObject();
            if (raw == null) return;

            double angle = 0.0;
            try { angle = (double)raw.Telemetry["SteeringWheelAngle"]; } catch { angle = 0.0; }
            this.SteeringWheelAngle = (angle * -1) * (180 / Math.PI);
            Brake = (int)data.NewData.Brake;
            Throttle = (int)data.NewData.Throttle;
            Clutch = (int)data.NewData.Clutch;
            WaterTemperature = (int)data.NewData.WaterTemperature;
            OilTemperature = (int)data.NewData.OilTemperature;
            RPM = (int)data.NewData.Rpms;
            Speed = (int)data.NewData.SpeedLocal;
            Gear = data.NewData.Gear;
        }

        public override void End(PluginManager pluginManager, InsanePlugin plugin)
        {
        }
    }
}
