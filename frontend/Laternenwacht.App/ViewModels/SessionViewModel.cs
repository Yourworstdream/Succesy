using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Laternenwacht.App.Services;
using Laternenwacht.App.Views;
using Laternenwacht.Platform.Windows;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.ViewModels;

/// <summary>
/// Präsentationslogik der laufenden Wacht. Wird von der Fokusleiste und vom Hauptfenster
/// gemeinsam verwendet, damit beide stets denselben Zustand zeigen.
/// </summary>
/// <remarks>
/// <para>Die Wacht wird als Reise durch „Der König von Narnia“ erzählt (<see cref="Journey"/>): Die Station hängt allein
/// an gemessener / geplanter Zeit, jede neue Verlockung ist ein Stück Türkischer Honig, und nach dem Ende zeigt die Seite
/// das Abschlussbild der Jahreszeit (<see cref="HasEnding"/>), bis eine neue Wacht vorbereitet wird.</para>
/// <para>Sparsam im Sekundentakt: Alle angezeigten Werte sind gespeichert und melden sich nur, wenn sie sich
/// tatsächlich ändern (sonst müssten beide Fenster jede Sekunde alle Bindungen neu auswerten). Texte, die nur von der
/// Station, der Zahl der Honigstücke oder dem Zustand abhängen, entstehen nur, wenn sich diese ändern; die Liste der
/// Honigmarken wird nur bei einem neuen Stück ersetzt. Die Ausführbarkeit der Befehle wird nur bei einem
/// Zustandswechsel neu abgefragt.</para>
/// </remarks>
internal sealed class SessionViewModel : ObservableObject
{
    public static readonly int[] Presets = [15, 25, 50, 90];

    /// <summary>Wie viele Sorten die Schachtel der Königin zeigt.</summary>
    private const int QueenBoxSize = 3;

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

    /// <summary>Puffer für die Schachtel der Königin – im Sekundentakt ohne neue Listen oder Einträge gefüllt.</summary>
    private readonly KeyValuePair<string, TimeSpan>[] _queenBuffer = new KeyValuePair<string, TimeSpan>[QueenBoxSize];

    // Zuletzt formatierte Werte: Texte entstehen nur neu, wenn sich die angezeigte Sekunde bzw. Zahl ändert.
    private long _remainingSeconds = -1;
    private long _focusedSeconds = -1;
    private long _frostSeconds = -1;
    private long _awaySeconds = -1;
    private long _streakSeconds = -1;
    private long _longestSeconds = -1;
    private long _goalRemainingSeconds = -1;
    private int _goalMinutes = -1;
    private int _frostPercent = -1;
    private TimeSpan _plannedFor = TimeSpan.MinValue;
    private string _stationEyebrow = Lore.Eyebrow(1);

    // Wovon Statuszeile, Untertitel und Kurzstatus zuletzt abhingen (Wacht, Phase, Zustand, Rückkehr, angezeigte Sekunde,
    // Programm, Verlockungszeile). Die Texte entstehen nur neu, wenn sich davon etwas ändert.
    private (FocusSession? Session, SessionPhase? Phase, ActivityState State, bool Returning, long Seconds, string? Process, string Temptation) _storyKey;
    private bool _hasStoryKey;
    private string? _admonition;
    private long _admonitionUntil;
    private int _admonitionSeed;

    // ===== Reise =====
    private int _stationNumber;
    private string _stationName = string.Empty;
    private string _stationNarration = string.Empty;
    private string _stationLabel = string.Empty;
    private string _nextStationText = string.Empty;
    private string _journeyDescription = string.Empty;
    private string _eyebrow = Lore.EyebrowReady;
    private IReadOnlyList<double> _honeyMarks = [];
    private int _honeyMarkCount;
    private string _honeyText = Lore.Honey(0);
    private string _temptationLine = string.Empty;
    private TemptationChapter? _temptationChapter;
    private string? _temptationProcess;
    private double _winterFrostSeconds;
    private double _plannedSeconds = 25 * 60;
    private string _winterMeterText = string.Empty;
    private string _winterMeterDescription = string.Empty;
    private bool _isWinterMeterWarning;
    private bool _isReturning;
    private long _returningUntil;
    private TimeSpan _returnedFrost;
    private bool _isLongTemptation;
    private string _archLabel = string.Empty;
    private string _archText = string.Empty;
    private ImageSource? _scene;
    private double _sceneFrostLevel;
    private string _frostAllowanceText = string.Empty;
    private string _readyRouteText = string.Empty;
    private string _startLabel = string.Empty;

