// ReSharper disable once CheckNamespace
namespace Content.Shared.Maps;

public sealed partial class ContentTileDefinition
{
    [DataField]
    public bool CanDig;

    [DataField]
    public bool WeedsSpreadable = true;
}
