using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Pen = System.Windows.Media.Pen;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;

namespace SoftScroll;

public class EasingCurveCanvas : Canvas
{
    private Pen? _gridPen;
    private Pen? _curvePen;
    private Pen? _axisPen;
    private Typeface _typeface = new Typeface("Segoe UI");

    public EasingCurveCanvas()
    {
        Height = 120;
        ClipToBounds = true;

        if (DesignerProperties.GetIsInDesignMode(this)) return;

        DataContextChanged += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is "AnimationTimeMs" or "EasingMode" or "TailToHeadRatio" or "AnimationEasing")
                        InvalidateVisual();
                };
            }
            InvalidateVisual();
        };
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var w = ActualWidth;
        var h = ActualHeight;
        if (w < 10 || h < 10) return;

        var vm = DataContext as SettingsViewModel;

        // Resolve theme colors
        var textBrush = TryFindResource("TextSecondaryBrush") as SolidColorBrush ?? Brushes.Gray;
        var accentBrush = TryFindResource("AccentBrush") as SolidColorBrush ?? Brushes.DodgerBlue;
        var borderBrush = TryFindResource("SurfaceBorderBrush") as SolidColorBrush ?? Brushes.DimGray;

        _gridPen = new Pen(borderBrush, 0.5) { DashStyle = DashStyles.Dot };
        _curvePen = new Pen(accentBrush, 2.5);
        _axisPen = new Pen(textBrush, 0.8);

        var margin = new Thickness(32, 8, 8, 20);
        var plotW = w - margin.Left - margin.Right;
        var plotH = h - margin.Top - margin.Bottom;

        // Draw grid
        for (int i = 1; i < 4; i++)
        {
            var gy = margin.Top + plotH * (i / 4.0);
            dc.DrawLine(_gridPen, new Point(margin.Left, gy), new Point(margin.Left + plotW, gy));
            var gx = margin.Left + plotW * (i / 4.0);
            dc.DrawLine(_gridPen, new Point(gx, margin.Top), new Point(gx, margin.Top + plotH));
        }

        // Draw axes
        dc.DrawLine(_axisPen, new Point(margin.Left, margin.Top), new Point(margin.Left, margin.Top + plotH));
        dc.DrawLine(_axisPen, new Point(margin.Left, margin.Top + plotH), new Point(margin.Left + plotW, margin.Top + plotH));

        // Axis labels
        var fmt0 = MakeText("0", textBrush, 9);
        var fmt1 = MakeText("1", textBrush, 9);
        dc.DrawText(fmt0, new Point(margin.Left - fmt0.Width - 3, margin.Top + plotH - fmt0.Height / 2));
        dc.DrawText(fmt1, new Point(margin.Left - fmt1.Width - 3, margin.Top - fmt1.Height / 2));

        var fmtT = MakeText("t", textBrush, 9);
        dc.DrawText(fmtT, new Point(margin.Left + plotW + 2, margin.Top + plotH - fmtT.Height / 2));

        // Draw easing curve
        if (vm == null) return;

        var duration = Math.Max(1.0, vm.AnimationTimeMs);
        var easing = vm.EasingMode;
        var tailHead = vm.TailToHeadRatio;
        var easingEnabled = vm.AnimationEasing;

        const int samples = 80;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (int i = 0; i <= samples; i++)
            {
                var t = i / (double)samples;
                var dtMs = t * duration;
                var frac = SmoothScrollEngine.ComputeEasingFraction(dtMs, duration, easing, tailHead, easingEnabled);
                frac = Math.Clamp(frac, 0, 1);

                var px = margin.Left + t * plotW;
                var py = margin.Top + plotH - frac * plotH;

                if (i == 0)
                    ctx.BeginFigure(new Point(px, py), false, false);
                else
                    ctx.LineTo(new Point(px, py), true, true);
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, _curvePen, geometry);

        // Label: easing mode name
        var label = MakeText(easingEnabled ? easing.ToString() : "Linear", accentBrush, 10);
        dc.DrawText(label, new Point(margin.Left + plotW - label.Width, margin.Top - 2));
    }

    private FormattedText MakeText(string text, Brush brush, double size)
    {
        return new FormattedText(
            text, System.Globalization.CultureInfo.InvariantCulture,
            System.Windows.FlowDirection.LeftToRight, _typeface, size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }
}
