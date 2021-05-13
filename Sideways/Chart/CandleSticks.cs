﻿namespace Sideways
{
    using System;
    using System.Collections.Immutable;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    public class CandleSticks : FrameworkElement
    {
        /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(Candles),
            typeof(CandleSticks),
            new FrameworkPropertyMetadata(
                default(Candles),
                FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly DependencyPropertyKey VisibleCandlesPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(VisibleCandles),
            typeof(ImmutableArray<Candle>?),
            typeof(CandleSticks),
            new PropertyMetadata(default(ImmutableArray<Candle>?)));

        /// <summary>Identifies the <see cref="VisibleCandles"/> dependency property.</summary>
        public static readonly DependencyProperty VisibleCandlesProperty = VisibleCandlesPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PriceRangePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PriceRange),
            typeof(FloatRange?),
            typeof(CandleSticks),
            new PropertyMetadata(default(FloatRange?)));

        /// <summary>Identifies the <see cref="PriceRange"/> dependency property.</summary>
        public static readonly DependencyProperty PriceRangeProperty = PriceRangePropertyKey.DependencyProperty;

        /// <summary>Identifies the <see cref="Time"/> dependency property.</summary>
        public static readonly DependencyProperty TimeProperty = Chart.TimeProperty.AddOwner(
            typeof(CandleSticks),
            new FrameworkPropertyMetadata(
                DateTimeOffset.Now,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>Identifies the <see cref="CandleInterval"/> dependency property.</summary>
        public static readonly DependencyProperty CandleIntervalProperty = DependencyProperty.Register(
            nameof(CandleInterval),
            typeof(CandleInterval),
            typeof(CandleSticks),
            new PropertyMetadata(CandleInterval.None));

        /// <summary>Identifies the <see cref="CandleWidth"/> dependency property.</summary>
        public static readonly DependencyProperty CandleWidthProperty = DependencyProperty.Register(
            nameof(CandleWidth),
            typeof(int),
            typeof(CandleSticks),
            new FrameworkPropertyMetadata(
                5,
                FrameworkPropertyMetadataOptions.AffectsRender));

        private readonly DrawingVisual drawing;

        static CandleSticks()
        {
            RenderOptions.EdgeModeProperty.OverrideMetadata(typeof(CandleSticks), new UIPropertyMetadata(EdgeMode.Aliased));
        }

        public CandleSticks()
        {
            this.drawing = new DrawingVisual();
            this.AddVisualChild(this.drawing);
        }

        public Candles? ItemsSource
        {
            get => (Candles?)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

        public ImmutableArray<Candle>? VisibleCandles
        {
            get => (ImmutableArray<Candle>?)this.GetValue(VisibleCandlesProperty);
            private set => this.SetValue(VisibleCandlesPropertyKey, value);
        }

        public FloatRange? PriceRange
        {
            get => (FloatRange?)this.GetValue(PriceRangeProperty);
            private set => this.SetValue(PriceRangePropertyKey, value);
        }

        public DateTimeOffset Time
        {
            get => (DateTimeOffset)this.GetValue(TimeProperty);
            set => this.SetValue(TimeProperty, value);
        }

        public CandleInterval CandleInterval
        {
            get => (CandleInterval)this.GetValue(CandleIntervalProperty);
            set => this.SetValue(CandleIntervalProperty, value);
        }

        public int CandleWidth
        {
            get => (int)this.GetValue(CandleWidthProperty);
            set => this.SetValue(CandleWidthProperty, value);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => index == 0
            ? this.drawing
            : throw new ArgumentOutOfRangeException(nameof(index));

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (this.ItemsSource is { } candles)
            {
                this.SetCurrentValue(TimeProperty, candles.Skip(this.Time, this.CandleInterval, Math.Sign(e.Delta)));
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = this.RenderSize;
            drawingContext.DrawRectangle(
                System.Windows.Media.Brushes.Transparent,
                null,
                new Rect(this.RenderSize));

            var candleWidth = this.CandleWidth;
            if (this.ItemsSource is { } itemsSource)
            {
                var builder = ImmutableArray.CreateBuilder<Candle>((int)Math.Ceiling(size.Width / candleWidth));
                var min = float.MaxValue;
                var max = float.MinValue;
                var x = 0.0;
                foreach (var candle in itemsSource.Get(this.Time, this.CandleInterval))
                {
                    if (x > size.Width)
                    {
                        break;
                    }

                    min = Math.Min(min, candle.Low);
                    max = Math.Max(max, candle.High);
                    builder.Add(candle);
                    x += candleWidth;
                }

                var priceRange = new FloatRange(min, max);
                this.PriceRange = priceRange;
                this.VisibleCandles = builder.ToImmutable();
                using var context = this.drawing.RenderOpen();
                var halfWidth = candleWidth / 2;
                x = size.Width - halfWidth;

                foreach (var candle in builder)
                {
                    var brush = Brushes.Get(candle);
                    context.DrawRectangle(
                        brush,
                        null,
                        Rect(
                            new Point(x - halfWidth - 1, Y(candle.Low)),
                            new Point(x - halfWidth, Y(candle.High))));
                    var yOpen = (int)Y(candle.Open);
                    var yClose = (int)Y(candle.Close);
                    if (yOpen == yClose)
                    {
                        yClose += 1;
                    }

                    context.DrawRectangle(
                        brush,
                        null,
                        Rect(
                            new Point(x - 4, yOpen),
                            new Point(x - 1, yClose)));

                    x -= candleWidth;
                    if (x < 0)
                    {
                        break;
                    }

                    double Y(float price) => size.Height * (1 - priceRange.Interpolate(price));

                    static Rect Rect(Point p1, Point p2)
                    {
                        return new Rect(Round(p1), Round(p2));

                        static Point Round(Point p) => new(Math.Round(p.X), Math.Round(p.Y));
                    }
                }
            }
            else
            {
                this.VisibleCandles = null;
                this.PriceRange = null;

                // clear
                using var context = this.drawing.RenderOpen();
            }
        }
    }
}
