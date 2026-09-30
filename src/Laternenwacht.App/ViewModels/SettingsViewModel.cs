using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Laternenwacht.App.Services;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.App.ViewModels;

/// <summary>Bearbeitung der Einstellungen ("Gefährten &amp; Verlockungen") mit Validierung.</summary>
internal sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _store;
    private FocusSettings _current;
    private string _allowedText = string.Empty;
    private string _distractingText = string.Empty;
    private string _durationText = string.Empty;
    private string _idleText = string.Empty;
    private bool _useAllowList;
    private bool _barAlwaysOnTop;
    private string? _statusMessage;
    private bool _statusIsError;

    public SettingsViewModel(SettingsStore store, FocusSettings current)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _current = current ?? throw new ArgumentNullException(nameof(current));
        LoadFrom(current);

        if (store.LastLoadWarning is { } warning)
        {
            SetStatus(warning + " Es gelten die Standardwerte.", isError: true);
        }

        SaveCommand = new RelayCommand(Save);
        ResetCommand = new RelayCommand(() => LoadFrom(_current));
        DefaultsCommand = new RelayCommand(() => LoadFrom(FocusSettings.Default));
    }

    /// <summary>Neue, gültige Einstellungen wurden gespeichert.</summary>
    public event EventHandler<FocusSettings>? SettingsSaved;

    public FocusSettings Current => _current;

    public ICommand SaveCommand { get; }

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

    private void SetStatus(string message, bool isError)
    {
        StatusIsError = isError;
        StatusMessage = message;
    }
}
