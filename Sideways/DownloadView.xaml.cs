﻿namespace Sideways
{
    using System;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;

    using Sideways.AlphaVantage;

    public partial class DownloadView : UserControl
    {
        public DownloadView()
        {
            this.InitializeComponent();
        }

        private void OnCanRefresh(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.DataContext is Downloader &&
                e.Parameter is TopUp topUp)
            {
                e.CanExecute = topUp switch
                {
                    { DaysDownload: { Started: { } } } => false,
                    { MinutesDownloads: { Length: > 0 } minutesDownloads } => minutesDownloads.All(x => x.Started is null),
                    _ => true,
                };

                e.Handled = true;
            }
        }

        private async void OnRefresh(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.DataContext is Downloader downloader &&
                e.Parameter is TopUp topUp)
            {
                if (topUp.DaysDownload is { Started: null } daysDownload)
                {
                    await downloader.ExecuteAsync(daysDownload).ConfigureAwait(false);
                }

                if (topUp.MinutesDownloads is { Length: > 0 } minutesDownloads)
                {
                    foreach (var minutesDownload in minutesDownloads)
                    {
                        if (minutesDownload.Started is null)
                        {
                            if (await downloader.ExecuteAsync(minutesDownload).ConfigureAwait(false) == 0)
                            {
                                return;
                            }
                        }
                    }
                }

                e.Handled = true;
            }
        }
    }
}
