﻿namespace Sideways.AlphaVantage
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public sealed class DataSource : IDisposable
    {
        private readonly AlphaVantageClient client;
        private bool disposed;

        public DataSource(HttpMessageHandler messageHandler, string apiKey)
        {
            this.client = new AlphaVantageClient(messageHandler, apiKey);
        }

        public async Task<ImmutableArray<AdjustedCandle>> DaysAsync(string symbol)
        {
            var candles = await Database.ReadDaysAsync(symbol).ConfigureAwait(false);
            if (candles.LastOrDefault().Time.Date == TradingDay.LastComplete)
            {
                return candles;
            }

            if ((TradingDay.LastComplete - candles.LastOrDefault().Time.Date).Days < 100)
            {
                Database.WriteDays(symbol, await this.client.DailyAdjustedAsync(symbol, OutputSize.Compact).ConfigureAwait(false));
                return await Database.ReadDaysAsync(symbol).ConfigureAwait(false);
            }

            var adjusted = await this.client.DailyAdjustedAsync(symbol, OutputSize.Full).ConfigureAwait(false);
            Database.WriteDays(symbol, adjusted);
            return adjusted;
        }

        public async Task<ImmutableArray<Candle>> MinutesAsync(string symbol)
        {
            var candles = await Database.ReadMinutesAsync(symbol).ConfigureAwait(false);
            if (candles.Length > 0)
            {
                return candles;
            }

            var adjusted = await this.client.IntervalExtendedAsync(symbol, Interval.Minute, Slice.Year1Month1, adjusted: false).ConfigureAwait(false);
            Database.WriteMinutes(symbol, adjusted);
            return await Database.ReadMinutesAsync(symbol).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.client.Dispose();
        }
    }
}
