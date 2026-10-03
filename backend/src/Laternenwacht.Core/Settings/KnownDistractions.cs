using System.Collections.Frozen;

namespace Laternenwacht.Core.Settings;

/// <summary>
/// Eingebauter Katalog bekannter Verlockungen (Spiele, Launcher, Messenger, Streaming).
/// Er greift zusätzlich zur eigenen Liste, solange <see cref="FocusSettings.UseKnownDistractions"/>
/// aktiv ist. Ein Eintrag bei den "Gefährten" hat immer Vorrang.
/// Die Namen sind Prozessnamen ohne ".exe" in Kleinschreibung.
/// </summary>
public static class KnownDistractions
{
    public static FrozenSet<string> Names { get; } = new[]
    {
        // Spiele
        "hearthstone", "hearthstonedecktracker", "wow", "wowclassic", "overwatch", "diablo iv", "diablo immortal",
        "league of legends", "leagueclient", "leagueclientux", "valorant-win64-shipping", "riotclientservices",
        "fortniteclient-win64-shipping", "robloxplayerbeta", "minecraft.windows", "minecraftlauncher",
        "cs2", "csgo", "dota2", "genshinimpact", "gta5", "rocketleague", "eldenring", "r5apex", "pubg",
        "tslgame", "fifa23", "fc24", "fc25", "rainbowsix", "destiny2", "warframe.x64", "pathofexile",
        "starrail", "zenlesszonezero", "osu!", "terraria", "stardew valley",
        // Launcher und Shops
        "battle.net", "steam", "steamwebhelper", "epicgameslauncher", "eadesktop", "origin", "ubisoftconnect",
        "upc", "galaxyclient", "xboxpcapp", "gamebar", "riotclientux",
        // Messenger, soziale Netzwerke, Streaming
        "discord", "spotify", "whatsapp", "whatsapp.root", "telegram", "netflix", "primevideo", "disneyplus",
        "twitch", "instagram", "tiktok",
    }.ToFrozenSet(StringComparer.Ordinal);
}
