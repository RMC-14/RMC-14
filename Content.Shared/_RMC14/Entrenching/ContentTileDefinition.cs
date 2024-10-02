// ReSharper disable once CheckNamespace
namespace Content.Shared.Maps;

public sealed partial class ContentTileDefinition
{
    [DataField]
    public bool CanDig;

    [DataField]
    public bool WeedsSpreadable = true;

    [DataField]
    public bool SemiWeedable = false;

    [DataField]
    public bool BlockConstruction;

    [DataField]
    public bool BlockAnchoring;

    [DataField]
    public Color MinimapColor;
}
