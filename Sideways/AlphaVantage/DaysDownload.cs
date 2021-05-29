﻿namespace Sideways.AlphaVantage
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class DaysDownload : Download
    {
        private readonly Downloader downloader;

        public DaysDownload(string symbol, OutputSize outputSize, Downloader downloader)
            : base(symbol)
        {
            this.downloader = downloader;
            this.OutputSize = outputSize;
        }

        public OutputSize OutputSize { get; }

        public static DaysDownload? TryCreate(string symbol, TimeRange dayRange, Downloader downloader, AlphaVantageSettings settings)
        {
            if (TradingDay.From(dayRange.Max) < TradingDay.LastComplete() &&
                !settings.UnlistedSymbols.Contains(symbol))
            {
                return Create(symbol, dayRange, downloader);
            }

            return null;
        }

        public static DaysDownload Create(string symbol, TimeRange dayRange, Downloader downloader)
        {
            return new DaysDownload(symbol, OutputSize(), downloader);

            OutputSize OutputSize()
            {
                // Compact returns only last 100, below can be tweaked further as it includes holidays but good enough for now
                if (dayRange.Max is { Year: var y, Month: var m, Day: var d } &&
                    DateTime.Today - new DateTime(y, m, d) < TimeSpan.FromDays(100))
                {
                    return AlphaVantage.OutputSize.Compact;
                }

                return AlphaVantage.OutputSize.Full;
            }
        }

        public async Task<int> ExecuteAsync()
        {
            this.downloader.Add(this);
            this.State.Start = DateTimeOffset.Now;

            try
            {
                var candles = await this.downloader.Client.DailyAdjustedAsync(this.Symbol, this.OutputSize).ConfigureAwait(false);
                if (TradingDay.From(candles.Max(x => x.Time).AddDays(5)) < TradingDay.LastComplete())
                {
                    this.downloader.Unlisted(this.Symbol);
                }

                this.State.End = DateTimeOffset.Now;
                Database.WriteDays(this.Symbol, candles);
                return candles.Length;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                this.State.Exception = e;
                this.State.End = DateTimeOffset.Now;
                return 0;
            }
        }
    }
}
