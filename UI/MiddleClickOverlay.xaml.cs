using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SoftScroll.UI;

public partial class MiddleClickOverlay : Window
{
    // Vibrant blue accent for active direction
    private static readonly SolidColorBrush ActiveBrush = new(System.Windows.Media.Color.FromArgb(0xFF, 0x3B, 0x82, 0xF6));
    // Semi-transparent white for inactive state
    private static readonly SolidColorBrush InactiveBrush = new(System.Windows.Media.Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF));
    // Speed indicator brush (blue with varying opacity)
    private static readonly SolidColorBrush SpeedBrush = new(System.Windows.Media.Color.FromArgb(0x99, 0x64, 0xC4, 0xF5));
    
    private int _currentSpeed;
    private DispatcherTimer? _fadeTimer;

    public MiddleClickOverlay()
    {
        InitializeComponent();
        _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _fadeTimer.Tick += (s, e) =>
        {
            _fadeTimer?.Stop();
            Dispatcher.InvokeAsync(() =>
            {
                SpeedText.Text = "0";
                SpeedText.Opacity = 0.5;
            });
        };
    }

    public void ShowAt(int screenX, int screenY)
    {
        Left = screenX - Width / 2;
        Top = screenY - Height / 2;
        _currentSpeed = 0;
        SpeedText.Text = "0";
        SpeedText.Opacity = 0.5;
        Show();
    }

    public void UpdateDirection(double nx, double ny, double magnitude)
    {
        if (!IsVisible) return;

        Dispatcher.InvokeAsync(() =>
        {
            ArrowUp.Fill = ny < -0.15 ? ActiveBrush : InactiveBrush;
            ArrowDown.Fill = ny > 0.15 ? ActiveBrush : InactiveBrush;
            ArrowLeft.Fill = nx < -0.15 ? ActiveBrush : InactiveBrush;
            ArrowRight.Fill = nx > 0.15 ? ActiveBrush : InactiveBrush;

            // Update speed indicator based on magnitude
            if (magnitude > 0.2)
            {
                _currentSpeed = Math.Min(99, (int)(magnitude * 100));
                SpeedText.Text = _currentSpeed.ToString();
                SpeedText.Opacity = 1.0;
                
                // Reset fade timer
                _fadeTimer?.Stop();
                _fadeTimer?.Start();
            }
        });
    }

    public void HideOverlay()
    {
        _fadeTimer?.Stop();
        Dispatcher.InvokeAsync(Hide);
    }
}
