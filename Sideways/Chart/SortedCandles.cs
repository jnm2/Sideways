﻿namespace Sideways
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    public class SortedCandles : IEnumerable<Candle>
    {
        private readonly ImmutableArray<Candle> candles;
        private readonly ImmutableArray<Split> splits;

        public SortedCandles(ImmutableArray<Candle> candles, ImmutableArray<Split> splits)
        {
            Debug.Assert(Enumerable.SequenceEqual(candles, candles.OrderByDescending(x => x.Time)), "Candles not sorted correctly.");
            Debug.Assert(Enumerable.SequenceEqual(splits, splits.OrderByDescending(x => x.Date)), "Splits not sorted correctly.");
            this.candles = candles;
            this.splits = splits;
        }

        public IEnumerable<Candle> Get(DateTimeOffset start, CandleGrouping grouping)
        {
            return grouping switch
            {
                CandleGrouping.None => this.Get(start),
                CandleGrouping.Week => this.Weeks(start),
                _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unhandled grouping."),
            };
        }

        public IEnumerable<Candle> Get(DateTimeOffset start)
        {
            for (var i = Start(); i < this.candles.Length; i++)
            {
                var candle = this.candles[i];
                if (candle.Time > start)
                {
                    continue;
                }

                yield return candle;
            }

            int Start()
            {
                var indexOf = this.IndexOf(start);
                return indexOf > 0 ? indexOf : 0;
            }
        }

        public IEnumerable<Candle> GetSplitAdjusted(DateTimeOffset start)
        {
            throw new NotImplementedException();
            //var splitCoefficient = 1f;
            //foreach (var day in candles)
            //{
            //    if (day.SplitCoefficient != 0)
            //    {
            //        splitCoefficient *= day.SplitCoefficient;
            //    }

            //    yield return day.AsCandle(splitCoefficient);
            //}
        }

        public IEnumerable<Candle> Weeks(DateTimeOffset start)
        {
            var week = new List<Candle>();
            foreach (var day in this.Get(start))
            {
                if (week.Count == 0 ||
                    Week(week[0]) == Week(day))
                {
                    week.Add(day);
                }
                else
                {
                    if (week.Count > 0)
                    {
                        yield return Candle.Create(week);
                    }

                    week.Clear();
                    week.Add(day);
                }

                static int Week(Candle c) => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(c.Time.Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            }

            if (week.Count > 0)
            {
                yield return Candle.Create(week);
            }
        }

        IEnumerable<Candle> Hours(DateTimeOffset start)
        {
            var hour = new List<Candle>();
            foreach (var minute in this.Get(start))
            {
                if (hour.Count == 0 ||
                    Hour(hour[0]) == Hour(minute))
                {
                    hour.Add(minute);
                }
                else
                {
                    if (hour.Count > 0)
                    {
                        yield return Candle.Create(hour);
                    }

                    hour.Clear();
                    hour.Add(minute);
                }

                static int Hour(Candle c) => c.Time.DayOfYear + c.Time.Hour;
            }

            if (hour.Count > 0)
            {
                yield return Candle.Create(hour);
            }
        }

        public Candle? Previous(DateTimeOffset time)
        {
            foreach (var candle in this.candles)
            {
                if (candle.Time < time)
                {
                    return candle;
                }
            }

            return null;
        }

        public Candle? Next(DateTimeOffset time)
        {
            for (var i = this.candles.Length - 1; i >= 0; i--)
            {
                var candle = this.candles[i];
                if (candle.Time > time)
                {
                    return candle;
                }
            }

            return null;
        }

        public int IndexOf(DateTimeOffset time)
        {
            if (this.candles.IsEmpty)
            {
                return -1;
            }

            var i = Math.Min((int)new TimeRange(this.candles[^1].Time, this.candles[0].Time).Interpolate(time) * this.candles.Length, this.candles.Length - 1);

            switch (time.CompareTo(this.candles[i].Time))
            {
                case 0:
                    return i;
                case < 0:
                    for (var j = i; j < this.candles.Length; j++)
                    {
                        switch (time.CompareTo(this.candles[j].Time))
                        {
                            case 0:
                                return j;
                            case < 0:
                                continue;
                            case > 0:
                                return -1;
                        }
                    }

                    return -1;
                case > 0:
                    for (var j = i; j >= 0; j--)
                    {
                        switch (time.CompareTo(this.candles[j].Time))
                        {
                            case 0:
                                return j;
                            case > 0:
                                continue;
                            case < 0:
                                return -1;
                        }
                    }

                    return -1;
            }
        }

        public IEnumerator<Candle> GetEnumerator() => ((IEnumerable<Candle>)this.candles).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.candles).GetEnumerator();
    }
}
