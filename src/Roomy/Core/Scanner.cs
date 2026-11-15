using System.Diagnostics;

namespace Roomy.Core;

public sealed class ScanContext
{
    public bool IsAdmin { get; init; }
    public CancellationToken Token { get; init; }

    readonly Lazy<List<FileInfo>> _downloads = new(LoadDownloads);
    public List<FileInfo> DownloadFiles => _downloads.Value;

    static List<FileInfo> LoadDownloads()
    {
        try
        {
            var d = new DirectoryInfo(Env.Downloads);
            if (!d.Exists) return [];
            return d.EnumerateFiles("*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System,
            }).ToList();
        }
        catch { return []; }
    }
}

public sealed record ScanStep(string Id, string TitleKey, Func<ScanContext, CleanupCategory?> Run)
{
    public string Title => Loc.T(TitleKey);
}

/// <summary>Each step looks in a few well-known places and reports what it found. Nothing here deletes.</summary>
public static class Scanner
{
    static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    static readonly HashSet<string> InstallerExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".msi", ".msix", ".msixbundle", ".appx", ".appxbundle",
    };

    public static readonly IReadOnlyList<ScanStep> Steps =
    [
        new("recycle", "StepRecycle", RecycleBin),
        new("temp", "StepTemp", Temp),
        new("browser", "StepBrowser", Browsers),
        new("apps", "StepApps", Apps),
        new("shaders", "StepShaders", Shaders),
        new("crash", "StepCrash", Crashes),
        new("windows", "StepWindows", WindowsLeftovers),
        new("installers", "StepInstallers", Installers),
        new("downloads", "StepDownloads", BigDownloads),
        new("games", "StepGames", Games),
        new("winold", "StepWindowsOld", WindowsOld),
    ];

    // ---------- helpers ----------

    static CleanupItem? Item(string name, TimeSpan? minAge, CancellationToken ct, IEnumerable<string> paths)
    {
        var existing = paths.Where(FileOps.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (existing.Count == 0) return null;
        long size = 0;
        int files = 0;
        foreach (var p in existing)
        {
            var (b, f) = FileOps.Measure(p, minAge, ct);
            size += b;
            files += f;
        }
        if (size <= 0) return null;
        return new CleanupItem
        {
            Name = name, Paths = existing, Size = size, FileCount = files, MinAge = minAge,
            Detail = Loc.T("DetailFiles", files.ToString("N0"), Loc.N(files, "FileOne", "FileFew", "FileMany")),
        };
    }

    static void Add(CleanupCategory c, CleanupItem? i)
    {
        if (i != null) c.Items.Add(i);
    }

    static CleanupCategory Sorted(CleanupCategory c)
    {
        c.Items.Sort((a, b) => b.Size.CompareTo(a.Size));
        return c;
    }

    static string[] Subdirs(string dir)
    {
        try { return Directory.Exists(dir) ? Directory.GetDirectories(dir) : []; }
        catch { return []; }
    }

    static List<string> Running(params (string Proc, string Name)[] apps)
    {
        var list = new List<string>();
        foreach (var (proc, name) in apps)
        {
            try
            {
                var ps = Process.GetProcessesByName(proc);
                if (ps.Length > 0) list.Add(name);
                foreach (var p in ps) p.Dispose();
            }
            catch { }
        }
        return list;
    }

    static string? RunningNote(List<string> names)
    {
        if (names.Count == 0) return null;
        return Loc.T("NoteAppsOpen", string.Join(", ", names));
    }

    static DateTime Touched(FileInfo f) => f.CreationTime > f.LastWriteTime ? f.CreationTime : f.LastWriteTime;

    // ---------- steps ----------

    static CleanupCategory RecycleBin(ScanContext c)
    {
        var cat = Catalog.Create("recycle");
        var (size, count) = Native.QueryRecycleBin();
        if (size > 0)
            cat.Items.Add(new CleanupItem
            {
                Name = cat.Title, Paths = ["shell:RecycleBinFolder"], Size = size,
                Detail = Loc.T("DetailItems", count.ToString("N0"), Loc.N(count, "ItemOne", "ItemFew", "ItemMany")),
            });
        return cat;
    }

    static CleanupCategory? Temp(ScanContext c)
    {
        // %TEMP% can be redirected anywhere by TMP/TEMP. Only touch folders that actually look
        // like a temp location, otherwise a stray environment variable could cost real data.
        if (!FileOps.IsTempRoot(Env.Temp))
        {
            System.Diagnostics.Trace.WriteLine($"Roomy: skipping unusual temp folder '{Env.Temp}'.");
            return null;
        }
        var cat = Catalog.Create("temp");
        Add(cat, Item(Loc.T("ItemYourTemp"), OneDay, c.Token, [Env.Temp]));
        return cat;
    }

    static readonly (string Name, string UserData)[] Chromium =
    [
        ("Google Chrome", Env.L("Google", "Chrome", "User Data")),
        ("Microsoft Edge", Env.L("Microsoft", "Edge", "User Data")),
        ("Brave", Env.L("BraveSoftware", "Brave-Browser", "User Data")),
        ("Vivaldi", Env.L("Vivaldi", "User Data")),
        ("Chromium", Env.L("Chromium", "User Data")),
    ];

    static IEnumerable<string> ChromiumCaches(string userData)
    {
        if (!Directory.Exists(userData)) yield break;
        yield return Path.Combine(userData, "ShaderCache");
        yield return Path.Combine(userData, "GrShaderCache");
        yield return Path.Combine(userData, "GraphiteDawnCache");
        foreach (var dir in Subdirs(userData))
        {
            var n = Path.GetFileName(dir);
            if (n == "Default" || n == "Guest Profile" || n.StartsWith("Profile ", StringComparison.Ordinal))
            {
                yield return Path.Combine(dir, "Cache");
                yield return Path.Combine(dir, "Code Cache");
                yield return Path.Combine(dir, "GPUCache");
            }
        }
    }

    static CleanupCategory Browsers(ScanContext c)
    {
        var cat = Catalog.Create("browser");
        foreach (var (name, ud) in Chromium) Add(cat, Item(name, null, c.Token, ChromiumCaches(ud)));
        Add(cat, Item("Firefox", null, c.Token,
            Subdirs(Env.L("Mozilla", "Firefox", "Profiles")).Select(p => Path.Combine(p, "cache2"))));
        Add(cat, Item("Opera", null, c.Token,
            [Env.L("Opera Software", "Opera Stable", "Cache"), Env.L("Opera Software", "Opera GX Stable", "Cache")]));
        if (cat.Items.Count > 0)
            cat.Note = RunningNote(Running(("chrome", "Chrome"), ("msedge", "Edge"), ("firefox", "Firefox"),
                ("brave", "Brave"), ("opera", "Opera"), ("vivaldi", "Vivaldi")));
        return Sorted(cat);
    }

    static CleanupCategory Apps(ScanContext c)
    {
        var cat = Catalog.Create("apps");
        string[] bits = ["Cache", "Code Cache", "GPUCache"];
        IEnumerable<string> Bits(string root) => bits.Select(b => Path.Combine(root, b));

        Add(cat, Item("Discord", null, c.Token,
            new[] { "discord", "discordptb", "discordcanary" }.SelectMany(d => Bits(Env.R(d)))));
        Add(cat, Item("Slack", null, c.Token, Bits(Env.R("Slack"))));
        Add(cat, Item(Loc.T("ItemTeamsClassic"), null, c.Token, Bits(Env.R("Microsoft", "Teams"))));
        Add(cat, Item(Loc.T("ItemVSCode"), null, c.Token,
            Bits(Env.R("Code")).Append(Env.R("Code", "CachedData")).Append(Env.R("Code", "CachedExtensionVSIXs"))));
        Add(cat, Item(Loc.T("ItemSteamWeb"), null, c.Token, [Env.L("Steam", "htmlcache")]));
        Add(cat, Item("npm cache", null, c.Token, [Env.L("npm-cache", "_cacache")]));
        Add(cat, Item("pip cache", null, c.Token, [Env.L("pip", "Cache")]));
        Add(cat, Item("Yarn cache", null, c.Token, [Env.L("Yarn", "Cache")]));
        Add(cat, Item("NuGet web cache", null, c.Token, [Env.L("NuGet", "v3-cache")]));
        if (cat.Items.Count > 0)
            cat.Note = RunningNote(Running(("Discord", "Discord"), ("slack", "Slack"), ("Code", "VS Code"), ("Teams", "Teams")));
        return Sorted(cat);
    }

    static CleanupCategory Shaders(ScanContext c)
    {
        var cat = Catalog.Create("shaders");
        Add(cat, Item("DirectX", null, c.Token, [Env.L("D3DSCache")]));
        Add(cat, Item("NVIDIA", null, c.Token,
            [Env.L("NVIDIA", "DXCache"), Env.L("NVIDIA", "GLCache"), Env.L("NVIDIA Corporation", "NV_Cache"), Env.PD("NVIDIA Corporation", "NV_Cache")]));
        Add(cat, Item("AMD", null, c.Token,
            [Env.L("AMD", "DxCache"), Env.L("AMD", "DxcCache"), Env.L("AMD", "GLCache"), Env.L("AMD", "VkCache")]));
        Add(cat, Item("Intel", null, c.Token, [Env.L("Intel", "ShaderCache")]));
        Add(cat, Item(Loc.T("ItemSteamGames"), null, c.Token,
            Steam.Libraries().Select(l => Path.Combine(l, "steamapps", "shadercache"))));
        return Sorted(cat);
    }

    static CleanupCategory Crashes(ScanContext c)
    {
        var cat = Catalog.Create("crash");
        Add(cat, Item(Loc.T("ItemAppCrashDumps"), null, c.Token, [Env.L("CrashDumps")]));
        Add(cat, Item(Loc.T("ItemWindowsErrorReports"), null, c.Token, [Env.L("Microsoft", "Windows", "WER")]));
        return Sorted(cat);
    }

    /// <summary>True while Windows is actively installing updates: the update cache is in use then.</summary>
    static bool Updating()
    {
        foreach (var name in new[] { "TiWorker", "TrustedInstaller", "wuauclt", "USOClient", "MoUsoCoreWorker" })
        {
            try
            {
                var running = Process.GetProcessesByName(name);
                foreach (var p in running) p.Dispose();
                if (running.Length > 0) return true;
            }
            catch { }
        }
        return false;
    }

    static CleanupCategory WindowsLeftovers(ScanContext c)
    {
        var cat = Catalog.Create("windows");
        if (!c.IsAdmin)
        {
            cat.Blocked = true;
            cat.Note = Loc.T("NoteWindowsLocked");
            return cat;
        }
        if (Updating())
            cat.Note = Loc.T("NoteUpdating");
        else
            Add(cat, Item(Loc.T("ItemDownloadedUpdates"), null, c.Token, [Env.W("SoftwareDistribution", "Download")]));
        Add(cat, Item(Loc.T("ItemDeliveryOptimization"), null, c.Token,
            [Env.W("ServiceProfiles", "NetworkService", "AppData", "Local", "Microsoft", "Windows", "DeliveryOptimization", "Cache")]));
        Add(cat, Item(Loc.T("ItemWindowsTemp"), OneDay, c.Token, [Env.W("Temp")]));
        Add(cat, Item(Loc.T("ItemSystemDumps"), null, c.Token,
            [Env.W("Minidump"), Env.W("MEMORY.DMP"), Env.PD("Microsoft", "Windows", "WER", "ReportArchive"), Env.PD("Microsoft", "Windows", "WER", "ReportQueue")]));
        return Sorted(cat);
    }

    static CleanupCategory Installers(ScanContext c)
    {
        var cat = Catalog.Create("installers");
        var cutoff = DateTime.Now.AddDays(-7);
        foreach (var f in c.DownloadFiles)
        {
            try
            {
                if (!InstallerExt.Contains(f.Extension)) continue;
                // Only loose files in Downloads itself. .exe files inside extracted folders are usually
                // portable apps, not installers.
                if (!string.Equals(f.DirectoryName?.TrimEnd('\\'), Env.Downloads.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) continue;
                var t = Touched(f);
                if (t > cutoff) continue;
                cat.Items.Add(new CleanupItem
                {
                    Name = f.Name, Paths = [f.FullName], Size = f.Length, FileCount = 1, IsFile = true,
                    Detail = Loc.T("DetailDownloaded", Format.Ago(t)),
                });
            }
            catch { }
        }
        return Sorted(cat);
    }

    static CleanupCategory BigDownloads(ScanContext c)
    {
        var cat = Catalog.Create("downloads");
        const long minSize = 250L * 1024 * 1024;
        var cutoff = DateTime.Now.AddDays(-30);
        foreach (var f in c.DownloadFiles)
        {
            try
            {
                if (f.Length < minSize || InstallerExt.Contains(f.Extension)) continue;
                var t = Touched(f);
                if (f.LastAccessTime > t) t = f.LastAccessTime;
                if (t > cutoff) continue;
                cat.Items.Add(new CleanupItem
                {
                    Name = f.Name, Paths = [f.FullName], Size = f.Length, FileCount = 1, IsFile = true,
                    Detail = Loc.T("DetailUntouched", Format.AgoShort(t)),
                });
            }
            catch { }
        }
        return Sorted(cat);
    }

    static CleanupCategory Games(ScanContext c)
    {
        var cat = Catalog.Create("games");
        var cutoff = DateTime.Now.AddDays(-60);
        foreach (var g in Steam.Games())
        {
            if (g.Size < 1L << 30) continue;
            if (g.LastPlayed is { } lp && lp > cutoff) continue;
            cat.Items.Add(new CleanupItem
            {
                Name = g.Name, Paths = [g.InstallPath], Size = g.Size, AppId = g.AppId,
                Detail = g.LastPlayed is { } p ? Loc.T("DetailLastPlayed", Format.Ago(p)) : Loc.T("DetailNeverPlayed"),
            });
        }
        return Sorted(cat);
    }

    static CleanupCategory? WindowsOld(ScanContext c)
    {
        var p = Path.Combine(Env.SystemRoot, "Windows.old");
        if (!Directory.Exists(p)) return null;
        var (b, _) = FileOps.Measure(p, null, c.Token);
        var cat = Catalog.Create("winold");
        cat.Items.Add(new CleanupItem { Name = "Windows.old", Paths = [p], Size = b });
        return cat;
    }
}
