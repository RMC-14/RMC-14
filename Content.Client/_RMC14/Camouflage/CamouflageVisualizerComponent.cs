using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Content.Shared._RMC14.Item;

namespace Content.Client._RMC14.Camouflage;

[RegisterComponent]
public sealed partial class CamouflageVisualizerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<CamouflageType, SpriteSpecifier.Rsi> CamouflageVariations = new();
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi Replace = new();
}
