﻿namespace Sideways.Scan
{
    public sealed class AverageVolumeCriteria : Criteria
    {
        private float? min;
        private float? max;

        public override string Info => (this.Min, this.Max) switch
        {
            // ReSharper disable LocalVariableHidesMember
            (Min: { } min, Max: { } max) => $"AV [{min:F1}..{max:F1}]",
            (Min: null, Max: { } max) => $"AV [..{max:F1}]",
            (Min: { } min, Max: null) => $"AV [{min:F1}..]",
            _ => "AV *",
        };

        public override int ExtraDays => this.IsActive ? 20 : 0;

        public float? Min
        {
            get => this.min;
            set
            {
                if (value == this.min)
                {
                    return;
                }

                this.min = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.Info));
            }
        }

        public float? Max
        {
            get => this.max;
            set
            {
                if (value == this.max)
                {
                    return;
                }

                this.max = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.Info));
            }
        }

        public override bool IsSatisfied(SortedCandles candles, int index)
        {
            return (this.IsActive, this.Min, this.Max) switch
            {
                // ReSharper disable LocalVariableHidesMember
                (IsActive: true, Min: { } min, Max: { } max) => IsBetween(AverageVolume(), min, max),
                (IsActive: true, Min: null, Max: { } max) => AverageVolume() <= max,
                (IsActive: true, Min: { } min, Max: null) => AverageVolume() >= min,
                _ => true,
            };

            static bool IsBetween(float adr, float min, float max) => adr >= min && adr <= max;

            float AverageVolume() => candles.AsSpan()[^20..].Average(x => x.Volume);
        }
    }
}
