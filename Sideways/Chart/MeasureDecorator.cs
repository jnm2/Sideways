﻿namespace Sideways
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Media;
    using System.Windows.Shapes;

    [ContentProperty(nameof(Child))]
    public class MeasureDecorator : CandleSeries
    {
        public static readonly DependencyProperty PriceRangeProperty = Chart.PriceRangeProperty.AddOwner(
            typeof(MeasureDecorator),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty PriceScaleProperty = Chart.PriceScaleProperty.AddOwner(
            typeof(MeasureDecorator),
            new FrameworkPropertyMetadata(
                Scale.Logarithmic,
                FrameworkPropertyMetadataOptions.AffectsArrange));

        private static readonly DependencyPropertyKey MeasurementPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Measurement),
            typeof(Measurement),
            typeof(MeasureDecorator),
            new FrameworkPropertyMetadata(
                default(Measurement),
                FrameworkPropertyMetadataOptions.AffectsArrange,
                OnMeasurementChanged));

        public static readonly DependencyProperty MeasurementProperty = MeasurementPropertyKey.DependencyProperty;

        private readonly Rectangle rectangle = new() { Visibility = Visibility.Collapsed, Fill = Brushes.MeasureBackground, IsHitTestVisible = false };
        private readonly ContentPresenter infoPresenter = new() { Visibility = Visibility.Collapsed, IsHitTestVisible = false };
        private CandleSticks? child;

        static MeasureDecorator()
        {
            ClipToBoundsProperty.OverrideMetadata(typeof(MeasureDecorator), new PropertyMetadata(true));
        }

        public MeasureDecorator()
        {
            this.AddVisualChild(this.rectangle);
            this.AddVisualChild(this.infoPresenter);
        }

        public FloatRange? PriceRange
        {
            get => (FloatRange?)this.GetValue(PriceRangeProperty);
            set => this.SetValue(PriceRangeProperty, value);
        }

        public Scale PriceScale
        {
            get => (Scale)this.GetValue(PriceScaleProperty);
            set => this.SetValue(PriceScaleProperty, value);
        }

        public Measurement? Measurement
        {
            get => (Measurement?)this.GetValue(MeasurementProperty);
            private set => this.SetValue(MeasurementPropertyKey, value);
        }

        [DefaultValue(null)]
        public virtual CandleSticks? Child
        {
            get => this.child;
            set
            {
                if (this.child != value)
                {
                    this.RemoveVisualChild(this.child);
                    this.RemoveLogicalChild(this.child);
                    this.child = value;
                    this.AddLogicalChild(value);
                    this.AddVisualChild(value);
                    this.InvalidateMeasure();
                }
            }
        }

        protected override IEnumerator LogicalChildren => this.child switch
        {
            { } child => new SingleChildEnumerator(child),
            _ => EmptyEnumerator.Instance,
        };

        protected override int VisualChildrenCount => this.child is null ? 2 : 3;

        protected override Visual GetVisualChild(int index) => index switch
        {
            0 => this.rectangle,
            1 => (Visual?)this.child ?? this.infoPresenter,
            2 when this.child is not null => this.infoPresenter,
            _ => throw new ArgumentOutOfRangeException(nameof(index), "Check VisualChildrenCount first"),
        };

        protected override Size MeasureOverride(Size availableSize)
        {
            this.rectangle.Measure(availableSize);
            this.child?.Measure(availableSize);
            this.infoPresenter.Measure(availableSize);
            return this.child?.DesiredSize ?? default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.PriceRange is { } priceRange &&
                this.Candles is { } candles &&
                this.CandleWidth is var candleWidth &&
                this.CandleInterval is var candleInterval &&
                this.Measurement is { To: { } to } measurement)
            {
                var valueRange = new ValueRange(priceRange, this.PriceScale);
                this.rectangle.Arrange(new Rect(
                    Point(measurement.From),
                    Point(to)));
                this.child?.Arrange(new Rect(finalSize));
                this.infoPresenter.Arrange(InfoRectangle());

                Point Point(TimeAndPrice timeAndPrice)
                {
                    return new(
                        CandlePosition.ClampedX(timeAndPrice.Time, candles, finalSize.Width, candleWidth, candleInterval),
                        valueRange.Y(timeAndPrice.Price, finalSize.Height));
                }

                Rect InfoRectangle()
                {
                    var position = Point(to);
                    var desiredSize = this.infoPresenter.DesiredSize;
                    return new Rect(new Point(position.X + OffsetX(), Y()), desiredSize);

                    double OffsetX()
                    {
                        return measurement switch
                        {
                            { From: { Time: var s }, To: { Time: var e } }
                                when s <= e
                                => -desiredSize.Width,
                            _ => 0,
                        };
                    }

                    double Y()
                    {
                        return this.Measurement switch
                        {
                            { From: { Price: var s }, To: { Price: var e } }
                                when s <= e
                                => Math.Max(0, position.Y - desiredSize.Height),
                            _ => position.Y,
                        };
                    }
                }
            }
            else
            {
                this.child?.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.PriceRange is { } priceRange &&
                CandlePosition.RightToLeft(this.RenderSize, this.CandleWidth, new ValueRange(priceRange, this.PriceScale)).TimeAndPrice(e.GetPosition(this), this.Candles) is { } timeAndPrice)
            {
                this.Measurement = Measurement.Start(timeAndPrice);
            }
            else
            {
                this.Measurement = null;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.PriceRange is { } priceRange &&
                e.LeftButton == MouseButtonState.Pressed &&
                CandlePosition.RightToLeft(this.RenderSize, this.CandleWidth, new ValueRange(priceRange, this.PriceScale)).TimeAndPrice(e.GetPosition(this), this.Candles) is { } timeAndPrice)
            {
                if (this.Measurement is { } measurement)
                {
                    var candles = this.Candles;
                    var fromIndex = IndexOf(measurement.From.Time);

                    this.Measurement = measurement.WithEnd(
                        timeAndPrice,
                        Math.Abs(fromIndex - IndexOf(timeAndPrice.Time)),
                        fromIndex >= 20 ? candles.Slice(fromIndex, 20).Adr() : null,
                        fromIndex >= 21 ? candles.Slice(fromIndex, 21).Atr() : null);

                    int IndexOf(DateTimeOffset time)
                    {
                        for (var i = 0; i < candles.Count; i++)
                        {
                            if (candles[i].Time == time)
                            {
                                return i;
                            }
                        }

                        return -1;
                    }
                }
                else
                {
                    this.Measurement = Measurement.Start(timeAndPrice);
                }
            }
        }

        private static void OnMeasurementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var decorator = (MeasureDecorator)d;
#pragma warning disable WPF0041 // Set mutable dependency properties using SetCurrentValue.
            if (e.NewValue is Measurement { To: not null } measurement)
            {
                decorator.rectangle.Visibility = Visibility.Visible;
                decorator.infoPresenter.Content = measurement;
                decorator.infoPresenter.Visibility = Visibility.Visible;
            }
            else
            {
                decorator.rectangle.Visibility = Visibility.Collapsed;
                decorator.infoPresenter.Visibility = Visibility.Collapsed;
                decorator.infoPresenter.Content = null;
            }
#pragma warning restore WPF0041 // Set mutable dependency properties using SetCurrentValue.
        }
    }
}
