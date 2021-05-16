﻿namespace Sideways
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    public class CrossHair : FrameworkElement
    {
        /// <summary>Identifies the <see cref="Brush"/> dependency property.</summary>
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush),
            typeof(SolidColorBrush),
            typeof(CrossHair),
            new FrameworkPropertyMetadata(
                default(SolidColorBrush),
                FrameworkPropertyMetadataOptions.AffectsRender,
#pragma warning disable WPF0022 // Cast value to correct type.
                (d, e) => ((CrossHair)d).pen = CreatePen((SolidColorBrush?)e.NewValue)));
#pragma warning restore WPF0022 // Cast value to correct type.

        /// <summary>Identifies the <see cref="Range"/> dependency property.</summary>
        public static readonly DependencyProperty RangeProperty = Chart.RangeProperty.AddOwner(
            typeof(CrossHair),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="Range"/> dependency property.</summary>
        public static readonly DependencyProperty CandlesProperty = Chart.CandlesProperty.AddOwner(typeof(CrossHair));

        /// <summary>Identifies the <see cref="CandleInterval"/> dependency property.</summary>
        public static readonly DependencyProperty CandleIntervalProperty = Chart.CandleIntervalProperty.AddOwner(
            typeof(CrossHair),
            new FrameworkPropertyMetadata(
                CandleInterval.None,
                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="CandleWidth"/> dependency property.</summary>
        public static readonly DependencyProperty CandleWidthProperty = Chart.CandleWidthProperty.AddOwner(
            typeof(CrossHair),
            new FrameworkPropertyMetadata(
                5,
                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Identifies the <see cref="Position"/> dependency property.</summary>
        public static readonly DependencyProperty PositionProperty = DependencyProperty.RegisterAttached(
            nameof(Position),
            typeof(CrossHairPosition?),
            typeof(CrossHair),
            new FrameworkPropertyMetadata(
                default(CrossHairPosition?),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private readonly DrawingVisual drawing;

        private Pen? pen;

        static CrossHair()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CrossHair), new FrameworkPropertyMetadata(typeof(CrossHair)));
            ClipToBoundsProperty.OverrideMetadata(typeof(CrossHair), new PropertyMetadata(true));
        }

        public CrossHair()
        {
            this.drawing = new DrawingVisual();
            this.AddVisualChild(this.drawing);
        }

#pragma warning disable WPF0012 // CLR property type should match registered type.
        public SolidColorBrush? Brush
#pragma warning restore WPF0012 // CLR property type should match registered type.
        {
            get => (SolidColorBrush?)this.GetValue(BrushProperty);
            set => this.SetValue(BrushProperty, value);
        }

#pragma warning disable WPF0012 // CLR property type should match registered type.
        public IReadOnlyList<Candle> Candles
#pragma warning restore WPF0012 // CLR property type should match registered type.
        {
            get => (IReadOnlyList<Candle>)this.GetValue(CandlesProperty);
            set => this.SetValue(CandlesProperty, value);
        }

        public int CandleWidth
        {
            get => (int)this.GetValue(CandleWidthProperty);
            set => this.SetValue(CandleWidthProperty, value);
        }

        public CandleInterval CandleInterval
        {
            get => (CandleInterval)this.GetValue(CandleIntervalProperty);
            set => this.SetValue(CandleIntervalProperty, value);
        }

        public FloatRange? Range
        {
            get => (FloatRange?)this.GetValue(RangeProperty);
            set => this.SetValue(RangeProperty, value);
        }

        public CrossHairPosition? Position
        {
            get => (CrossHairPosition?)this.GetValue(PositionProperty);
            set => this.SetValue(PositionProperty, value);
        }

        public static CrossHairPosition? GetPosition(UIElement e) => (CrossHairPosition?)e.GetValue(PositionProperty);

        public static void SetPosition(UIElement e, CrossHairPosition? position) => e.SetValue(PositionProperty, position);

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => index == 0
            ? this.drawing
            : throw new ArgumentOutOfRangeException(nameof(index));

        protected override void OnRender(DrawingContext drawingContext)
        {
            var size = this.RenderSize;
            using var context = this.drawing.RenderOpen();
            context.DrawRectangle(
                System.Windows.Media.Brushes.Transparent,
                null,
                new Rect(this.RenderSize));
            if (this.pen is { } &&
                this.Range is { } priceRange &&
                this.Position is { Time: var time, Price: var price })
            {
                if (this.IsMouseOver)
                {
                    var p = Mouse.GetPosition(this);
                    context.DrawLine(this.pen, new Point(0, p.Y), new Point(size.Width, p.Y));
                    context.DrawLine(this.pen, new Point(p.X, 0), new Point(p.X, size.Height));
                }
                else
                {
                    var y = priceRange.Y(price, size.Height);
                    context.DrawLine(this.pen, new Point(0, y), new Point(size.Width, y));
                    //context.DrawLine(this.pen, new Point(p.X, 0), new Point(p.X, size.Height));
                }
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            var size = this.RenderSize;
            var pos = e.GetPosition(this);
            var i = (int)Math.Round((size.Width - pos.X) / this.CandleWidth);
            if (this.Candles.Count > i &&
                i >= 0 &&
                this.Range is { } range)
            {
                this.SetCurrentValue(PositionProperty, new CrossHairPosition(this.Candles[i].Time, range.ValueFromY(pos.Y, size.Height), this.CandleInterval));
            }
            else
            {
                this.SetCurrentValue(PositionProperty, null);
            }

            base.OnPreviewMouseMove(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            this.SetCurrentValue(PositionProperty, null);
            base.OnMouseLeave(e);
        }

        private static Pen? CreatePen(SolidColorBrush? brush)
        {
            if (brush is { })
            {
                var temp = new Pen(brush, 0.5);
                temp.Freeze();
                return temp;
            }

            return null;
        }
    }
}