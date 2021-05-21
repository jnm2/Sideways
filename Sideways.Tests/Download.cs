﻿namespace Sideways.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using Sideways.AlphaVantage;

    [Explicit]
    public static class Download
    {
        public static readonly string[] Symbols = MissingMinutes().ToArray();

        private static readonly TestCaseData[] SymbolsAndSlices = Symbols.Take(500 / 24).SelectMany(x => Enum.GetValues(typeof(Slice)).Cast<Slice>().Select(y => new TestCaseData(x, y))).ToArray();

        private static readonly Downloader Downloader = new();
        private static readonly AlphaVantageClient Client = new(new HttpClientHandler(), ApiKey);

        private static string ApiKey
        {
            get
            {
                var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sideways/AlphaVantage.key");
                if (File.Exists(fileName))
                {
                    return File.ReadAllText(fileName).Trim();
                }

                throw new InvalidOperationException($"Expected the API key in {fileName}");
            }
        }

        [Test]
        public static void Sort()
        {
            var old = Database.ReadSymbols();
            foreach (var symbol in Symbols.Except(old).OrderBy(x => x))
            {
                Console.WriteLine($"            \"{symbol}\",");
            }
        }

        [Test]
        public static async Task Listings()
        {
            using var client = new AlphaVantageClient(new HttpClientHandler(), ApiKey);
            var listings = await client.ListingsAsync().ConfigureAwait(false);
            Database.WriteListings(listings);
        }

        [TestCaseSource(nameof(Symbols))]
        public static async Task Days(string symbol)
        {
            var dataSource = new DataSource(Downloader);
            if (dataSource.Days(symbol, Client).Download is { } task)
            {
                await task;
            }
            else
            {
                Assert.Pass("Already downloaded.");
            }
        }

        [TestCaseSource(nameof(SymbolsAndSlices))]
        public static async Task Minutes(string symbol, Slice slice)
        {
            var range = TimeRange.FromSlice(slice);
            var days = Database.ReadDays(symbol, range.Min, range.Max).Select(x => TradingDay.Create(x.Time)).Distinct().ToArray();
            if (days.Length == 0)
            {
                if (Database.ReadDays(symbol).Count == 0)
                {
                    Assert.Inconclusive("Download days first");
                }

                Assert.Pass("No days slice this far back.");
            }

            var minutes = Database.ReadMinutes(symbol, range.Min, range.Max).Select(x => TradingDay.Create(x.Time)).Distinct().ToArray();
            if (minutes.Length == 0 &&
                slice != Slice.Year1Month1 &&
                Database.ReadMinutes(symbol, range.Min.AddDays(1), range.Max.AddDays(10)).Count == 0)
            {
                Assert.Pass("No slice this far back.");
            }

            if (!days.SequenceEqual(minutes))
            {
                using var client = new AlphaVantageClient(new HttpClientHandler(), ApiKey);
                var candles = await client.IntervalExtendedAsync(symbol, Interval.Minute, slice, adjusted: false);
                if (candles.IsEmpty)
                {
                    Assert.Inconclusive("Empty slice, maybe missing data on AlphaVantage. Exclude this symbol from script as it uses up daily calls.");
                }

                Database.WriteMinutes(symbol, candles);
            }
            else
            {
                Assert.Pass("Already downloaded.");
            }
        }

        private static IEnumerable<string> All() => Database.ReadSymbols();

        private static IEnumerable<string> MissingMinutes() => Database.ReadSymbols().Where(x => Sideways.Sync.CountMinutes(x, Database.DbFile) == 0);
    }
}
