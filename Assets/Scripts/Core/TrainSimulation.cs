using System;

namespace Trackbound.Core
{
    public sealed class TrainSimulation
    {
        private const double MaximumSpeed = 22.0;
        private const double MaximumAcceleration = 1.25;
        private const double BrakeDeceleration = 3.8;
        private const double FuelPerSecondAtFullLoad = 0.18;

        public double SpeedMetersPerSecond { get; private set; }
        public double FuelChunks { get; private set; }
        public double Throttle { get; private set; }
        public double Brake { get; private set; }

        public TrainSimulation(double initialFuelChunks)
        {
            FuelChunks = Math.Max(0, initialFuelChunks);
        }

        public void SetControls(double throttle, double brake)
        {
            Throttle = Clamp01(throttle);
            Brake = Clamp01(brake);
        }

        public void AddFuel(double chunks)
        {
            if (chunks > 0)
            {
                FuelChunks += chunks;
            }
        }

        public void RestoreState(double speed, double fuel, double throttle, double brake)
        {
            SpeedMetersPerSecond = Math.Clamp(speed, 0, MaximumSpeed);
            FuelChunks = Math.Max(0, fuel);
            Throttle = Clamp01(throttle);
            Brake = Clamp01(brake);
        }

        public void Tick(double deltaSeconds, double trackGrade = 0)
        {
            if (deltaSeconds <= 0)
            {
                return;
            }

            var availableThrottle = FuelChunks > 0 ? Throttle : 0;
            FuelChunks = Math.Max(0, FuelChunks - availableThrottle * FuelPerSecondAtFullLoad * deltaSeconds);

            var acceleration = availableThrottle * MaximumAcceleration
                - Brake * BrakeDeceleration
                - 9.81 * Math.Clamp(trackGrade, -0.05, 0.05)
                - 0.025;

            SpeedMetersPerSecond = Math.Clamp(
                SpeedMetersPerSecond + acceleration * deltaSeconds,
                0,
                MaximumSpeed);
        }

        private static double Clamp01(double value)
        {
            return Math.Clamp(value, 0, 1);
        }
    }
}