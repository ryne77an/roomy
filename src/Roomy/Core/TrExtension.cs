using System.Windows.Markup;

namespace Roomy.Core;

/// <summary>XAML access: <c>Text="{loc:Tr HomeTitle}"</c>.</summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension : MarkupExtension
{
    public TrExtension() { }
    public TrExtension(string key) => Key = key;

    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);
}
