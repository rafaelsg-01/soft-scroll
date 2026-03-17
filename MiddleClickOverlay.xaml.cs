using System;
using System.Windows;
using System.Windows.Media;

namespace SoftScroll;

public partial class MiddleClickOverlay : Window
{
    // Vibrant blue accent for active direction
    private static readonly SolidColorBrush ActiveBrush = new(System.Windows.Media.Color.FromArgb(0xFF, 0x3B, 0x82, 0xF6));
    // Semi-transparent white for inactive state
    private static readonly SolidColorBrush InactiveBrush = new(System.Windows.Media.Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF));

    public MiddleClickOverlay()
    {
        InitializeComponent();
    }

    public void ShowAt(int screenX, int screenY)
    {
        Left = screenX - Width / 2;
        Top = screenY - Height / 2;
        Show();
    }

    public void UpdateDirection(double nx, double ny)
    {
        if (!IsVisible) return;

        Dispatcher.InvokeAsync(() =>
        {
            ArrowUp.Fill = ny < -0.15 ? ActiveBrush : InactiveBrush;
            ArrowDown.Fill = ny > 0.15 ? ActiveBrush : InactiveBrush;
            ArrowLeft.Fill = nx < -0.15 ? ActiveBrush : InactiveBrush;
            ArrowRight.Fill = nx > 0.15 ? ActiveBrush : InactiveBrush;
        });
    }

    public void HideOverlay()
    {
        Dispatcher.InvokeAsync(Hide);
    }
}
