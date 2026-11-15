namespace Roomy.Core;

public static class Format
{
    static string[] Units => Loc.IsRu
        ? ["Б", "КБ", "МБ", "ГБ", "ТБ"]
        : ["B", "KB", "MB", "GB", "TB"];

    public static string Bytes(long bytes)
    {
        var units = Units;
        if (bytes <= 0) return $"0 {units[0]}";
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        string n = i == 0 || v >= 100 ? v.ToString("0") : v >= 10 ? v.ToString("0.#") : v.ToString("0.##");
        return $"{n} {units[i]}";
    }

    /// <summary>"today", "3 days ago", "2 weeks ago" … in the active UI language.</summary>
    public static string Ago(DateTime when)
    {
        var d = DateTime.Now - when;
        if (d.TotalDays < 1) return Loc.T("AgoToday");
        if (d.TotalDays < 2) return Loc.T("AgoYesterday");
        if (d.TotalDays < 14) return Loc.T("AgoDays", (int)d.TotalDays);
        if (d.TotalDays < 60) return Loc.T("AgoWeeks", (int)(d.TotalDays / 7));
        if (d.TotalDays < 365) return Loc.T("AgoMonths", (int)(d.TotalDays / 30));
        var y = (int)(d.TotalDays / 365);
        return y == 1 ? Loc.T("AgoYear") : Loc.T("AgoYears", y);
    }

    /// <summary>The same span without "ago", for sentences that already carry the preposition.</summary>
    public static string AgoShort(DateTime when)
    {
        var d = DateTime.Now - when;
        if (d.TotalDays < 1) return Loc.T("AgoShortToday");
        if (d.TotalDays < 14) return Loc.T("AgoShortDays", (int)d.TotalDays);
        if (d.TotalDays < 60) return Loc.T("AgoShortWeeks", (int)(d.TotalDays / 7));
        if (d.TotalDays < 365) return Loc.T("AgoShortMonths", (int)(d.TotalDays / 30));
        var y = (int)(d.TotalDays / 365);
        return y == 1 ? Loc.T("AgoShortYear") : Loc.T("AgoShortYears", y);
    }
}
