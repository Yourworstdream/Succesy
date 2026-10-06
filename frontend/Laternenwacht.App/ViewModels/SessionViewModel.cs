using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Laternenwacht.App.Services;
using Laternenwacht.Platform.Windows;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.ViewModels;

/// <summary>
/// Präsentationslogik der laufenden Wacht. Wird von der Fokusleiste und vom Hauptfenster
/// gemeinsam verwendet, damit beide stets denselben Zustand zeigen.
/// </summary>
/// <remarks>
/// Sparsam im Sekundentakt: Alle angezeigten Werte sind gespeichert und melden sich nur, wenn sie sich
/// tatsächlich ändern (sonst müssten beide Fenster jede Sekunde alle Bindungen neu auswerten).
/// Die Ausführbarkeit der Befehle wird nur bei einem Zustandswechsel neu abgefragt.
/// </remarks>
internal sealed class SessionViewModel : ObservableObject
{
    public static readonly int[] Presets = [15, 25, 50, 90];

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private readonly FocusWarden _warden;
    private readonly DispatcherTimer _timer;
    private int _durationMinutes = 25;
    private string _headline = string.Empty;
    private string _detail = string.Empty;
    private string _remainingText = "00:00";
    private string _focusedText = "00:00";
    private string _frostText = "00:00";
    private string _awayText = "00:00";
    private string _currentStreakText = "00:00";
    private string _shortStatus = string.Empty;
    private string _plannedText = string.Empty;
    private double _focusWeight;
    private double _frostWeight;
    private double _awayWeight;
    private string _nextGoalText = string.Empty;
    private string _nextGoalRemainingText = string.Empty;
    private double _nextGoalProgress;
    private string _longestStreakText = "00:00";
    private int _distractionCount;
    private double _progress;
    private double _frostRatio;
    private RealmMood _mood = RealmMood.Spring;
    private ActivityState _state = ActivityState.Focused;
    private bool _isActive;
    private bool _isPaused;
    private bool _hasSession;
    private bool _isDistracted;
    private bool _isAway;
    private bool _isLit = true;
    private string _frostPercentText = "0 %";
    private string? _markCandidate;
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
        set
        {
            if (SetProperty(ref _durationMinutes, Math.Clamp(value, 1, 480)) && _warden.Current is null)
            {
                RemainingText = TimeFormat.Clock(TimeSpan.FromMinutes(_durationMinutes));
            }
        }
    }

    public bool IsActive { get => _isActive; private set => SetProperty(ref _isActive, value); }

    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            if (SetProperty(ref _isPaused, value))
            {
                OnPropertyChanged(nameof(PauseResumeLabel));
            }
        }
    }

    public bool HasSession { get => _hasSession; private set => SetProperty(ref _hasSession, value); }

    public string PauseResumeLabel => IsPaused ? "Weiterziehen" : "Rasten";

    public string Headline { get => _headline; private set => SetProperty(ref _headline, value); }

    public string Detail { get => _detail; private set => SetProperty(ref _detail, value); }

    public string RemainingText { get => _remainingText; private set => SetProperty(ref _remainingText, value); }

    public string FocusedText { get => _focusedText; private set => SetProperty(ref _focusedText, value); }

    /// <summary>Kumulierte Ablenkungszeit – die zentrale Kennzahl der Fokusleiste.</summary>
    public string FrostText { get => _frostText; private set => SetProperty(ref _frostText, value); }

    public string AwayText { get => _awayText; private set => SetProperty(ref _awayText, value); }

    /// <summary>Kurzstatus für die Fokusleiste.</summary>
    public string ShortStatus { get => _shortStatus; private set => SetProperty(ref _shortStatus, value); }

    /// <summary>"von 25:00" unter der Restzeit.</summary>
    public string PlannedText { get => _plannedText; private set => SetProperty(ref _plannedText, value); }

    /// <summary>Anteile für den Verteilungsbalken (Licht / Frost / abwesend), als Gewichte.</summary>
    public double FocusWeight { get => _focusWeight; private set => SetProperty(ref _focusWeight, value); }

    public double FrostWeight { get => _frostWeight; private set => SetProperty(ref _frostWeight, value); }

    public double AwayWeight { get => _awayWeight; private set => SetProperty(ref _awayWeight, value); }

    /// <summary>Nächste Lob-Schwelle der Fokus-Serie, z. B. "25 Minuten am Stück im Licht".</summary>
    public string NextGoalText { get => _nextGoalText; private set => SetProperty(ref _nextGoalText, value); }

    /// <summary>z. B. "noch 4:29".</summary>
    public string NextGoalRemainingText { get => _nextGoalRemainingText; private set => SetProperty(ref _nextGoalRemainingText, value); }

    /// <summary>Fortschritt zur nächsten Schwelle (0–1).</summary>
    public double NextGoalProgress { get => _nextGoalProgress; private set => SetProperty(ref _nextGoalProgress, value); }

    /// <summary>Fokuszeit seit der letzten Ablenkung.</summary>
    public string CurrentStreakText { get => _currentStreakText; private set => SetProperty(ref _currentStreakText, value); }

    /// <summary>Längste Fokus-Serie dieser Wacht.</summary>
    public string LongestStreakText { get => _longestStreakText; private set => SetProperty(ref _longestStreakText, value); }

    public int DistractionCount { get => _distractionCount; private set => SetProperty(ref _distractionCount, value); }

    public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }

    public double FrostRatio { get => _frostRatio; private set => SetProperty(ref _frostRatio, value); }

    public string FrostPercentText { get => _frostPercentText; private set => SetProperty(ref _frostPercentText, value); }

    public RealmMood Mood
    {
        get => _mood;
        private set
        {
            if (SetProperty(ref _mood, value))
            {
                OnPropertyChanged(nameof(MoodName));
            }
        }
    }

    public string MoodName => Lore.MoodName(Mood);

    public ActivityState State { get => _state; private set => SetProperty(ref _state, value); }

    public bool IsDistracted { get => _isDistracted; private set => SetProperty(ref _isDistracted, value); }

    public bool IsAway { get => _isAway; private set => SetProperty(ref _isAway, value); }

    /// <summary>
    /// Fremdes Programm im Vordergrund, das gerade nicht als Ablenkung zählt – Kandidat für
    /// "als Verlockung markieren" im Rechtsklick-Menü (die Leiste stiehlt keinen Fokus,
    /// daher ist das beim Rechtsklick noch das eben benutzte Programm).
    /// </summary>
    public string? MarkCandidate
    {
        get => _markCandidate;
        private set
        {
            if (SetProperty(ref _markCandidate, value))
            {
                OnPropertyChanged(nameof(HasMarkCandidate));
                OnPropertyChanged(nameof(MarkCandidateLabel));
            }
        }
    }

    public bool HasMarkCandidate => MarkCandidate is not null;

    public string MarkCandidateLabel => MarkCandidate is { } p ? $"„{p}“ als Verlockung markieren" : "Aktuelles Programm als Verlockung markieren";

    public bool IsLit { get => _isLit; private set => SetProperty(ref _isLit, value); }

    public string Proverb { get => _proverb; private set => SetProperty(ref _proverb, value); }

    /// <summary>Aktueller augenzwinkernder Mahnruf (wird einige Sekunden lang gezeigt).</summary>
    public string? Admonition
    {
        get => _admonition;
        private set
        {
            if (SetProperty(ref _admonition, value))
            {
                OnPropertyChanged(nameof(HasAdmonition));
            }
        }
    }

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
        ShortStatus = Lore.Short(session);

        if (session is not null)
        {
            RemainingText = TimeFormat.Clock(session.Remaining);
            FocusedText = TimeFormat.Clock(session.Focused);
            FrostText = TimeFormat.Clock(session.Distracted);
            AwayText = TimeFormat.Clock(session.Away);
            CurrentStreakText = TimeFormat.Clock(session.CurrentStreak);
            PlannedText = "von " + TimeFormat.Clock(session.Planned);
            FocusWeight = session.Focused.TotalSeconds;
            FrostWeight = session.Distracted.TotalSeconds;
            AwayWeight = session.Away.TotalSeconds;
            UpdateNextGoal(session.CurrentStreak);
            LongestStreakText = TimeFormat.Clock(session.LongestStreak);
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
            PlannedText = "bereit";
        }

        // Abgeleitete Zustände – melden sich nur bei Änderung.
        var commandState = (IsActive, IsPaused, HasMarkCandidate);
        IsActive = _warden.IsActive;
        IsPaused = session?.Phase == SessionPhase.Paused;
        HasSession = session is not null;
        var running = IsActive && !IsPaused;
        IsDistracted = running && State == ActivityState.Distracted;
        IsAway = running && State == ActivityState.Away;
        IsLit = !running || State == ActivityState.Focused;
        FrostPercentText = FrostRatio.ToString("P0", German);
        MarkCandidate = IsActive && State != ActivityState.Distracted
            && _warden.LastSnapshot?.ProcessName is { Length: > 0 } process
            && !string.Equals(process, _warden.SelfProcessName, StringComparison.OrdinalIgnoreCase)
                ? process
                : null;

        // Schaltflächen nur dann neu bewerten, wenn sich etwas für ihre Ausführbarkeit geändert hat.
        if (commandState != (IsActive, IsPaused, HasMarkCandidate))
        {
            RelayCommand.Refresh();
        }
    }

    /// <summary>Berechnet das nächste positive Ziel (Lob-Schwelle) aus der laufenden Fokus-Serie.</summary>
    private void UpdateNextGoal(TimeSpan streak)
    {
        var previous = TimeSpan.Zero;
        foreach (var minutes in Praises.StreakMilestones)
        {
            var target = TimeSpan.FromMinutes(minutes);
            if (streak < target)
            {
                NextGoalText = $"{minutes} Minuten am Stück im Licht";
                NextGoalRemainingText = "noch " + TimeFormat.Clock(target - streak);
                NextGoalProgress = (streak - previous) / (target - previous);
                return;
            }

            previous = target;
        }

        NextGoalText = "Alle Ziele erreicht – eine Legende!";
        NextGoalRemainingText = TimeFormat.Clock(streak);
        NextGoalProgress = 1;
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
