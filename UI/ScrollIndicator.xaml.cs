using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SoftScroll.Settings;

namespace SoftScroll.UI;

public partial class ScrollIndicator : Window
{
    private DispatcherTimer? _hideTimer;
    private int _lastSpeed;

    public ScrollIndicator()
    {
        InitializeComponent();
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer?.Stop();
            FadeOut();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set initial position to top-right
        var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        var screenHeight = (int)SystemParameters.PrimaryScreenHeight;
        Left = screenWidth - Width - 20;
        Top = 20;
    }

    private void FadeOut()
    {
        Dispatcher.Invoke(() =>
        {
            var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(300));
            fade.Completed += (s, e) => Hide();
            BeginAnimation(OpacityProperty, fade);
        });
    }

    public void ShowAt(int screenX, int screenY, int speed)
    {
        Dispatcher.Invoke(() =>
        {
            // Position near cursor
            Left = Math.Max(0, Math.Min(screenX - Width / 2, SystemParameters.PrimaryScreenWidth - Width));
            Top = Math.Max(0, Math.Min(screenY - Height / 2, SystemParameters.PrimaryScreenHeight - Height));
            
            // Update speed text
            _lastSpeed = Math.Abs(speed);
            SpeedText.Text = _lastSpeed.ToString();
            
            // Make visible with fade-in
            Opacity = 0;
            Show();
            Activate();
            
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            BeginAnimation(OpacityProperty, fade);

            // Reset hide timer
            _hideTimer?.Stop();
            _hideTimer?.Start();
        });
    }

    public void UpdateSpeed(int speed)
    {
        Dispatcher.Invoke(() =>
        {
            _lastSpeed = Math.Abs(speed);
            SpeedText.Text = _lastSpeed.ToString();

            // Reset hide timer
            _hideTimer?.Stop();
            _hideTimer?.Start();
        });
    }

    public void SetPosition(IndicatorPosition position)
    {
        Dispatcher.Invoke(() =>
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            switch (position)
            {
                case IndicatorPosition.TopLeft:
                    Left = 20;
                    Top = 20;
                    break;
                case IndicatorPosition.TopRight:
                    Left = screenWidth - Width - 20;
                    Top = 20;
                    break;
                case IndicatorPosition.BottomLeft:
                    Left = 20;
                    Top = screenHeight - Height - 20;
                    break;
                case IndicatorPosition.BottomRight:
                    Left = screenWidth - Width - 20;
                    Top = screenHeight - Height - 20;
                    break;
                case IndicatorPosition.Center:
                    Left = (screenWidth - Width) / 2;
                    Top = (screenHeight - Height) / 2;
                    break;
            }
        });
    }
}
