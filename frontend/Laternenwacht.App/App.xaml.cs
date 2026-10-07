using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Laternenwacht.App.Services;
using Laternenwacht.App.ViewModels;
using Laternenwacht.App.Views;
using Laternenwacht.Platform.Windows;
using Laternenwacht.Core.Integrity;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App;

/// <summary>
/// Einstiegspunkt und Kompositionswurzel: Hier werden alle Dienste erzeugt und verbunden.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "Application ist nicht IDisposable; der Mutex wird in OnExit freigegeben.")]
public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\Laternenwacht.EinzigeInstanz";
    private static readonly TimeSpan CompletionBannerDuration = TimeSpan.FromSeconds(12);

    /// <summary>Wartezeit nach dem Minimieren, bevor Speicher zurückgegeben wird (kurzes Hin und Her soll nichts auslösen).</summary>
    private static readonly TimeSpan BackgroundReliefDelay = TimeSpan.FromSeconds(3);

    /// <summary>Höchste Bildrate für Animationen, sofern eine Animation nichts anderes verlangt.</summary>
    private const int DefaultAnimationFrameRate = 30;

    private Mutex? _singleInstance;
    private FocusBarWindow? _bar;
    private MainWindow? _main;
    private SessionViewModel? _session;
    private DispatcherTimer? _hideBarTimer;
    private DispatcherTimer? _reliefTimer;

    static App()
    {
        // WPF animiert sonst mit bis zu 60 Bildern je Sekunde. Für Überblendungen und Bewegungen dieser
        // Oberfläche genügen 30 – das halbiert die Arbeit, solange etwas animiert wird.
        Timeline.DesiredFrameRateProperty.OverrideMetadata(
            typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = DefaultAnimationFrameRate });
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Die Laternenwacht brennt bereits. Es kann nur eine Laterne zugleich gehütet werden.",
                "Laternenwacht", MessageBoxButton.OK, MessageBoxImage.Information);
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                AppLog.Error("Unbehandelt", ex);
            }
        };

        // --- Einstellungen ---
        var settingsStore = new SettingsStore(AppPaths.SettingsFile);
        var settings = settingsStore.Load();

        // --- Chronik (versiegeltes Journal) ---
        JournalOpenResult? journal = null;
        string? journalError = null;
        try
        {
            journal = JournalBootstrapper.Open(AppPaths.DataDirectory, new DpapiSecretProtector(), TimeProvider.System);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Chronik öffnen", ex);
            journalError = ex.Message;
        }

        // --- Fachlogik ---
        using var self = Process.GetCurrentProcess();
        var selfName = self.ProcessName;
        var warden = new FocusWarden(new Win32ActivityProbe(), TimeProvider.System, settings, selfName);

        // --- ViewModels ---
        _session = new SessionViewModel(warden)
        {
            DurationMinutes = (int)settings.DefaultDuration.TotalMinutes,
            SayingsBook = settings.SayingsBook,
        };
        var chronicle = new ChronicleViewModel(journal, journalError);
        var memes = new MemeService();
        var settingsViewModel = new SettingsViewModel(settingsStore, settings, memes)
        {
            PickMemeFiles = PickMemeFiles,
        };
        var shell = new ShellViewModel(_session, chronicle, settingsViewModel);

        // --- Fenster ---
        _main = new MainWindow { DataContext = shell };
        _bar = new FocusBarWindow { DataContext = new BarViewModel(_session, settingsViewModel), Topmost = settings.BarAlwaysOnTop };
        _bar.ApplyPosition(settings.BarLeft, settings.BarTop);
        _bar.PositionChosen += (_, position) => settingsViewModel.SaveBarPosition(position.X, position.Y);
        MainWindow = _main;

        // Jede neue Ablenkung schickt ein Meme auf die Reise.
        warden.DistractionStarted += (_, distraction) =>
        {
            if (settingsViewModel.ShowMemes)
            {
                memes.Show(distraction);
            }
        };

        var notifications = new NotificationService();

        // Positives würdigen: Rückkehr zur Arbeit (wie Edmund am Steinernen Tisch – ohne Vorwurf) und lange Fokus-Serien.
        warden.ReturnedToWork += (_, back) =>
        {
            memes.SinkCurrent();   // die Schachtel der Königin versinkt im Schnee
            if (settingsViewModel.ShowPraise)
            {
                notifications.ShowWelcome(
                    Homecomings.For(settingsViewModel.SelectedBook, back.Absence, Random.Shared.Next()), back.Absence);
            }
        };
        warden.FocusStreakReached += (_, streak) =>
        {
            if (settingsViewModel.ShowPraise)
            {
                notifications.ShowStreakPraise(streak.Minutes,
                    Praises.ForStreak(settingsViewModel.SelectedBook, streak.Index, Random.Shared.Next()));
            }
        };
        // Mahnrufe: Bei „Der König von Narnia“ spricht die Königin, und eine Stimme aus Narnia antwortet (Admonition.Reply).
        _session.AdmonitionRaised += (_, admonition) =>
        {
            if (settingsViewModel.ShowNotifications)
            {
                notifications.Show(admonition);
            }
        };

        _session.ConfirmAbort = () => MessageBox.Show(_main,
            "Willst du die Wacht wirklich abbrechen? Sie wird dennoch in der Chronik verzeichnet.",
            "Wacht abbrechen", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

        _session.SessionStarted += (_, _) => ShowBar();
        _session.ShowChamberRequested += (_, _) => _main.BringToFront();
        _session.SessionEnded += (_, record) =>
        {
            // Eine Bestmarke zählt nur gegen frühere Einträge mit gemessener Serie – sonst würde die erste Wacht
            // nach dem Update (alte Einträge haben LongestFocusStreak = 0) fälschlich gefeiert.
            var previousBest = chronicle.BestStreak;
            OnSessionEnded(record, chronicle);
            if (settingsViewModel.ShowPraise)
            {
                PraiseFinishedSession(notifications, record, previousBest);
            }
        };

        // Die gewählte Dauer der Bereit-Karte nur überschreiben, wenn sich die Standarddauer wirklich geändert hat –
        // sonst setzte schon ein Klick auf einen Buchrücken (Schnellschalter) die eben gewählte Dauer zurück.
        var lastDefaultDuration = settings.DefaultDuration;
        settingsViewModel.SettingsSaved += (_, saved) =>
        {
            warden.ApplySettings(saved);
            _session.SayingsBook = saved.SayingsBook;
            _bar.Topmost = saved.BarAlwaysOnTop;
            _bar.ApplyPosition(saved.BarLeft, saved.BarTop);
            if (!_session.IsActive && saved.DefaultDuration != lastDefaultDuration)
            {
                _session.DurationMinutes = (int)saved.DefaultDuration.TotalMinutes;
            }

            lastDefaultDuration = saved.DefaultDuration;
        };

        _main.Closing += (_, args) =>
        {
            if (_session.IsActive && _session.ConfirmAbort?.Invoke() == false)
            {
                args.Cancel = true;
                return;
            }

            _session.Abort();
        };
        _main.Closed += (_, _) => _bar.Close();
        WatchForBackground(_main);

        _main.Show();

        if (journal is { ArchivedTo: not null } || journalError is not null)
        {
            _main.ShowChronicle();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_singleInstance is not null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Ist das Hauptfenster einige Sekunden minimiert, gibt die Anwendung nicht mehr benötigten
    /// Arbeitsspeicher an Windows zurück. Danach läuft nur noch die schlanke Messung im Sekundentakt.
    /// </summary>
    private void WatchForBackground(Window main)
    {
        _reliefTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle) { Interval = BackgroundReliefDelay };
        _reliefTimer.Tick += (_, _) =>
        {
            _reliefTimer.Stop();
            if (main.WindowState == WindowState.Minimized)
            {
                MemoryRelief.Release();
            }
        };

        main.StateChanged += (_, _) =>
        {
            _reliefTimer.Stop();
            if (main.WindowState == WindowState.Minimized)
            {
                _reliefTimer.Start();
            }
        };
    }

    private void ShowBar()
    {
        if (_bar is null)
        {
            return;
        }

        _hideBarTimer?.Stop();
        _bar.Show();
        _bar.Reposition();
    }

    /// <summary>
    /// Würdigt am Ende einer Wacht, was gut lief – auch bei einer abgebrochenen. Die Kopfzeile ist bei einer
    /// vollendeten Wacht der Titel ihres Abschlussbildes (z. B. „Krönung in Cair Paravel“).
    /// </summary>
    private static void PraiseFinishedSession(NotificationService notifications, SessionRecord record, TimeSpan previousBest)
    {
        if (record.Focused < TimeSpan.FromMinutes(1))
        {
            return;
        }

        var parts = new List<string>();
        if (record.Outcome == SessionPhase.Completed && record.DistractionCount == 0)
        {
            parts.Add("Die Schachtel blieb zu – kein einziges Stück Türkischer Honig in dieser Wacht.");
        }

        if (previousBest > TimeSpan.Zero && record.LongestFocusStreak > previousBest)
        {
            parts.Add($"Neue Bestmarke: {TimeFormat.Clock(record.LongestFocusStreak)} am Stück im Licht. " +
                "So lange hast du die Laterne noch nie gehalten.");
        }

        parts.Add($"{TimeFormat.Clock(record.Focused)} im Licht – gut gemacht.");

        var header = record.Outcome == SessionPhase.Completed
            ? Journey.Describe(record).Title
            : "Auch eine kurze Wacht zählt";
        notifications.ShowPraise(header, string.Join(" ", parts), "Laternenwacht");
    }

    private void OnSessionEnded(SessionRecord record, ChronicleViewModel chronicle)
    {
        chronicle.Record(record);

        if (record.Outcome == SessionPhase.Completed)
        {
            SystemSounds.Asterisk.Play();
        }

        // Das Banner zeigt die Jahreszeit noch einen Moment lang, dann verschwindet es.
        _hideBarTimer?.Stop();
        _hideBarTimer = new DispatcherTimer { Interval = CompletionBannerDuration };
        _hideBarTimer.Tick += (_, _) =>
        {
            _hideBarTimer.Stop();
            if (_session is { IsActive: false })
            {
                _bar?.Hide();
            }
        };
        _hideBarTimer.Start();

        if (record.Outcome == SessionPhase.Completed)
        {
            _main?.BringToFront();
        }
    }

    private string[] PickMemeFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Memes für den Fluss der Ablenkung auswählen",
            Filter = "Bilder (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = true,
            CheckFileExists = true,
        };

        return dialog.ShowDialog(_main) == true ? dialog.FileNames : [];
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Error("Oberfläche", e.Exception);
        MessageBox.Show(
            "Ein unerwarteter Fehler ist aufgetreten. Die Laterne brennt weiter, doch bitte prüfe das Protokoll:\n" + AppPaths.LogFile,
            "Laternenwacht", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
