using System.Text.Json.Serialization;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.Core.Integrity;

/// <summary>
/// Quellgenerierte JSON-Serialisierung: keine Reflection, keine polymorphe Typauflösung,
/// unbekannte Felder werden abgelehnt. Das verkleinert die Angriffsfläche beim Einlesen.
/// </summary>
[JsonSourceGenerationOptions(
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(SessionRecord))]
[JsonSerializable(typeof(JournalEntry))]
[JsonSerializable(typeof(JournalAnchor))]
internal sealed partial class CoreJsonContext : JsonSerializerContext;

/// <summary>Wie <see cref="CoreJsonContext"/>, jedoch eingerückt für die von Menschen lesbare Einstellungsdatei.</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(FocusSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
