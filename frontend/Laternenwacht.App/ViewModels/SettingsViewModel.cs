using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Laternenwacht.App.Services;
using Laternenwacht.Platform.Windows;
using Laternenwacht.Core.Media;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.App.ViewModels;

/// <summary>Bearbeitung der Einstellungen ("Gefährten &amp; Verlockungen") mit Validierung.</summary>
internal sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private readonly MemeService _memes;
    private FocusSettings _current;
    private string _allowedText = string.Empty;
    private string _distractingText = string.Empty;
    private string _durationText = string.Empty;
    private string _idleText = string.Empty;
    private bool _useAllowList;
    private bool _barAlwaysOnTop;
    private string? _statusMessage;
    private bool _statusIsError;

    public SettingsViewModel(SettingsStore store, FocusSettings current, MemeService memes)
    {
        _memes = memes ?? throw new ArgumentNullException(nameof(memes));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _current = current ?? throw new ArgumentNullException(nameof(current));
        LoadFrom(current);

        if (store.LastLoadWarning is { } warning)
        {
            SetStatus(warning + " Es gelten die Standardwerte.", isError: true);
        }

        BookOptions = [.. Enum.GetValues<ChronicleBook>().Select(b => new BookOption(b, () => SelectedBook = b))];
        UpdateBookSelection();

        SaveCommand = new RelayCommand(Save);
        ResetBarPositionCommand = new RelayCommand(ResetBarPosition, () => _current.HasCustomBarPosition);
        ToggleNotificationsCommand = new RelayCommand(() => ShowNotifications = !ShowNotifications);
        ToggleMemesCommand = new RelayCommand(() => ShowMemes = !ShowMemes);
        TogglePraiseCommand = new RelayCommand(() => ShowPraise = !ShowPraise);
        ToggleKnownDistractionsCommand = new RelayCommand(() => UseKnownDistractions = !UseKnownDistractions);
        OpenMemeFolderCommand = new RelayCommand(OpenMemeFolder);
        AddMemesCommand = new RelayCommand(AddMemes);
        ResetCommand = new RelayCommand(() => LoadFrom(_current));
        DefaultsCommand = new RelayCommand(() => LoadFrom(FocusSettings.Default));
    }

    /// <summary>Neue, gültige Einstellungen wurden gespeichert.</summary>
    public event EventHandler<FocusSettings>? SettingsSaved;

    public FocusSettings Current => _current;

    public ICommand SaveCommand { get; }

    public ICommand ResetBarPositionCommand { get; }

    public ICommand ToggleNotificationsCommand { get; }

    public ICommand ToggleMemesCommand { get; }

    public ICommand TogglePraiseCommand { get; }

    /// <summary>Lob für Fokus-Serien, "Willkommen zurück" und Bestleistungen. Wird sofort gespeichert.</summary>
    public bool ShowPraise
    {
        get => _current.ShowPraise;
        set
        {
            if (value != _current.ShowPraise && ApplyQuickChange(_current with { ShowPraise = value }))
            {
                OnPropertyChanged();
            }
        }
    }

    public ICommand ToggleKnownDistractionsCommand { get; }

    /// <summary>Eingebauten Katalog bekannter Verlockungen (Spiele, Launcher, Messenger) verwenden. Wird sofort gespeichert.</summary>
    public bool UseKnownDistractions
    {
        get => _current.UseKnownDistractions;
        set
        {
            if (value != _current.UseKnownDistractions && ApplyQuickChange(_current with { UseKnownDistractions = value }))
            {
                OnPropertyChanged();
            }
        }
    }

    public string KnownDistractionsList { get; } = string.Join(", ", KnownDistractions.Names.Order(StringComparer.Ordinal));

    /// <summary>Trägt ein Programm sofort als Verlockung ein (Rechtsklick auf die Leiste).</summary>
    public void MarkAsDistraction(string processName)
    {
        if (SettingsEditing.MarkAsDistraction(_current, processName) is not { } updated)
        {
            SetStatus($"„{processName}“ konnte nicht als Verlockung eingetragen werden.", isError: true);
            return;
        }

        if (ApplyQuickChange(updated))
        {
            // Nur die Listenfelder aktualisieren, übrige ungespeicherte Formulareingaben bleiben erhalten.
            AllowedText = string.Join(Environment.NewLine, updated.AllowedProcesses);
            DistractingText = string.Join(Environment.NewLine, updated.DistractingProcesses);
            SetStatus($"„{ProcessNames.Normalize(processName)}“ zählt ab sofort als Verlockung.", isError: false);
        }
    }

    public ICommand OpenMemeFolderCommand { get; }

    public ICommand AddMemesCommand { get; }

    /// <summary>Dateiauswahl für "Memes hinzufügen…" (wird von der Oberfläche gesetzt).</summary>
    public Func<IReadOnlyList<string>>? PickMemeFiles { get; set; }

    /// <summary>Bei jeder neuen Ablenkung treibt ein Meme über den Bildschirm. Wird sofort gespeichert.</summary>
    public bool ShowMemes
    {
        get => _current.ShowMemes;
        set
        {
            if (value != _current.ShowMemes && ApplyQuickChange(_current with { ShowMemes = value }))
            {
                OnPropertyChanged();
            }
        }
    }

    public string MemeSummary => $"{_memes.BuiltInCount} eingebaute und {_memes.CustomCount} eigene Memes treiben im Fluss.";

    /// <summary>Auswahlmöglichkeiten "Alle Chroniken" und die sieben Bücher.</summary>
    public IReadOnlyList<BookOption> BookOptions { get; }

    /// <summary>Das Buch, aus dem die Mahnrufe stammen. Wird sofort gespeichert.</summary>
    public ChronicleBook SelectedBook
    {
        get => _current.SayingsBook;
        set
        {
            if (value != _current.SayingsBook && ApplyQuickChange(_current with { SayingsBook = value }))
            {
                UpdateBookSelection();
                SetStatus($"Die Mahnrufe stammen nun aus: {ChronicleBooks.Title(value)}.", isError: false);
            }
        }
    }

    public BookOption? SelectedBookOption
    {
        get => BookOptions.FirstOrDefault(o => o.Book == SelectedBook);
        set
        {
            if (value is not null)
            {
                SelectedBook = value.Book;
            }
        }
    }

    /// <summary>Mahnrufe als Push-Benachrichtigung zeigen. Wird sofort gespeichert.</summary>
    public bool ShowNotifications
    {
        get => _current.ShowNotifications;
        set
        {
            if (value != _current.ShowNotifications && ApplyQuickChange(_current with { ShowNotifications = value }))
            {
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Merkt sich die vom Benutzer verschobene Position der Fokusleiste.</summary>
    public void SaveBarPosition(double left, double top) =>
        ApplyQuickChange(_current with { BarLeft = Math.Round(left, 1), BarTop = Math.Round(top, 1) });

    /// <summary>Die Leiste kehrt an den oberen Bildschirmrand zurück.</summary>
    public void ResetBarPosition() => ApplyQuickChange(_current with { BarLeft = null, BarTop = null });

    public ICommand ResetCommand { get; }

    public ICommand DefaultsCommand { get; }

    public ObservableCollection<string> Errors { get; } = [];

    public string AllowedText { get => _allowedText; set => SetProperty(ref _allowedText, value); }

    public string DistractingText { get => _distractingText; set => SetProperty(ref _distractingText, value); }

    public string DurationText { get => _durationText; set => SetProperty(ref _durationText, value); }

    public string IdleText { get => _idleText; set => SetProperty(ref _idleText, value); }

    public bool UseAllowList
    {
        get => _useAllowList;
        set
        {
            if (SetProperty(ref _useAllowList, value))
            {
                OnPropertyChanged(nameof(UseBlockList));
            }
        }
    }

    public bool UseBlockList
    {
        get => !_useAllowList;
        set => UseAllowList = !value;
    }

    public bool BarAlwaysOnTop { get => _barAlwaysOnTop; set => SetProperty(ref _barAlwaysOnTop, value); }

    public string? StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

    public bool StatusIsError { get => _statusIsError; private set => SetProperty(ref _statusIsError, value); }

    private void LoadFrom(FocusSettings settings)
    {
        AllowedText = string.Join(Environment.NewLine, settings.AllowedProcesses);
        DistractingText = string.Join(Environment.NewLine, settings.DistractingProcesses);
        DurationText = settings.DefaultDuration.TotalMinutes.ToString("0", CultureInfo.InvariantCulture);
        IdleText = settings.IdleThreshold.TotalSeconds.ToString("0", CultureInfo.InvariantCulture);
        UseAllowList = settings.Mode == ClassificationMode.AllowList;
        BarAlwaysOnTop = settings.BarAlwaysOnTop;
        Errors.Clear();
        StatusMessage = null;
    }

    private void Save()
    {
        Errors.Clear();

        var (allowed, invalidAllowed) = ProcessNames.ParseList(AllowedText);
        var (distracting, invalidDistracting) = ProcessNames.ParseList(DistractingText);
        foreach (var invalid in invalidAllowed.Concat(invalidDistracting))
        {
            Errors.Add($"\"{invalid}\" ist kein gültiger Programmname (erlaubt: Buchstaben, Ziffern, Punkt, Bindestrich, Unterstrich).");
        }

        if (!int.TryParse(DurationText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var minutes))
        {
            Errors.Add("Die Dauer einer Wacht muss eine ganze Zahl (Minuten) sein.");
        }

        if (!int.TryParse(IdleText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var idleSeconds))
        {
            Errors.Add("Die Abwesenheitsschwelle muss eine ganze Zahl (Sekunden) sein.");
        }

        if (Errors.Count > 0)
        {
            SetStatus("Die Einstellungen wurden nicht gespeichert.", isError: true);
            return;
        }

        var candidate = _current with
        {
            AllowedProcesses = allowed,
            DistractingProcesses = distracting,
            DefaultDuration = TimeSpan.FromMinutes(minutes),
            IdleThreshold = TimeSpan.FromSeconds(idleSeconds),
            Mode = UseAllowList ? ClassificationMode.AllowList : ClassificationMode.BlockList,
            BarAlwaysOnTop = BarAlwaysOnTop,
        };

        var errors = SettingsValidator.Validate(candidate);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Errors.Add(error);
            }

            SetStatus("Die Einstellungen wurden nicht gespeichert.", isError: true);
            return;
        }

        try
        {
            _store.Save(candidate);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Einstellungen", ex);
            SetStatus("Speichern fehlgeschlagen: " + ex.Message, isError: true);
            return;
        }

        _current = candidate;
        LoadFrom(candidate);
        SetStatus("Die Einstellungen wurden in die Schriftrolle übertragen.", isError: false);
        SettingsSaved?.Invoke(this, candidate);
    }

    private void OpenMemeFolder()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.MemeDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppPaths.MemeDirectory,
                UseShellExecute = true,
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            AppLog.Error("Meme-Ordner", ex);
            SetStatus("Der Meme-Ordner konnte nicht geöffnet werden: " + ex.Message, isError: true);
        }

        // Nach dem Befüllen im Explorer zählen die neuen Memes bei der nächsten Ablenkung mit.
        RefreshMemes();
    }

    /// <summary>Kopiert ausgewählte Bilder (geprüft auf Endung und Größe) in den Meme-Ordner.</summary>
    private void AddMemes()
    {
        var files = PickMemeFiles?.Invoke() ?? [];
        if (files.Count == 0)
        {
            return;
        }

        var added = 0;
        var skipped = new List<string>();
        try
        {
            Directory.CreateDirectory(AppPaths.MemeDirectory);
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (!info.Exists || !MemeCatalog.AllowedExtensions.Contains(info.Extension)
                    || info.Length is 0 or > MemeCatalog.MaxFileSizeBytes)
                {
                    skipped.Add(info.Name);
                    continue;
                }

                info.CopyTo(UniqueTarget(info.Name), overwrite: false);
                added++;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Memes hinzufügen", ex);
            SetStatus("Memes konnten nicht kopiert werden: " + ex.Message, isError: true);
            RefreshMemes();
            return;
        }

        RefreshMemes();
        SetStatus(skipped.Count == 0
                ? $"{added} Meme(s) wurden dem Fluss übergeben."
                : $"{added} Meme(s) hinzugefügt, übersprungen (kein Bild oder größer als 10 MB): {string.Join(", ", skipped)}",
            isError: skipped.Count > 0 && added == 0);
    }

    private static string UniqueTarget(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var target = Path.Combine(AppPaths.MemeDirectory, name + extension);
        for (var i = 2; File.Exists(target); i++)
        {
            target = Path.Combine(AppPaths.MemeDirectory, $"{name} ({i}){extension}");
        }

        return target;
    }

    private void RefreshMemes()
    {
        _memes.Reload();
        OnPropertyChanged(nameof(MemeSummary));
    }

    /// <summary>
    /// Speichert eine einzelne Änderung sofort (ohne das Formular anzutasten, damit
    /// ungespeicherte Eingaben dort erhalten bleiben).
    /// </summary>
    private bool ApplyQuickChange(FocusSettings candidate)
    {
        if (SettingsValidator.Validate(candidate).Count > 0)
        {
            return false;
        }

        try
        {
            _store.Save(candidate);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLog.Error("Einstellungen", ex);
            SetStatus("Speichern fehlgeschlagen: " + ex.Message, isError: true);
            return false;
        }

        _current = candidate;
        SettingsSaved?.Invoke(this, candidate);
        return true;
    }

    private void UpdateBookSelection()
    {
        foreach (var option in BookOptions)
        {
            option.IsSelected = option.Book == _current.SayingsBook;
        }

        OnPropertyChanged(nameof(SelectedBook));
        OnPropertyChanged(nameof(SelectedBookOption));
    }

    private void SetStatus(string message, bool isError)
    {
        StatusIsError = isError;
        StatusMessage = message;
    }
}

/// <summary>Ein Eintrag der Buchauswahl (Kontextmenü der Leiste und Einstellungen).</summary>
internal sealed class BookOption : ObservableObject
{
    private bool _isSelected;

    public BookOption(ChronicleBook book, Action select)
    {
        Book = book;
        SelectCommand = new RelayCommand(select);
    }

    public ChronicleBook Book { get; }

    public string Title => ChronicleBooks.Title(Book);

    /// <summary>"Band 2 · Der König von Narnia" bzw. "Alle Chroniken (gemischt)".</summary>
    public string Label => Book == ChronicleBook.All ? Title : $"Band {ChronicleBooks.Volume(Book)} · {Title}";

    public ICommand SelectCommand { get; }

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}
