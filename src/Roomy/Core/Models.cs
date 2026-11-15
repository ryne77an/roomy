namespace Roomy.Core;

public enum Safety { Safe, Review }

public enum CleanAction
{
    /// <summary>Delete files (for folders: delete the contents, keep the folder).</summary>
    Delete,
    EmptyRecycleBin,
    /// <summary>Hand off to Steam's own uninstaller.</summary>
    SteamUninstall,
    /// <summary>Only Windows can remove it safely, so we open Settings.</summary>
    OpenSettings,
}

public sealed class CleanupItem
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Paths { get; init; }
    public string Path => Paths.Count > 0 ? Paths[0] : "";
    public long Size { get; set; }
    public int FileCount { get; set; }
    public bool IsFile { get; init; }
    public string? Detail { get; init; }
    /// <summary>Only files older than this are measured and deleted.</summary>
    public TimeSpan? MinAge { get; init; }
    public string? AppId { get; init; }
}

public sealed class CleanupCategory
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Glyph { get; init; }
    public Safety Safety { get; init; } = Safety.Safe;
    public CleanAction Action { get; init; } = CleanAction.Delete;
    public bool SelectedByDefault { get; init; }
    public bool ShowItems { get; init; } = true;
    public bool Blocked { get; set; }
    public string? Note { get; set; }
    public List<CleanupItem> Items { get; } = [];
    public long Size => Items.Sum(i => i.Size);
}

/// <summary>Segoe Fluent Icons / MDL2 code points.</summary>
public static class G
{
    public const string Delete = "";
    public const string Clock = "";
    public const string Globe = "";
    public const string Apps = "";
    public const string Color = "";
    public const string Bug = "";
    public const string Update = "";
    public const string Package = "";
    public const string Download = "";
    public const string Game = "";
    public const string History = "";
    public const string Folder = "";
    public const string Document = "";
    public const string Shield = "";
    public const string Check = "";
    public const string ChevronDown = "";
    public const string ChevronUp = "";
    public const string ChevronRight = "";
}

/// <summary>The fixed copy for every category, shared by the real scanner and demo mode.</summary>
public static class Catalog
{
    public static CleanupCategory Create(string id) => id switch
    {
        "recycle" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatRecycleTitle"), Glyph = G.Delete,
            Description = Loc.T("CatRecycleBody"),
            Action = CleanAction.EmptyRecycleBin, SelectedByDefault = true, ShowItems = false,
        },
        "temp" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatTempTitle"), Glyph = G.Clock,
            Description = Loc.T("CatTempBody"),
            SelectedByDefault = true,
        },
        "browser" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatBrowserTitle"), Glyph = G.Globe,
            Description = Loc.T("CatBrowserBody"),
            SelectedByDefault = true,
        },
        "apps" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatAppsTitle"), Glyph = G.Apps,
            Description = Loc.T("CatAppsBody"),
            SelectedByDefault = true,
        },
        "shaders" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatShadersTitle"), Glyph = G.Color,
            Description = Loc.T("CatShadersBody"),
            SelectedByDefault = false,
        },
        "crash" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatCrashTitle"), Glyph = G.Bug,
            Description = Loc.T("CatCrashBody"),
            SelectedByDefault = true,
        },
        "windows" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatWindowsTitle"), Glyph = G.Update,
            Description = Loc.T("CatWindowsBody"),
            SelectedByDefault = true,
        },
        "installers" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatInstallersTitle"), Glyph = G.Package, Safety = Safety.Review,
            Description = Loc.T("CatInstallersBody"),
        },
        "downloads" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatDownloadsTitle"), Glyph = G.Download, Safety = Safety.Review,
            Description = Loc.T("CatDownloadsBody"),
        },
        "games" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatGamesTitle"), Glyph = G.Game, Safety = Safety.Review,
            Description = Loc.T("CatGamesBody"),
            Action = CleanAction.SteamUninstall,
        },
        "winold" => new CleanupCategory
        {
            Id = id, Title = Loc.T("CatWindowsOldTitle"), Glyph = G.History, Safety = Safety.Review,
            Description = Loc.T("CatWindowsOldBody"),
            Action = CleanAction.OpenSettings, ShowItems = false,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown category"),
    };
}
