using System.Diagnostics;
using System.Media;
using System.Windows;
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

    private Mutex? _singleInstance;
    private FocusBarWindow? _bar;
    private MainWindow? _main;
    private SessionViewModel? _session;
    private DispatcherTimer? _hideBarTimer;

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

        // Positives würdigen: Rückkehr zur Arbeit und lange Fokus-Serien.
        warden.ReturnedToWork += (_, back) =>
        {
            memes.SinkCurrent();
            if (settingsViewModel.ShowPraise)
            {
                notifications.ShowEncouragement("Willkommen zurück im Licht",
                    Homecomings.For(settingsViewModel.SelectedBook, back.Absence, Random.Shared.Next()));
            }
        };
        warden.FocusStreakReached += (_, streak) =>
        {
            if (settingsViewModel.ShowPraise)
            {
                notifications.ShowEncouragement($"{streak.Minutes} Minuten am Stück im Licht",
                    Praises.ForStreak(settingsViewModel.SelectedBook, streak.Index, Random.Shared.Next()));
            }
        };
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
            var previousBest = chronicle.BestStreak;
            var hadHistory = chronicle.Entries.Count > 0;
            OnSessionEnded(record, chronicle);
            if (settingsViewModel.ShowPraise)
            {
                PraiseFinishedSession(notifications, record, previousBest, hadHistory);
            }
        };

        settingsViewModel.SettingsSaved += (_, saved) =>
        {
            warden.ApplySettings(saved);
            _session.SayingsBook = saved.SayingsBook;
            _bar.Topmost = saved.BarAlwaysOnTop;
            _bar.ApplyPosition(saved.BarLeft, saved.BarTop);
            if (!_session.IsActive)
            {
                _session.DurationMinutes = (int)saved.DefaultDuration.TotalMinutes;
            }
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

    /// <summary>Würdigt am Ende einer Wacht, was gut lief – auch bei einer abgebrochenen.</summary>
    private static void PraiseFinishedSession(NotificationService notifications, SessionRecord record, TimeSpan previousBest, bool hadHistory)
    {
        if (record.Focused < TimeSpan.FromMinutes(1))
        {
            return;
        }

        var parts = new List<string>();
        if (record.Outcome == SessionPhase.Completed && record.DistractionCount == 0)
        {
            parts.Add("Makellose Wacht – kein einziges Mal verlockt!");
        }

        if (hadHistory && record.LongestFocusStreak > previousBest)
        {
            parts.Add($"Neue Bestleistung: {TimeFormat.Clock(record.LongestFocusStreak)} am Stück im Licht!");
        }

        parts.Add($"{TimeFormat.Clock(record.Focused)} im Licht – gut gemacht.");

        var header = record.Outcome == SessionPhase.Completed
            ? Lore.Completed(RealmMoods.FromFrost(record.Measured <= TimeSpan.Zero ? 0 : record.Distracted / record.Measured)).Headline
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
