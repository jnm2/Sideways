﻿namespace Sideways.Tests
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using NUnit.Framework;

    using Sideways.AlphaVantage;

    public static class DatabaseTests
    {
        private static readonly ImmutableArray<AdjustedCandle> DayCandles = ImmutableArray.Create(new AdjustedCandle(new DateTimeOffset(2021, 04, 15, 00, 00, 00, 0, TimeSpan.Zero), 1.2f, 2.3f, 3.4f, 4.5f, 5.6f, 7, 8.9f, 9.1f));
        private static readonly ImmutableArray<Candle> MinuteCandles = ImmutableArray.Create(new Candle(new DateTimeOffset(2021, 04, 15, 00, 00, 00, 0, TimeSpan.Zero), 1.2f, 2.3f, 3.4f, 4.5f, 6));
        private static readonly FileInfo DbFile = new(Path.Combine(Path.GetTempPath(), "Sideways", "Database.sqlite3"));

        [Test]
        public static void Days()
        {
            Database.WriteDays("UNIT_TEST", DayCandles, DbFile);

            var candles = Database.ReadDays("UNIT_TEST", DbFile);
            CollectionAssert.AreEqual(DayCandles.Select(x => x.AsCandle(1)).OrderBy(x => x.Time), candles.OrderBy(x => x.Time));

            candles = Database.ReadDays("UNIT_TEST", DayCandles.Min(x => x.Time), DayCandles.Max(x => x.Time), DbFile);
            CollectionAssert.AreEqual(DayCandles.Select(x => x.AsCandle(1)).OrderBy(x => x.Time), candles.OrderBy(x => x.Time));

            Database.WriteDays("UNIT_TEST", DayCandles.Select(x => x.AsCandle(2)), DbFile);
            candles = Database.ReadDays("UNIT_TEST", DbFile);
            CollectionAssert.AreEqual(DayCandles.Select(x => x.AsCandle(1)).OrderBy(x => x.Time), candles.OrderBy(x => x.Time));
        }

        [Test]
        public static void Minutes()
        {
            Database.WriteMinutes("UNIT_TEST", MinuteCandles, DbFile);

            var read = Database.ReadMinutes("UNIT_TEST", DbFile);
            CollectionAssert.AreEqual(MinuteCandles.OrderBy(x => x.Time), read.OrderBy(x => x.Time));

            read = Database.ReadMinutes("UNIT_TEST", DayCandles.Min(x => x.Time), DayCandles.Max(x => x.Time), DbFile);
            CollectionAssert.AreEqual(MinuteCandles.OrderBy(x => x.Time), read.OrderBy(x => x.Time));
        }

        [Test]
        public static void AnnualEarnings()
        {
            var earnings = ImmutableArray.Create(
                new AnnualEarning(
                    new DateTimeOffset(2021, 03, 31, 00, 00, 00, 0, TimeSpan.Zero),
                    1.77f),
                new AnnualEarning(
                    new DateTimeOffset(2020, 12, 31, 00, 00, 00, 0, TimeSpan.Zero),
                    8.67f),
                new AnnualEarning(
                    new DateTimeOffset(2019, 12, 31, 00, 00, 00, 0, TimeSpan.Zero),
                    12.81f),
                new AnnualEarning(
                    new DateTimeOffset(2018, 12, 31, 00, 00, 00, 0, TimeSpan.Zero),
                    13.82f));
            Database.WriteAnnualEarnings("UNIT_TEST", earnings, DbFile);

            var read = Database.ReadAnnualEarnings("UNIT_TEST", DbFile);
            CollectionAssert.AreEqual(earnings.OrderBy(x => x.FiscalDateEnding), read.OrderBy(x => x.FiscalDateEnding));
        }

        [Test]
        public static void QuarterlyEarnings()
        {
            var earnings = ImmutableArray.Create(
                new QuarterlyEarning(
                    new DateTimeOffset(2021, 03, 31, 00, 00, 00, 0, TimeSpan.Zero),
                    new DateTimeOffset(2021, 04, 19, 00, 00, 00, 0, TimeSpan.Zero),
                    1.77f,
                    1.6524f),
                new QuarterlyEarning(
                    new DateTimeOffset(2020, 12, 31, 00, 00, 00, 0, TimeSpan.Zero),
                    new DateTimeOffset(2020, 01, 21, 00, 00, 00, 0, TimeSpan.Zero),
                    2.08f,
                    2.07f),
                new QuarterlyEarning(
                    new DateTimeOffset(2020, 09, 30, 00, 00, 00, 0, TimeSpan.Zero),
                    new DateTimeOffset(2020, 10, 19, 00, 00, 00, 0, TimeSpan.Zero),
                    2.58f,
                    2.579f),
                new QuarterlyEarning(
                    new DateTimeOffset(2020, 06, 30, 00, 00, 00, 0, TimeSpan.Zero),
                    new DateTimeOffset(2020, 07, 20, 00, 00, 00, 0, TimeSpan.Zero),
                    2.18f,
                    2.0851f));
            Database.WriteQuarterlyEarnings("UNIT_TEST", earnings, DbFile);

            var read = Database.ReadQuarterlyEarnings("UNIT_TEST", DbFile);
            CollectionAssert.AreEqual(earnings.OrderBy(x => x.FiscalDateEnding), read.OrderBy(x => x.FiscalDateEnding));
        }

        [Explicit]
        [Test]
        public static void Timings()
        {
            var stopwatch = Stopwatch.StartNew();
            var symbols = Database.ReadSymbols();
            stopwatch.Stop();
            Console.WriteLine($"Read {symbols.Length} symbols took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            var days = Database.ReadDays("TSLA");
            stopwatch.Stop();
            Console.WriteLine($"Read {days.Count} days took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            var dayRanges = Database.DayRanges();
            stopwatch.Stop();
            Console.WriteLine($"Read {dayRanges.Count} day ranges took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            var minutes = Database.ReadMinutes("TSLA");
            stopwatch.Stop();
            Console.WriteLine($"Read {minutes.Count} minutes took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            minutes = Database.ReadMinutes("TSLA", new DateTimeOffset(2021, 01, 01, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2021, 02, 01, 0, 0, 0, TimeSpan.Zero));
            stopwatch.Stop();
            Console.WriteLine($"Read {minutes.Count} minutes took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            minutes = Database.ReadMinutes("TSLA", new DateTimeOffset(2021, 01, 01, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2021, 01, 07, 0, 0, 0, TimeSpan.Zero));
            stopwatch.Stop();
            Console.WriteLine($"Read {minutes.Count} minutes took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            var n = Database.CountMinutes("TSLA");
            stopwatch.Stop();
            Console.WriteLine($"Count {n} minutes took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            _ = Database.FirstMinute("TSLA");
            stopwatch.Stop();
            Console.WriteLine($"FirstMinute(TSLA) took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            _ = Database.LastMinute("TSLA");
            stopwatch.Stop();
            Console.WriteLine($"LastMinute(TSLA) took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            _ = Database.MinuteRange("TSLA");
            stopwatch.Stop();
            Console.WriteLine($"MinuteRange(TSLA) took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            var minuteRanges = Database.MinuteRanges(symbols);
            stopwatch.Stop();
            Console.WriteLine($"Read {minuteRanges.Count} minute ranges took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();
            minuteRanges = Database.MinuteRanges();
            stopwatch.Stop();
            Console.WriteLine($"Read {minuteRanges.Count} minute ranges took {stopwatch.ElapsedMilliseconds} ms.");
        }
    }
}
