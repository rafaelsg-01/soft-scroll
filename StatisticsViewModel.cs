using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoftScroll;

public sealed class StatisticsViewModel : INotifyPropertyChanged
{
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    public StatisticsViewModel()
    {
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => RefreshAll();
        _timer.Start();
    }

    public void RefreshAll()
    {
        OnPropertyChanged(nameof(TotalEvents));
        OnPropertyChanged(nameof(TotalPixels));
        OnPropertyChanged(nameof(SessionEvents));
        OnPropertyChanged(nameof(SessionPixels));
        OnPropertyChanged(nameof(ActiveTime));
    }

    public void Stop() => _timer.Stop();

    public string TotalEvents => ScrollStatistics.Instance.TotalScrollEvents.ToString("N0");
    public string TotalPixels => ScrollStatistics.Instance.FormattedTotalPixels;
    public string SessionEvents => ScrollStatistics.Instance.SessionScrollEvents.ToString("N0");
    public string SessionPixels => ScrollStatistics.Instance.FormattedSessionPixels;
    public string ActiveTime => ScrollStatistics.Instance.FormattedActiveTime;

    public void ResetAll()
    {
        ScrollStatistics.Instance.Reset();
        RefreshAll();
    }

    public void ResetSession()
    {
        ScrollStatistics.Instance.ResetSession();
        RefreshAll();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
