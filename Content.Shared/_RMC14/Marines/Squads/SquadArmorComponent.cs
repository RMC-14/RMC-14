using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem))]
public sealed partial class SquadArmorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SquadArmorLayers Layer;

    [DataField(required: true), AutoNetworkedField]
    public SlotFlags Slot;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi Rsi;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi LeaderRsi;
}

[Serializable, NetSerializable]
public enum SquadArmorLayers
{
    Helmet,
    Goggles,
    Armor,
    Gloves,
}
