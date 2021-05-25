﻿namespace Sideways
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public class MovingAverage : CandleSeries
    {
        /// <summary>Identifies the <see cref="PriceRange"/> dependency property.</summary>
        public static readonly DependencyProperty PriceRangeProperty = Chart.PriceRangeProperty.AddOwner(
            typeof(MovingAverage),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="Brush"/> dependency property.</summary>
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush),
            typeof(SolidColorBrush),
            typeof(MovingAverage),
            new FrameworkPropertyMetadata(
                default(SolidColorBrush),
                FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((MovingAverage)d).pen = CreatePen((SolidColorBrush?)e.NewValue)));

        /// <summary>Identifies the <see cref="Period"/> dependency property.</summary>
        public static readonly DependencyProperty PeriodProperty = DependencyProperty.Register(
            nameof(Period),
            typeof(int),
            typeof(MovingAverage),
            new PropertyMetadata(default(int)));

        private readonly DrawingVisual drawing;

        private Pen? pen;

        public MovingAverage()
        {
            this.drawing = new DrawingVisual();
            this.AddVisualChild(this.drawing);
        }

        public FloatRange? PriceRange
        {
            get => (FloatRange?)this.GetValue(PriceRangeProperty);
            set => this.SetValue(PriceRangeProperty, value);
        }

        public SolidColorBrush? Brush
        {
            get => (SolidColorBrush?)this.GetValue(BrushProperty);
            set => this.SetValue(BrushProperty, value);
        }

        public int Period
        {
            get => (int)this.GetValue(PeriodProperty);
            set => this.SetValue(PeriodProperty, value);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => index == 0
            ? this.drawing
            : throw new ArgumentOutOfRangeException(nameof(index));

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = this.RenderSize;
            using var context = this.drawing.RenderOpen();
            if (this.pen is { } &&
                this.PriceRange is { } priceRange)
            {
                Point? previous = null;
                var position = CandlePosition.Create(size.Width, this.CandleWidth);
                foreach (var a in this.Candles.MovingAverage(this.Period, c => c.Close))
                {
                    var p2 = new Point(position.CenterLeft, Y(a));
                    if (previous is { } p1)
                    {
                        context.DrawLine(
                            this.pen,
                            p1,
                            p2);
                    }

                    previous = p2;
                    position = position.Shift();
                    if (position.Right < 0)
                    {
                        break;
                    }

                    double Y(float price) => priceRange.Y(price, size.Height);
                }
            }
        }

        private static Pen? CreatePen(SolidColorBrush? brush)
        {
            if (brush is { })
            {
                var temp = new Pen(brush, 1);
                temp.Freeze();
                return temp;
            }

            return null;
        }
    }
}
