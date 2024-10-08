using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class DropshipTerminalWeaponsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Screen ScreenOne;

    [DataField, AutoNetworkedField]
    public Screen ScreenTwo;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public Vector2i Offset;

    [DataField, AutoNetworkedField]
    public Vector2i OffsetLimit = new(12, 12);

    [DataField, AutoNetworkedField]
    public List<TargetEnt> Targets = new();

    [DataField, AutoNetworkedField]
    public int TargetsPage;

    [DataRecord]
    [Serializable, NetSerializable]
    public record struct Screen(
        DropshipTerminalWeaponsScreen State,
        NetEntity? Weapon
    );

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct TargetEnt(
        NetEntity Id,
        string Name
    );
}
