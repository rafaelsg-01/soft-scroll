using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace SoftScroll.UI;

public partial class AccessibilityAnnouncer : Window
{
    private DispatcherTimer? _hideTimer;
    private static AccessibilityAnnouncer? _instance;
    
    public static AccessibilityAnnouncer Instance => _instance ??= new AccessibilityAnnouncer();

    public AccessibilityAnnouncer()
    {
        InitializeComponent();
        _instance = this;
        
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer?.Stop();
            FadeOut();
        };
        
        // Position at bottom center
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        Width = Math.Min(500, screenWidth - 40);
        Left = (screenWidth - Width) / 2;
        Top = screenHeight - 120;
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

    public void Announce(string message)
    {
        Dispatcher.Invoke(() =>
        {
            AnnouncementText.Text = message;
            Opacity = 0;
            Show();
            Activate();
            
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            BeginAnimation(OpacityProperty, fade);

            // Reset hide timer
            _hideTimer?.Stop();
            _hideTimer?.Start();
        });
    }

    public void AnnounceScrollSpeed(int pixelsPerNotch)
    {
        Announce($"Scroll speed: {pixelsPerNotch} pixels per notch");
    }

    public void AnnouncePosition(int currentPosition)
    {
        Announce($"Position: {currentPosition}%");
    }
}
