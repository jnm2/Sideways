﻿// ReSharper disable UnusedMember.Global
namespace Sideways.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public static class BackTest
    {
        private static readonly string[] Symbols = Download.Symbols;

        [TestCaseSource(nameof(Symbols))]
        public static async Task GapUp(string symbol)
        {
            const double gap = 0.10;
            Console.WriteLine($"{symbol} {100 * gap}% gap ups. Buy at open sell at close three days later.");
            var candles = await Database.ReadDaysAsync(symbol);
            for (var i = 1; i < candles.Length - 5; i++)
            {
                var candle = candles[i];
                if (Gap() > gap)
                {
                    Console.WriteLine($"{candle.Time:yyyy-MM-dd} {100 * (candles[i + 5].Close - candle.Open) / candle.Open,6:F1}%");
                }

                double Gap()
                {
                    return (candle.Open - candles[i - 1].High) / candle.Open;
                }
            }
        }

        [Test]
        public static async Task AllGapUps()
        {
            const double minGap = 0.05;
            Console.WriteLine("symbol;date;gap;relative_volume;relative_close;three_day;five_day;link");
            foreach (var symbol in Symbols)
            {
                var candles = await Database.ReadDaysAsync(symbol);
                for (var i = 10; i < candles.Length - 5; i++)
                {
                    var candle = candles[i];
                    var gap = Percent(candles[i - 1].High, candle.Open);
                    if (gap > minGap)
                    {
                        Console.WriteLine($"{symbol};{candle.Time:yyyy-MM-dd};{gap};{RelativeVolume()};{RelativeClose()};{Percent(candle.Open, candles[i + 3].Close)};{Percent(candle.Open, candles[i + 5].Close)};https://www.tradingview.com/chart/?symbol={symbol}&interval=1D&date={candle.Time:yyyy-MM-dd}");
                    }

                    double Percent(float start, float end)
                    {
                        return (end - start) / start;
                    }

                    double RelativeVolume()
                    {
                        return candle.Volume / Slice(candles, i - 10, i - 1).Average(x => x.Volume);
                    }

                    double RelativeClose()
                    {
                        return (candle.Close - candle.Low) / (candle.High - candle.Low);
                    }

                    static IEnumerable<AdjustedCandle> Slice(ImmutableArray<AdjustedCandle> source, int from, int to)
                    {
                        for (var i = from; i <= to; i++)
                        {
                            yield return source[i];
                        }
                    }
                }
            }
        }
    }
}