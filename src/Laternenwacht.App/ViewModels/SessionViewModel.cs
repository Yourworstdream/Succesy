using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Laternenwacht.App.Services;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.ViewModels;

/// <summary>
/// Präsentationslogik der laufenden Wacht. Wird von der Fokusleiste und vom Hauptfenster
/// gemeinsam verwendet, damit beide stets denselben Zustand zeigen.
/// </summary>
internal sealed class SessionViewModel : ObservableObject
{
    public static readonly int[] Presets = [15, 25, 50, 90];

    private readonly FocusWarden _warden;
    private readonly DispatcherTimer _timer;
    private int _durationMinutes = 25;
    private string _headline = string.Empty;
    private string _detail = string.Empty;
    private string _remainingText = "00:00";
    private string _focusedText = "00:00";
    private string _frostText = "00:00";
    private string _awayText = "00:00";
    private int _distractionCount;
    private double _progress;
    private double _frostRatio;
    private RealmMood _mood = RealmMood.Spring;
    private ActivityState _state = ActivityState.Focused;
    private string _proverb = Lore.Proverb(Environment.TickCount);
    private readonly HashSet<string> _shownAdmonitions = [];
    private string? _admonition;
    private long _admonitionUntil;
    private int _admonitionSeed;

    public SessionViewModel(FocusWarden warden)
    {
        _warden = warden ?? throw new ArgumentNullException(nameof(warden));
        _warden.SessionEnded += (_, record) => SessionEnded?.Invoke(this, record);

        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Pulse();

        StartCommand = new RelayCommand(Start, () => !IsActive);
        StartPresetCommand = new ParameterCommand(p =>
        {
            if (int.TryParse(Convert.ToString(p, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
            {
                DurationMinutes = minutes;
                Start();
            }
        }, () => !IsActive);
        PauseResumeCommand = new RelayCommand(PauseResume, () => IsActive);
        AbortCommand = new RelayCommand(() =>
        {
            if (ConfirmAbort?.Invoke() != false)
            {
                Abort();
            }
        }, () => IsActive);
        ShowChamberCommand = new RelayCommand(() => ShowChamberRequested?.Invoke(this, EventArgs.Empty));

        Refresh();
    }

    /// <summary>Wird ausgelöst, wenn eine Wacht endet – der Aufrufer trägt sie in die Chronik ein.</summary>
    public event EventHandler<SessionRecord>? SessionEnded;

    /// <summary>Die Fokusleiste bittet darum, das Hauptfenster zu zeigen.</summary>
    public event EventHandler? ShowChamberRequested;

    /// <summary>Eine Wacht wurde begonnen.</summary>
    public event EventHandler? SessionStarted;

    /// <summary>Ein neuer Mahnruf ist fällig – die Oberfläche überbringt ihn als Benachrichtigung.</summary>
    public event EventHandler<Admonition>? AdmonitionRaised;

    /// <summary>Aus welchem Buch der Chroniken die Mahnrufe stammen.</summary>
    public ChronicleBook SayingsBook { get; set; } = ChronicleBook.All;

    /// <summary>Rückfrage vor dem Abbrechen (wird von der Oberfläche gesetzt).</summary>
    public Func<bool>? ConfirmAbort { get; set; }

    public ICommand StartCommand { get; }

    public ICommand StartPresetCommand { get; }

    public ICommand PauseResumeCommand { get; }

    public ICommand AbortCommand { get; }

    public ICommand ShowChamberCommand { get; }

    public int DurationMinutes
    {
        get => _durationMinutes;
        set => SetProperty(ref _durationMinutes, Math.Clamp(value, 1, 480));
    }

    public bool IsActive => _warden.IsActive;

    public bool IsPaused => _warden.Current?.Phase == SessionPhase.Paused;

    public bool HasSession => _warden.Current is not null;

    public string PauseResumeLabel => IsPaused ? "Weiterziehen" : "Rasten";

    public string Headline { get => _headline; private set => SetProperty(ref _headline, value); }

    public string Detail { get => _detail; private set => SetProperty(ref _detail, value); }

    public string RemainingText { get => _remainingText; private set => SetProperty(ref _remainingText, value); }

    public string FocusedText { get => _focusedText; private set => SetProperty(ref _focusedText, value); }

    /// <summary>Kumulierte Ablenkungszeit – die zentrale Kennzahl der Fokusleiste.</summary>
    public string FrostText { get => _frostText; private set => SetProperty(ref _frostText, value); }

    public string AwayText { get => _awayText; private set => SetProperty(ref _awayText, value); }

    public int DistractionCount { get => _distractionCount; private set => SetProperty(ref _distractionCount, value); }

    public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }

    public double FrostRatio { get => _frostRatio; private set => SetProperty(ref _frostRatio, value); }

    public string FrostPercentText => FrostRatio.ToString("P0", CultureInfo.GetCultureInfo("de-DE"));

    public RealmMood Mood { get => _mood; private set => SetProperty(ref _mood, value); }

    public string MoodName => Lore.MoodName(Mood);

    public ActivityState State { get => _state; private set => SetProperty(ref _state, value); }

    public bool IsDistracted => IsActive && !IsPaused && State == ActivityState.Distracted;

    public bool IsAway => IsActive && !IsPaused && State == ActivityState.Away;

    public bool IsLit => !IsActive || IsPaused || State == ActivityState.Focused;

    public string Proverb { get => _proverb; private set => SetProperty(ref _proverb, value); }

    /// <summary>Aktueller augenzwinkernder Mahnruf (wird einige Sekunden lang gezeigt).</summary>
    public string? Admonition { get => _admonition; private set => SetProperty(ref _admonition, value); }

    public bool HasAdmonition => !string.IsNullOrEmpty(Admonition);

    /// <summary>Wie lange ein Mahnruf sichtbar bleibt.</summary>
    public static TimeSpan AdmonitionDuration { get; } = TimeSpan.FromSeconds(15);

    private void Start()
    {
        if (_warden.IsActive)
        {
            return;
        }

        _warden.Start(TimeSpan.FromMinutes(DurationMinutes));
        _shownAdmonitions.Clear();
        _admonitionSeed = Random.Shared.Next();
        Admonition = null;
        Proverb = Lore.Proverb(Environment.TickCount);
        _timer.Start();
        Refresh();
        SessionStarted?.Invoke(this, EventArgs.Empty);
    }

    private void PauseResume()
    {
        if (IsPaused)
        {
            _warden.Resume();
        }
        else
        {
            _warden.Pause();
        }

        Refresh();
    }

    /// <summary>Bricht die laufende Wacht ab; sie wird dennoch in der Chronik verzeichnet.</summary>
    public void Abort()
    {
        _warden.Abort();
        _timer.Stop();
        Refresh();
    }

    private void Pulse()
    {
        try
        {
            _warden.Pulse();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            AppLog.Error("Messung", ex);
        }

        if (!_warden.IsActive)
        {
            _timer.Stop();
        }

        Refresh();
    }

    private void Refresh()
    {
        var session = _warden.Current;
        (Headline, Detail) = Lore.Describe(session, _warden.LastSnapshot);

        if (session is not null)
        {
            RemainingText = TimeFormat.Clock(session.Remaining);
            FocusedText = TimeFormat.Clock(session.Focused);
            FrostText = TimeFormat.Clock(session.Distracted);
            AwayText = TimeFormat.Clock(session.Away);
            DistractionCount = session.DistractionCount;
            Progress = session.Progress;
            FrostRatio = session.FrostRatio;
            Mood = RealmMoods.FromFrost(session.FrostRatio);
            State = session.CurrentState;
            UpdateAdmonition(session);
        }
        else
        {
            RemainingText = TimeFormat.Clock(TimeSpan.FromMinutes(DurationMinutes));
        }

        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(HasSession));
        OnPropertyChanged(nameof(PauseResumeLabel));
        OnPropertyChanged(nameof(IsDistracted));
        OnPropertyChanged(nameof(IsAway));
        OnPropertyChanged(nameof(IsLit));
        OnPropertyChanged(nameof(FrostPercentText));
        OnPropertyChanged(nameof(MoodName));
        OnPropertyChanged(nameof(HasAdmonition));
        RelayCommand.Refresh();
    }

    private void UpdateAdmonition(FocusSession session)
    {
        var now = Environment.TickCount64;
        if (session.Phase == SessionPhase.Running
            && Admonitions.Next(session.DistractionCount, session.Distracted, _shownAdmonitions, SayingsBook, _admonitionSeed) is { } next)
        {
            Admonition = next.Text;
            _admonitionUntil = now + (long)AdmonitionDuration.TotalMilliseconds;
            AdmonitionRaised?.Invoke(this, next);
        }
        else if (Admonition is not null && (now >= _admonitionUntil || session.IsFinished))
        {
            Admonition = null;
        }
    }
}

/// <summary>ICommand mit Parameter (für die Schnellwahl-Schaltflächen).</summary>
internal sealed class ParameterCommand(Action<object?> execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            execute(parameter);
        }
    }
}
