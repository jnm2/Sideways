﻿namespace Sideways.AlphaVantage
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Sideways;

    public sealed class Downloader : IDisposable, INotifyPropertyChanged
    {
        private readonly Settings settings;
        private AlphaVantageClient? client;

        private ImmutableList<Download> downloads = ImmutableList<Download>.Empty;
        private ImmutableSortedSet<SymbolDownloads> symbolDownloads;
        private DownloadState symbolDownloadState = new();
        private bool disposed;

        public Downloader(Settings settings)
        {
            this.settings = settings;
            this.symbolDownloads = ImmutableSortedSet.Create<SymbolDownloads>(new SymbolDownloadComparer(settings));
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.RefreshSymbolsCommand = new RelayCommand(_ => this.RefreshSymbolDownloadsAsync());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            this.DownloadAllSymbolsCommand = new RelayCommand(_ => DownloadAll(), _ => !this.symbolDownloads.IsEmpty);

            async void DownloadAll()
            {
                this.SymbolDownloadState.Start = DateTimeOffset.Now;
                while (this.symbolDownloads.FirstOrDefault(x => x is { State: { Status: DownloadStatus.Waiting } } && x.DownloadCommand.CanExecute(null)) is { } symbolDownload)
                {
                    await symbolDownload.DownloadAsync().ConfigureAwait(false);
                    if (symbolDownload.State is { Exception: { Message: { } message } } &&
                        message.Contains("Our standard API call frequency is 5 calls per minute and 500 calls per day.", StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                this.symbolDownloadState.Exception = this.symbolDownloads.FirstOrDefault(x => x.State.Exception is { })?.State.Exception;
                this.SymbolDownloadState.End = DateTimeOffset.Now;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<string>? NewSymbol;

        public event EventHandler<string>? NewDays;

        public event EventHandler<string>? NewMinutes;

        public event EventHandler<string>? NewEarnings;

        public ICommand RefreshSymbolsCommand { get; }

        public ICommand DownloadAllSymbolsCommand { get; }

        public AlphaVantageClient Client
        {
            get
            {
                if (this.client is { })
                {
                    return this.client;
                }

                if (this.settings is { AlphaVantage: { ClientSettings: { ApiKey: { } apiKey, MaxCallsPerMinute: var maxCallsPerMinute } } })
                {
                    return this.client ??= new(new HttpClientHandler(), apiKey, maxCallsPerMinute);
                }

                throw new InvalidOperationException("Missing AlphaVantage settings. Configure it first and try again.");
            }
        }

        public ImmutableList<Download> Downloads
        {
            get => this.downloads;
            private set
            {
                if (ReferenceEquals(value, this.downloads))
                {
                    return;
                }

                this.downloads = value;
                this.OnPropertyChanged();
            }
        }

        public ImmutableSortedSet<SymbolDownloads> SymbolDownloads
        {
            get => this.symbolDownloads;
            private set
            {
                if (ReferenceEquals(value, this.symbolDownloads))
                {
                    return;
                }

                this.symbolDownloads = value;
                this.OnPropertyChanged();
            }
        }

        public DownloadState SymbolDownloadState
        {
            get => this.symbolDownloadState;
            private set
            {
                if (value == this.symbolDownloadState)
                {
                    return;
                }

                this.symbolDownloadState = value;
                this.OnPropertyChanged();
            }
        }

        public async Task RefreshSymbolDownloadsAsync()
        {
            this.SymbolDownloads = this.symbolDownloads.Clear();
            this.SymbolDownloadState = new DownloadState();
            this.SymbolDownloads = this.symbolDownloads.Union(await Task.Run(Create).ConfigureAwait(false));

            IEnumerable<SymbolDownloads> Create()
            {
                var symbols = Database.ReadSymbols();
                var dayRanges = Database.DayRanges(symbols);
                var minuteRanges = Database.MinuteRanges(symbols);

                FixAlphaVantageSettings();

                foreach (var (symbol, dayRange) in dayRanges)
                {
                    if (AlphaVantage.SymbolDownloads.TryCreate(symbol, dayRange, minuteRanges.GetValueOrDefault(symbol), this, this.settings.AlphaVantage) is { } symbolDownloads)
                    {
                        yield return symbolDownloads;
                    }
                }

                void FixAlphaVantageSettings()
                {
                    foreach (var (symbol, range) in minuteRanges)
                    {
                        // Migrate settings in case symbol was marked as missing minutes due to empty old slice.
                        if (range != default &&
                            this.settings.AlphaVantage.SymbolsWithMissingMinutes.Contains(symbol))
                        {
                            this.settings.AlphaVantage.HasMinutes(symbol);
                            this.settings.Save();
                        }
                    }
                }
            }
        }

        public async Task<DaysAndSplits> DaysAndSplitsAsync(string symbol)
        {
            var daysDownload = new SymbolDownloads(symbol, default, DaysDownload.Create(symbol, default, this), default, ImmutableArray<MinutesDownload>.Empty, null);
            //// Adding so that the button shows status.
            this.SymbolDownloads = this.symbolDownloads.Add(daysDownload);
            await daysDownload.DownloadAsync().ConfigureAwait(false);
            this.NewSymbol?.Invoke(this, symbol);
            if (AlphaVantage.SymbolDownloads.TryCreate(symbol, Database.DayRange(symbol), default, this, this.settings.AlphaVantage) is { } symbolDownload)
            {
                this.SymbolDownloads = this.symbolDownloads.Remove(daysDownload);
                this.SymbolDownloads = this.symbolDownloads.Add(symbolDownload);
            }

            return new DaysAndSplits(
                Database.ReadDays(symbol),
                Database.ReadSplits(symbol));
        }

        public void Add(Download download)
        {
            this.Downloads = this.Downloads.Add(download);
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.client?.Dispose();
        }

        public void Unlisted(string symbol)
        {
            this.settings.AlphaVantage.Unlisted(symbol);
            this.settings.Save();
        }

        public void MissingMinutes(string symbol)
        {
            this.settings.AlphaVantage.MissingMinutes(symbol);
            this.settings.Save();
        }

        public void FirstMinute(string symbol, DateTimeOffset first)
        {
            this.settings.AlphaVantage.FirstMinute(symbol, first);
            this.settings.Save();
        }

        public void NotifyDownloadedDays(string symbol) => this.NewDays?.Invoke(this, symbol);

        public void NotifyDownloadedMinutes(string symbol) => this.NewMinutes?.Invoke(this, symbol);

        public void NotifyDownloadedEarnings(string symbol) => this.NewEarnings?.Invoke(this, symbol);

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private sealed class SymbolDownloadComparer : IComparer<SymbolDownloads>
        {
            private readonly Settings settings;

            internal SymbolDownloadComparer(Settings settings)
            {
                this.settings = settings;
            }

            public int Compare(SymbolDownloads? x, SymbolDownloads? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (y is null)
                {
                    return 1;
                }

                if (x is null)
                {
                    return -1;
                }

                var result = Comparer<int>.Default.Compare(ExcludingMinutes(x), ExcludingMinutes(y));
                if (result != 0)
                {
                    return result;
                }

                result = Comparer<int>.Default.Compare(x.MinutesDownloads.Length, y.MinutesDownloads.Length);
                if (result != 0)
                {
                    return -1 * result;
                }

                result = Comparer<int>.Default.Compare(DaysAndMinutesInSync(x), DaysAndMinutesInSync(y));
                if (result != 0)
                {
                    return result;
                }

                result = Comparer<DateTimeOffset>.Default.Compare(x.ExistingDays.Max, y.ExistingDays.Max);
                if (result != 0)
                {
                    return result;
                }

                return string.Compare(x.Symbol, y.Symbol, StringComparison.OrdinalIgnoreCase);

                int ExcludingMinutes(SymbolDownloads candidate)
                {
                    return this.settings.AlphaVantage.SymbolsWithMissingMinutes.Contains(candidate.Symbol) ? 1 : 0;
                }

                static int DaysAndMinutesInSync(SymbolDownloads x)
                {
                    return TradingDay.From(x.ExistingDays.Max) != TradingDay.From(x.ExistingMinutes.Max) ? 0 : 1;
                }
            }
        }
    }
}
