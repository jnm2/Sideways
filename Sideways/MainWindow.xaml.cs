﻿namespace Sideways
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;

    public partial class MainWindow : Window
    {
        private DispatcherTimer? timer;

        public MainWindow()
        {
            this.InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                var mainViewModel = new MainViewModel();
                mainViewModel.CurrentSymbol = mainViewModel.Symbols.FirstOrDefault();
                this.DataContext = mainViewModel;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    Skip(CandleInterval.Day, -1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    Skip(CandleInterval.Day, 1);
                    e.Handled = true;
                    break;
                case Key.Space
                    when this.timer is null:
                    this.timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(20),
                    };
                    this.timer.Tick += (_, _) => Skip(CandleInterval.Hour, 1);
                    this.timer.Start();
                    break;
                case Key.Space:
                    this.timer.Stop();
                    this.timer = null;
                    break;
            }

            base.OnPreviewKeyDown(e);

            void Skip(CandleInterval interval, int count)
            {
                if (this.DataContext is MainViewModel { CurrentSymbol: { Candles: { } candles } } vm)
                {
                    vm.Time = candles.Skip(vm.Time, interval, count);
                }
            }
        }
    }
}