    // ===== Ende =====
    private bool _hasEnding;
    private JourneyEnding _ending = JourneyEnding.Wardrobe;
    private bool _isCompleted;
    private string _endingEyebrow = string.Empty;
    private string _endingLabel = string.Empty;
    private string _endingTitle = string.Empty;
    private string _endingNarration = string.Empty;
    private string _endingPill = string.Empty;
    private string _endingStationLabel = string.Empty;
    private string _endingCaption = string.Empty;
    private string _endingRouteText = string.Empty;
    private ImageSource? _endingScene;
    private bool _isFlawless;
    private bool _hasRecordStreak;
    private string _recordStreakText = string.Empty;
    private bool _hasQueenBoxItems;

    public SessionViewModel(FocusWarden warden)
    {
        _warden = warden ?? throw new ArgumentNullException(nameof(warden));
        _warden.SessionEnded += (_, record) =>
        {
            // Erst das Abschlussbild festhalten (der Vergleich mit der bisherigen Bestleistung braucht die Chronik
            // vor dem Eintrag), dann den Aufrufer die Wacht verzeichnen lassen.
            ShowEnding(record);
            SessionEnded?.Invoke(this, record);
        };
        _warden.ReturnedToWork += (_, back) =>
        {
            _returnedFrost = back.Absence;
            _returningUntil = Environment.TickCount64 + (long)Lore.ReturnWelcomeDuration.TotalMilliseconds;
        };

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
        NewWatchCommand = new RelayCommand(PrepareNewWatch, () => !IsActive);

        UpdateReadyTexts();
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

    /// <summary>
    /// Längste Fokus-Serie aller bisherigen Chronik-Einträge (vor dem Eintrag der eben beendeten Wacht);
    /// wird beim Zusammensetzen gesetzt. Ohne Quelle gibt es kein Schriftband für eine neue Bestmarke.
    /// </summary>
    public Func<TimeSpan>? PreviousBestStreak { get; set; }

    public ICommand StartCommand { get; }

    public ICommand StartPresetCommand { get; }

    public ICommand PauseResumeCommand { get; }

    public ICommand AbortCommand { get; }

    public ICommand ShowChamberCommand { get; }

    /// <summary>Schließt das Abschlussbild und öffnet den Schrank für eine neue Wacht.</summary>
    public ICommand NewWatchCommand { get; }

    public int DurationMinutes
    {
        get => _durationMinutes;
        set
        {
            if (SetProperty(ref _durationMinutes, Math.Clamp(value, 1, 480)))
            {
                UpdateReadyTexts();
                if (!_warden.IsActive)
                {
                    Refresh();
                }
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (SetProperty(ref _isActive, value))
            {
                OnPropertyChanged(nameof(IsReady));
            }
        }
    }

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

    /// <summary>Der Schrank steht offen: keine Wacht unterwegs und kein Abschlussbild offen.</summary>
    public bool IsReady => !IsActive && !HasEnding;

    public string PauseResumeLabel => IsPaused ? "Weiterziehen" : "Rasten";

    /// <summary>Statuszeile (Pille im Bild), z. B. "Die Laterne brennt hell".</summary>
    public string Headline { get => _headline; private set => SetProperty(ref _headline, value); }

    /// <summary>Untertitel der Seite „Die Wacht“ – je nach Zustand aus der Geschichte erzählt.</summary>
    public string Detail { get => _detail; private set => SetProperty(ref _detail, value); }

    /// <summary>Augenbraue über dem Seitentitel, z. B. "KAPITEL I · STATION 3 VON 10".</summary>
    public string Eyebrow { get => _eyebrow; private set => SetProperty(ref _eyebrow, value); }

    public string RemainingText { get => _remainingText; private set => SetProperty(ref _remainingText, value); }

    public string FocusedText { get => _focusedText; private set => SetProperty(ref _focusedText, value); }

    /// <summary>Kumulierte Ablenkungszeit – die zentrale Kennzahl der Fokusleiste.</summary>
    public string FrostText { get => _frostText; private set => SetProperty(ref _frostText, value); }

    public string AwayText { get => _awayText; private set => SetProperty(ref _awayText, value); }

    /// <summary>Kurzstatus für die Fokusleiste.</summary>
    public string ShortStatus { get => _shortStatus; private set => SetProperty(ref _shortStatus, value); }

    /// <summary>"von 25:00 Minuten" unter der Restzeit.</summary>
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

    /// <summary>Zurückgelegter Anteil der Reise (gemessen / geplant, 0–1).</summary>
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

    // ===== Reise =====

    /// <summary>Erreichte Station 1–10 (allein aus gemessener / geplanter Zeit).</summary>
    public int StationNumber { get => _stationNumber; private set => SetProperty(ref _stationNumber, value); }

    /// <summary>Name der erreichten Station, z. B. "Die leere Höhle".</summary>
    public string StationName { get => _stationName; private set => SetProperty(ref _stationName, value); }

    /// <summary>Erzählung der Station (kursiv im Bogenbild).</summary>
    public string StationNarration { get => _stationNarration; private set => SetProperty(ref _stationNarration, value); }

    /// <summary>"STATION 3 VON 10 · DIE LEERE HÖHLE".</summary>
    public string StationLabel { get => _stationLabel; private set => SetProperty(ref _stationLabel, value); }

    /// <summary>"Nächste Station: Der Biberdamm · in 2:02", bei Rast "Die Reise steht still, solange du rastest."</summary>
    public string NextStationText { get => _nextStationText; private set => SetProperty(ref _nextStationText, value); }

    /// <summary>Beschreibung des Wegbands für Screenreader (ändert sich nur mit Station und Honig).</summary>
    public string JourneyDescription { get => _journeyDescription; private set => SetProperty(ref _journeyDescription, value); }

    /// <summary>
    /// Wo auf der Reise jedes Stück Türkischer Honig genommen wurde (Anteile 0–1 der geplanten Zeit).
    /// Eine neue Liste entsteht nur, wenn ein Stück dazukommt.
    /// </summary>
    public IReadOnlyList<double> HoneyMarks { get => _honeyMarks; private set => SetProperty(ref _honeyMarks, value); }

    /// <summary>"3 Stück".</summary>
    public string HoneyText { get => _honeyText; private set => SetProperty(ref _honeyText, value); }

    /// <summary>Wie die Königin gerade lockt (nach Edmunds Versuchungskapitel der erreichten Station).</summary>
    public string TemptationLine { get => _temptationLine; private set => SetProperty(ref _temptationLine, value); }

    /// <summary>Frost in ganzen Sekunden (für das Wintermesser).</summary>
    public double WinterFrostSeconds { get => _winterFrostSeconds; private set => SetProperty(ref _winterFrostSeconds, value); }

    /// <summary>Geplante Dauer in Sekunden (für das Wintermesser).</summary>
    public double PlannedSeconds { get => _plannedSeconds; private set => SetProperty(ref _plannedSeconds, value); }

    /// <summary>"Noch 1:58 Frost, dann wird aus Frühling Tauwetter."</summary>
    public string WinterMeterText { get => _winterMeterText; private set => SetProperty(ref _winterMeterText, value); }

    /// <summary>Beschreibung des Wintermessers für Screenreader.</summary>
    public string WinterMeterDescription { get => _winterMeterDescription; private set => SetProperty(ref _winterMeterDescription, value); }

    /// <summary>Die nächste Jahreszeitgrenze ist weniger als eine Minute Frost entfernt.</summary>
    public bool IsWinterMeterWarning { get => _isWinterMeterWarning; private set => SetProperty(ref _isWinterMeterWarning, value); }

    /// <summary>Etwa 20 Sekunden nach der Rückkehr von einer Verlockung: empfangen wie Edmund am Steinernen Tisch.</summary>
    public bool IsReturning { get => _isReturning; private set => SetProperty(ref _isReturning, value); }

    /// <summary>Beschriftung über der Erzählzeile im Bogenbild.</summary>
    public string ArchLabel { get => _archLabel; private set => SetProperty(ref _archLabel, value); }

    /// <summary>Erzählzeile im Bogenbild (Station, Rückkehr oder "Anders als Edmund …").</summary>
    public string ArchText { get => _archText; private set => SetProperty(ref _archText, value); }

    /// <summary>Szenenbild des Zustands: Schrank (bereit), Laternenpfahl, Schlitten (Verlockung) oder das Abschlussbild.</summary>
    public ImageSource? Scene { get => _scene; private set => SetProperty(ref _scene, value); }

    /// <summary>Deckkraft des Frostschleiers über dem Bild (0 außerhalb einer Verlockung).</summary>
    public double SceneFrostLevel { get => _sceneFrostLevel; private set => SetProperty(ref _sceneFrostLevel, value); }

    /// <summary>Die Schachtel der Königin: meistgenutzte Verlockungen mit Dauer (laufend bzw. aus dem Chronik-Eintrag).</summary>
    public ObservableCollection<QueenBoxItem> QueenBox { get; } = [];

    public bool HasQueenBoxItems { get => _hasQueenBoxItems; private set => SetProperty(ref _hasQueenBoxItems, value); }

    /// <summary>"Frühling bis 2:30 Frost · Tauwetter bis 6:15" (für die gewählte Dauer).</summary>
    public string FrostAllowanceText { get => _frostAllowanceText; private set => SetProperty(ref _frostAllowanceText, value); }

    /// <summary>"Vom Laternenpfahl bis Cair Paravel · 25:00".</summary>
    public string ReadyRouteText { get => _readyRouteText; private set => SetProperty(ref _readyRouteText, value); }

    /// <summary>"Laterne entzünden · 25 min".</summary>
    public string StartLabel { get => _startLabel; private set => SetProperty(ref _startLabel, value); }

    // ===== Ende =====

    /// <summary>Das Abschlussbild der zuletzt beendeten Wacht wird gezeigt.</summary>
    public bool HasEnding
    {
        get => _hasEnding;
        private set
        {
            if (SetProperty(ref _hasEnding, value))
            {
                OnPropertyChanged(nameof(IsReady));
            }
        }
    }

    /// <summary>Welches Abschlussbild die Jahreszeit malt.</summary>
    public JourneyEnding Ending { get => _ending; private set => SetProperty(ref _ending, value); }

    /// <summary>Die beendete Wacht wurde vollendet (nicht abgebrochen).</summary>
    public bool IsCompleted { get => _isCompleted; private set => SetProperty(ref _isCompleted, value); }

    public string EndingEyebrow { get => _endingEyebrow; private set => SetProperty(ref _endingEyebrow, value); }

    /// <summary>"FRÜHLING", "TAUWETTER", "WINTER" oder "ABGEBROCHEN".</summary>
    public string EndingLabel { get => _endingLabel; private set => SetProperty(ref _endingLabel, value); }

    /// <summary>z. B. "Krönung in Cair Paravel".</summary>
    public string EndingTitle { get => _endingTitle; private set => SetProperty(ref _endingTitle, value); }

    public string EndingNarration { get => _endingNarration; private set => SetProperty(ref _endingNarration, value); }

    /// <summary>"Frühling · 7 % Frost" bzw. "Abgebrochen · Station 6".</summary>
    public string EndingPill { get => _endingPill; private set => SetProperty(ref _endingPill, value); }

    /// <summary>"STATION 10 · CAIR PARAVEL".</summary>
    public string EndingStationLabel { get => _endingStationLabel; private set => SetProperty(ref _endingStationLabel, value); }

    /// <summary>"vollendet · 25:00" bzw. "9:20 von 15:00".</summary>
    public string EndingCaption { get => _endingCaption; private set => SetProperty(ref _endingCaption, value); }

    /// <summary>"Alle zehn Stationen" bzw. "Bis Cair Paravel fehlten 5:40".</summary>
    public string EndingRouteText { get => _endingRouteText; private set => SetProperty(ref _endingRouteText, value); }

    public ImageSource? EndingScene { get => _endingScene; private set => SetProperty(ref _endingScene, value); }

    /// <summary>Kein einziges Stück Türkischer Honig: "Die Schachtel blieb zu".</summary>
    public bool IsFlawless { get => _isFlawless; private set => SetProperty(ref _isFlawless, value); }

    /// <summary>Krone über der Laterne auf dem Wegband (Krönung in Cair Paravel).</summary>
    public bool ShowCrown => HasEnding && Ending == JourneyEnding.Coronation;

    /// <summary>Die längste Serie dieser Wacht übertrifft alle bisherigen Chronik-Einträge.</summary>
    public bool HasRecordStreak { get => _hasRecordStreak; private set => SetProperty(ref _hasRecordStreak, value); }

    public string RecordStreakText { get => _recordStreakText; private set => SetProperty(ref _recordStreakText, value); }

    private void Start()
    {
        if (_warden.IsActive)
        {
            return;
        }

        HasEnding = false;
        OnPropertyChanged(nameof(ShowCrown));
        _warden.Start(TimeSpan.FromMinutes(DurationMinutes));
        _shownAdmonitions.Clear();
        _admonitionSeed = Random.Shared.Next();
        _returningUntil = 0;
        _honeyMarkCount = -1;   // Honigmarken der neuen Wacht in jedem Fall neu aufbauen
        QueenBox.Clear();
        HasQueenBoxItems = false;
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

    /// <summary>"Neue Wacht": Abschlussbild schließen, der Schrank steht wieder offen.</summary>
    private void PrepareNewWatch()
    {
        if (_warden.IsActive)
        {
            return;
        }

        HasEnding = false;
        OnPropertyChanged(nameof(ShowCrown));
        Proverb = Lore.Proverb(Environment.TickCount);
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
        // Nach "Neue Wacht" zählt die alte, beendete Wacht nicht mehr als gezeigt.
        var session = _warden.Current is { IsFinished: true } && !HasEnding ? null : _warden.Current;
        var running = session is { Phase: SessionPhase.Running };
        var returning = running && session!.CurrentState == ActivityState.Focused && Environment.TickCount64 < _returningUntil;

        if (session is not null)
        {
            if (ClockChanged(ref _remainingSeconds, session.Remaining))
            {
                RemainingText = TimeFormat.Clock(session.Remaining);
            }

            if (ClockChanged(ref _focusedSeconds, session.Focused))
            {
                FocusedText = TimeFormat.Clock(session.Focused);
            }

            if (ClockChanged(ref _frostSeconds, session.Distracted))
            {
                FrostText = TimeFormat.Clock(session.Distracted);
            }

            if (ClockChanged(ref _awaySeconds, session.Away))
            {
                AwayText = TimeFormat.Clock(session.Away);
            }

            if (ClockChanged(ref _streakSeconds, session.CurrentStreak))
            {
                CurrentStreakText = TimeFormat.Clock(session.CurrentStreak);
            }

            UpdatePlannedText(session.Planned);
            FocusWeight = session.Focused.TotalSeconds;
            FrostWeight = session.Distracted.TotalSeconds;
            AwayWeight = session.Away.TotalSeconds;
            UpdateNextGoal(session.CurrentStreak);
            if (ClockChanged(ref _longestSeconds, session.LongestStreak))
            {
                LongestStreakText = TimeFormat.Clock(session.LongestStreak);
            }

            DistractionCount = session.DistractionCount;
            Progress = session.Progress;
            FrostRatio = session.FrostRatio;
            Mood = RealmMoods.FromFrost(session.FrostRatio);
            State = session.CurrentState;
            UpdateAdmonition(session);
            UpdateJourney(session);
        }
        else
        {
            var planned = TimeSpan.FromMinutes(DurationMinutes);
            if (ClockChanged(ref _remainingSeconds, planned))
            {
                RemainingText = TimeFormat.Clock(planned);
            }

            UpdatePlannedText(planned);
            PlannedSeconds = planned.TotalSeconds;
            Progress = 0;
            UpdateStation(1);
        }

        // Nach der Reise, damit die Verlockungszeile nur bei neuem Kapitel oder Programm entsteht. Neu gebildet wird nur,
        // wenn sich ein Baustein der Texte ändert – während einer Verlockung oder Rast also gar nicht im Sekundentakt.
        var storyKey = (session, session?.Phase, session?.CurrentState ?? ActivityState.Focused, returning,
            StorySeconds(session, returning), session?.CurrentProcess, TemptationLine);
        if (!_hasStoryKey || storyKey != _storyKey)
        {
            _hasStoryKey = true;
            _storyKey = storyKey;
            (Headline, Detail) = Lore.Describe(session, _warden.LastSnapshot, returning ? _returnedFrost : null, TemptationLine);
            ShortStatus = Lore.Short(session);
        }

        // Abgeleitete Zustände – melden sich nur bei Änderung.
        var commandState = (IsActive, IsPaused, HasMarkCandidate);
        IsActive = _warden.IsActive;
        IsPaused = session?.Phase == SessionPhase.Paused;
        HasSession = session is not null;
        IsDistracted = running && State == ActivityState.Distracted;
        IsAway = running && State == ActivityState.Away;
        IsReturning = returning;
        IsLit = !running || State == ActivityState.Focused;
        var frostPercent = (int)Math.Round(FrostRatio * 100, MidpointRounding.AwayFromZero);
        if (frostPercent != _frostPercent)
        {
            _frostPercent = frostPercent;
            FrostPercentText = FrostRatio.ToString("P0", German);
        }

        MarkCandidate = IsActive && State != ActivityState.Distracted
            && _warden.LastSnapshot?.ProcessName is { Length: > 0 } process
            && !string.Equals(process, _warden.SelfProcessName, StringComparison.OrdinalIgnoreCase)
                ? process
                : null;

        UpdateStory(session);

        // Schaltflächen nur dann neu bewerten, wenn sich etwas für ihre Ausführbarkeit geändert hat.
        if (commandState != (IsActive, IsPaused, HasMarkCandidate))
        {
            RelayCommand.Refresh();
        }
    }

    /// <summary>
    /// Die Sekunde, die Statuszeile und Kurzstatus anzeigen: im Licht die laufende Serie, bei Abwesenheit die Leerlaufzeit,
    /// sonst 0 (die Texte enthalten dann keine laufende Zeit).
    /// </summary>
    private long StorySeconds(FocusSession? session, bool returning)
    {
        if (session is not { Phase: SessionPhase.Running } || returning)
        {
            return 0;
        }

        return session.CurrentState switch
        {
            ActivityState.Focused => (long)session.CurrentStreak.TotalSeconds,
            ActivityState.Away => (long)(_warden.LastSnapshot?.IdleTime ?? TimeSpan.Zero).TotalSeconds,
            _ => 0,
        };
    }

    /// <summary>Station, nächste Station, Honigmarken, Wintermesser und Schachtel der laufenden (oder beendeten) Wacht.</summary>
    private void UpdateJourney(FocusSession session)
    {
        var planned = session.Planned;
        var measured = session.Measured;
        var station = Journey.StationNumber(measured, planned);
        var honeyChanged = session.DistractionCount != _honeyMarkCount;
        var stationChanged = UpdateStation(station);

        // Honig: neue Liste nur bei einem neuen Stück (DistractionStarts wächst nur bei Episodenbeginn).
        if (honeyChanged)
        {
            var starts = session.DistractionStarts;
            var marks = new double[starts.Count];
            for (var i = 0; i < marks.Length; i++)
            {
                marks[i] = Journey.Fraction(starts[i], planned);
            }

            _honeyMarkCount = session.DistractionCount;
            HoneyMarks = marks;
            HoneyText = Lore.Honey(_honeyMarkCount);
        }

        if (stationChanged || honeyChanged)
        {
            JourneyDescription = Lore.JourneyDescription(station, _honeyMarkCount);
        }

        NextStationText = session.Phase == SessionPhase.Paused
            ? Lore.RestRoute
            : Lore.NextStation(station, Journey.UntilNextStation(measured, planned));

        // Verlockungstext nur neu, wenn sich Kapitel oder Programm ändern.
        var chapter = Journey.ChapterFor(station);
        if (chapter != _temptationChapter || !string.Equals(session.CurrentProcess, _temptationProcess, StringComparison.Ordinal))
        {
            _temptationChapter = chapter;
            _temptationProcess = session.CurrentProcess;
            TemptationLine = Journey.TemptationLine(chapter, session.CurrentProcess);
        }

        // Wintermesser: ganze Sekunden, Text nur bei Änderung.
        var frostSeconds = Math.Floor(session.Distracted.TotalSeconds);
        if (frostSeconds != WinterFrostSeconds || planned.TotalSeconds != PlannedSeconds || WinterMeterText.Length == 0)
        {
            WinterFrostSeconds = frostSeconds;
            PlannedSeconds = planned.TotalSeconds;
            var frost = TimeSpan.FromSeconds(frostSeconds);
            WinterMeterText = Lore.WinterMeterLine(frost, planned, out var warning);
            IsWinterMeterWarning = warning;
            WinterMeterDescription = Lore.WinterMeterDescription(frost, planned);
        }

        // Frostschleier: wächst mit dem Frost, in Stufen von 2 %, damit das Bild nicht jede Sekunde neu gezeichnet wird.
        if (session.Phase == SessionPhase.Running && session.CurrentState == ActivityState.Distracted && planned > TimeSpan.Zero)
        {
            var level = 0.35 + (0.65 * Math.Min(1, session.Distracted / planned / RealmMoods.WinterThreshold));
            SceneFrostLevel = Math.Round(level * 50) / 50;

            var starts = session.DistractionStarts;
            _isLongTemptation = starts.Count > 0 && measured - starts[^1] >= Lore.LongTemptation;
            UpdateQueenBox(_queenBuffer.AsSpan(0, session.TopDistractions(_queenBuffer)));
        }
        else
        {
            SceneFrostLevel = 0;
            _isLongTemptation = false;
        }
    }

    /// <summary>Setzt Station, Name, Erzählung und Beschriftungen – nur, wenn sich die Station ändert.</summary>
    /// <returns><c>true</c>, wenn sich die Station geändert hat.</returns>
    private bool UpdateStation(int station)
    {
        if (station == StationNumber)
        {
            return false;
        }

        var current = Journey.Stations[station - 1];
        StationNumber = station;
        StationName = current.Name;
        StationNarration = current.Narration;
        StationLabel = Lore.StationLabel(current);
        _stationEyebrow = Lore.Eyebrow(station);
        return true;
    }

    /// <summary>Szene, Augenbraue und Zeile im Bogenbild – abhängig vom Zustand, nicht von der Uhr.</summary>
    private void UpdateStory(FocusSession? session)
    {
        if (HasEnding)
        {
            Eyebrow = EndingEyebrow;
            Scene = EndingScene;
            return;
        }

        if (session is null || !IsActive)
        {
            Eyebrow = Lore.EyebrowReady;
            Scene = SceneLibrary.Wardrobe;
            ArchLabel = string.Empty;
            ArchText = string.Empty;
            return;
        }

        Eyebrow = _stationEyebrow;
        if (IsDistracted)
        {
            Scene = SceneLibrary.Sledge;
            ArchLabel = Lore.TemptationArchLabel;
            ArchText = _isLongTemptation ? Lore.TemptationArchTextLong : Lore.TemptationArchText;
        }
        else if (IsReturning)
        {
            Scene = SceneLibrary.Lantern;
            ArchLabel = Lore.ReturnArchLabel;
            ArchText = Lore.ReturnArchText;
        }
        else
        {
            Scene = SceneLibrary.Lantern;
            ArchLabel = StationLabel;
            ArchText = StationNarration;
        }
    }

    /// <summary>Aktualisiert die Schachtel der Königin an Ort und Stelle (vorhandene Zeilen werden wiederverwendet).</summary>
    private void UpdateQueenBox(ReadOnlySpan<KeyValuePair<string, TimeSpan>> entries)
    {
        for (var i = 0; i < entries.Length; i++)
        {
            if (i < QueenBox.Count)
            {
                QueenBox[i].Update(entries[i].Key, entries[i].Value);
            }
            else
            {
                var item = new QueenBoxItem();
                item.Update(entries[i].Key, entries[i].Value);
                QueenBox.Add(item);
            }
        }

        while (QueenBox.Count > entries.Length)
        {
            QueenBox.RemoveAt(QueenBox.Count - 1);
        }

        HasQueenBoxItems = QueenBox.Count > 0;
    }

    /// <summary>Schachtel aus dem Chronik-Eintrag der beendeten Wacht (einmalig, nicht im Sekundentakt).</summary>
    private void UpdateQueenBox(IReadOnlyList<DistractionEntry> entries)
    {
        // Alle Sorten des Eintrags (bis zu fünf), nicht nur die drei der laufenden Anzeige.
        var all = new KeyValuePair<string, TimeSpan>[entries.Count];
        for (var i = 0; i < all.Length; i++)
        {
            all[i] = new KeyValuePair<string, TimeSpan>(entries[i].ProcessName, entries[i].Duration);
        }

        UpdateQueenBox(all);
    }

    /// <summary>Hält das Abschlussbild der eben beendeten Wacht fest (vor dem Eintrag in die Chronik).</summary>
    private void ShowEnding(SessionRecord record)
    {
        var ending = Journey.EndingFor(record);
        var text = Journey.Describe(record);
        var station = Journey.StationAt(record.Measured, record.Planned);
        var previousBest = PreviousBestStreak?.Invoke() ?? TimeSpan.Zero;

        Ending = ending;
        IsCompleted = record.Outcome == SessionPhase.Completed;
        EndingEyebrow = IsCompleted ? Lore.EyebrowCompleted : Lore.EyebrowAborted;
        EndingLabel = text.Label;
        EndingTitle = text.Title;
        EndingNarration = text.Narration;
        EndingPill = Lore.EndingPill(record, station.Number);
        EndingStationLabel = Lore.EndingStationLabel(station);
        EndingCaption = Lore.EndingCaption(record);
        EndingRouteText = Lore.EndingRoute(record);
        EndingScene = ending switch
        {
            JourneyEnding.Coronation => SceneLibrary.Coronation,
            JourneyEnding.Thaw => SceneLibrary.Thaw,
            JourneyEnding.StoneCourtyard => SceneLibrary.StoneCourtyard,
            _ => SceneLibrary.Wardrobe,
        };
        IsFlawless = record.DistractionCount == 0;

        // Eine Bestmarke braucht frühere Einträge mit Serie – sonst würde die erste Wacht fälschlich gefeiert.
        HasRecordStreak = previousBest > TimeSpan.Zero && record.LongestFocusStreak > previousBest;
        RecordStreakText = HasRecordStreak ? Lore.RecordStreak(record.LongestFocusStreak) : string.Empty;

        UpdateQueenBox(record.TopDistractions);
        SceneFrostLevel = 0;
        HasEnding = true;
        OnPropertyChanged(nameof(ShowCrown));
    }

    /// <summary>Texte des offenen Schranks, abhängig von der gewählten Dauer.</summary>
    private void UpdateReadyTexts()
    {
        var planned = TimeSpan.FromMinutes(DurationMinutes);
        FrostAllowanceText = Lore.FrostAllowance(planned);
        ReadyRouteText = Lore.ReadyRoute(planned);
        StartLabel = string.Create(CultureInfo.InvariantCulture, $"Laterne entzünden · {DurationMinutes} min");
    }

    /// <summary>
    /// Berechnet das nächste positive Ziel (Lob-Schwelle) aus der laufenden Fokus-Serie.
    /// Die Texte entstehen nur neu, wenn sich die Schwelle bzw. die angezeigte Restsekunde ändert.
    /// </summary>
    private void UpdateNextGoal(TimeSpan streak)
    {
        var previous = TimeSpan.Zero;
        foreach (var minutes in Praises.StreakMilestones)
        {
            var target = TimeSpan.FromMinutes(minutes);
            if (streak < target)
            {
                if (minutes != _goalMinutes)
                {
                    _goalMinutes = minutes;
                    _goalRemainingSeconds = -1;
                    NextGoalText = string.Create(CultureInfo.InvariantCulture, $"{minutes} Minuten am Stück im Licht");
                }

                var remainingSeconds = (long)Math.Ceiling((target - streak).TotalSeconds);
                if (remainingSeconds != _goalRemainingSeconds)
                {
                    _goalRemainingSeconds = remainingSeconds;
                    NextGoalRemainingText = "noch " + Lore.SpanUp(target - streak);
                }

                NextGoalProgress = (streak - previous) / (target - previous);
                return;
            }

            previous = target;
        }

        if (_goalMinutes != 0)
        {
            _goalMinutes = 0;
            _goalRemainingSeconds = -1;
            NextGoalText = "Alle Ziele erreicht – eine Legende!";
        }

        if (ClockChanged(ref _goalRemainingSeconds, streak))
        {
            NextGoalRemainingText = TimeFormat.Clock(streak);
        }

        NextGoalProgress = 1;
    }

    /// <summary>"von 25:00 Minuten" – nur neu, wenn sich die geplante Dauer ändert.</summary>
    private void UpdatePlannedText(TimeSpan planned)
    {
        if (planned != _plannedFor)
        {
            _plannedFor = planned;
            PlannedText = Lore.PlannedLine(planned);
        }
    }

    /// <summary>
    /// <c>true</c>, wenn sich die ganze Sekunde gegenüber dem zuletzt formatierten Wert geändert hat
    /// (gleiche Rundung wie <see cref="TimeFormat.Clock"/>); merkt sich dann die neue Sekunde.
    /// </summary>
    private static bool ClockChanged(ref long lastSeconds, TimeSpan value)
    {
        var seconds = value <= TimeSpan.Zero ? 0 : (long)value.TotalSeconds;
        if (seconds == lastSeconds)
        {
            return false;
        }

        lastSeconds = seconds;
        return true;
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

/// <summary>Eine Sorte aus der Schachtel der Königin: Programm und wie lange es lockte.</summary>
internal sealed class QueenBoxItem : ObservableObject
{
    private string _name = string.Empty;
    private string _durationText = string.Empty;
    private long _durationSeconds = -1;

    /// <summary>Programmname, z. B. "Hearthstone".</summary>
    public string Name { get => _name; private set => SetProperty(ref _name, value); }

    /// <summary>Dauer, z. B. "1:41".</summary>
    public string DurationText { get => _durationText; private set => SetProperty(ref _durationText, value); }

    /// <summary>Übernimmt Programm und Dauer; der Dauertext entsteht nur neu, wenn sich die angezeigte Sekunde ändert.</summary>
    public void Update(string processName, TimeSpan duration)
    {
        Name = processName;
        var seconds = duration <= TimeSpan.Zero ? 0 : (long)duration.TotalSeconds;
        if (seconds != _durationSeconds)
        {
            _durationSeconds = seconds;
            DurationText = Lore.Span(duration);
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
