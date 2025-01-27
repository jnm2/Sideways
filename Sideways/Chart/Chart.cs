﻿namespace Sideways
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Media;

    [ContentProperty(nameof(Children))]
    public class Chart : FrameworkElement
    {
        /// <summary>Identifies the <see cref="Time"/> dependency property.</summary>
        public static readonly DependencyProperty TimeProperty = DependencyProperty.RegisterAttached(
            nameof(Time),
            typeof(DateTimeOffset),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                default(DateTimeOffset),
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.RegisterAttached(
            nameof(ItemsSource),
            typeof(Candles),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                default(Candles),
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>Identifies the <see cref="Candles"/> dependency property.</summary>
        public static readonly DependencyProperty CandlesProperty = DependencyProperty.RegisterAttached(
            nameof(Candles),
            typeof(DescendingCandles),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                default(DescendingCandles),
                FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Identifies the <see cref="CandleInterval"/> dependency property.</summary>
        public static readonly DependencyProperty CandleIntervalProperty = DependencyProperty.RegisterAttached(
            nameof(CandleInterval),
            typeof(CandleInterval),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                CandleInterval.None,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="CandleWidth"/> dependency property.</summary>
        public static readonly DependencyProperty CandleWidthProperty = DependencyProperty.RegisterAttached(
            nameof(CandleWidth),
            typeof(int),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                5,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="PriceRange"/> dependency property.</summary>
        public static readonly DependencyProperty PriceRangeProperty = DependencyProperty.RegisterAttached(
            nameof(PriceRange),
            typeof(FloatRange?),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Identifies the <see cref="PriceRange"/> dependency property.</summary>
        public static readonly DependencyProperty PriceScaleProperty = DependencyProperty.RegisterAttached(
            nameof(PriceScale),
            typeof(Scale),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                Scale.Logarithmic,
                FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Identifies the <see cref="MaxVolume"/> dependency property.</summary>
        public static readonly DependencyProperty MaxVolumeProperty = DependencyProperty.RegisterAttached(
            nameof(MaxVolume),
            typeof(int?),
            typeof(Chart),
            new FrameworkPropertyMetadata(
                default(int?),
                FrameworkPropertyMetadataOptions.Inherits));

        public Chart()
        {
            this.Children = new UIElementCollection(this, this);
            this.Candles = new DescendingCandles();
        }

        public DateTimeOffset Time
        {
            get => (DateTimeOffset)this.GetValue(TimeProperty);
            set => this.SetValue(TimeProperty, value);
        }

        public Candles? ItemsSource
        {
            get => (Candles?)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

#pragma warning disable WPF0012 // CLR property type should match registered type.
        public DescendingCandles Candles
#pragma warning restore WPF0012 // CLR property type should match registered type.
        {
            get => (DescendingCandles)this.GetValue(CandlesProperty);
            set => this.SetValue(CandlesProperty, value);
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

        public FloatRange? PriceRange
        {
            get => (FloatRange?)this.GetValue(PriceRangeProperty);
            protected set => this.SetValue(PriceRangeProperty, value);
        }

        public Scale PriceScale
        {
            get => (Scale)this.GetValue(PriceScaleProperty);
            set => this.SetValue(PriceScaleProperty, value);
        }

        public int? MaxVolume
        {
            get => (int?)this.GetValue(MaxVolumeProperty);
            protected set => this.SetValue(MaxVolumeProperty, value);
        }

        public UIElementCollection Children { get; }

        protected override int VisualChildrenCount => this.Children.Count;

        protected override IEnumerator LogicalChildren => this.Children.GetEnumerator();

        protected override Visual GetVisualChild(int index) => this.Children[index];

        protected override Size MeasureOverride(Size availableSize)
        {
            var rect = Rect.Empty;
            foreach (UIElement child in this.Children)
            {
                child.Measure(availableSize);
                rect.Union(new Rect(child.DesiredSize));
            }

            return rect.Size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var candles = this.Candles;
            candles.Clear();
            if (finalSize is { Width: > 0, Height: > 0 } &&
                this.ItemsSource is { } itemsSource)
            {
                var min = float.MaxValue;
                var max = float.MinValue;
                var maxVolume = 0;
                foreach (var candle in itemsSource.Get(this.Time, this.CandleInterval)
                                                  .Take(candles.VisibleCount + candles.ExtraCount))
                {
                    if (candles.Count <= candles.VisibleCount)
                    {
                        min = Math.Min(min, candle.Low);
                        max = Math.Max(max, candle.High);
                        maxVolume = Math.Max(maxVolume, candle.Volume);
                    }

                    candles.Add(candle);
                }

                if (candles.Count > 0)
                {
                    this.SetCurrentValue(PriceRangeProperty, new FloatRange(min, max));
                    this.SetCurrentValue(MaxVolumeProperty, maxVolume);
                }
                else
                {
                    this.SetCurrentValue(PriceRangeProperty, null);
                    this.SetCurrentValue(MaxVolumeProperty, 0);
                }
            }
            else
            {
                this.SetCurrentValue(PriceRangeProperty, null);
                this.SetCurrentValue(MaxVolumeProperty, 0);
            }

            foreach (UIElement child in this.Children)
            {
                child.Arrange(new Rect(finalSize));
            }

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        ///     Fills in the background based on the Background property.
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(
                Brushes.Transparent,
                null,
                new Rect(this.RenderSize));
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (this.ItemsSource is { } candles &&
               Delta() is var delta &&
               delta != 0)
            {
                this.SetCurrentValue(
                    TimeProperty,
                    candles.Skip(
                        this.Time,
                        this.CandleInterval,
                        delta));
            }

            int Delta()
            {
                if (IsFromTouch(e))
                {
                    if (Math.Abs(e.Delta) < 4)
                    {
                        return Math.Sign(e.Delta);
                    }

                    return e.Delta switch
                    {
                        < 0 => Math.Min(-1, TouchDelta()),
                        > 0 => Math.Max(1, TouchDelta()),
                        _ => 0,
                    };

                    // Pan about the same length horizontally as the swipe
                    int TouchDelta() => e.Delta / this.CandleWidth;
                }

                // We try to calculate a step based on how fast user is spinning the wheel.
                return Scroll.DeltaTime(e) switch
                {
                    <= 0 => 0,
                    > 50 => Math.Sign(e.Delta),
                    var dt => e.Delta switch
                    {
                        < 0 => Math.Min(-1, -120 / dt),
                        > 0 => Math.Max(1, 240 / dt),
                        _ => 0,
                    },
                };

                // Must be better ways for this but may require pinvoke. Good enough for now.
                static bool IsFromTouch(MouseWheelEventArgs e) => e.Delta % Mouse.MouseWheelDeltaForOneLine != 0;
            }
        }

        private static class Scroll
        {
            private static int lastTimeStamp;

            internal static int DeltaTime(MouseWheelEventArgs e)
            {
                var delta = e.Timestamp - lastTimeStamp;
                lastTimeStamp = e.Timestamp;
                return delta;
            }
        }
    }
}
