using System.Windows.Media;
using Roomy.Core;

namespace Roomy.ViewModels;

public sealed class DriveVM : ObservableObject
{
    public DriveVM(string root, string label, long total, long free)
    {
        Root = root;
        Label = label;
        Update(total, free);
    }

    public string Root { get; }
    public string Label { get; }
    public string Letter => Root.TrimEnd('\\');
    /// <summary>Just "C", for sentences that already say "drive"/"диск".</summary>
    public string LetterName => Letter.TrimEnd(':');
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? Loc.T("DriveLocalDisk") : Label;
    public string Title => $"{DisplayName} ({Letter})";

    public long Total { get; private set; }
    public long Free { get; private set; }
    public long Used => Math.Max(0, Total - Free);
    public double UsedFraction => Total > 0 ? (double)Used / Total : 0;

    public string Subtitle => Loc.T("DriveSubtitle", Format.Bytes(Total));
    public string FreeNumber => Format.Bytes(Free);
    public string FreeRest => Loc.T("DriveFreeRest", Format.Bytes(Total));
    public string FreeShort => Loc.T("DriveFreeShort", Format.Bytes(Free));
    public string UsedText => Loc.T("DriveUsed", Format.Bytes(Used));
    public string PercentText => Loc.T("DrivePercentFull", (UsedFraction * 100).ToString("0"));
    public string StatusText => UsedFraction >= .9 ? Loc.T("DriveAlmostFull")
        : UsedFraction >= .75 ? Loc.T("DriveGettingFull")
        : Loc.T("DriveLookingGood");

    string Tone => UsedFraction >= .9 ? "Danger" : UsedFraction >= .75 ? "Warn" : "Accent";
    public Brush StatusBrush => Res.Brush(Tone + "Brush");
    public Brush StatusBg => Res.Brush(Tone + "SoftBrush");
    public Brush BarBrush => Res.Brush(Tone + "Gradient");

    bool _spaceSelected;
    public bool IsSpaceSelected { get => _spaceSelected; set => Set(ref _spaceSelected, value); }

    public void Update(long total, long free)
    {
        Total = total;
        Free = free;
        RaiseAll();
    }
}
