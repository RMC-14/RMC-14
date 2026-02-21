using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunMuzzleOffsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 Offset = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public bool UseDirectionalOffsets = false;

    [DataField, AutoNetworkedField]
    public bool RotateDirectionalOffsets = false;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetNorth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetEast = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetSouth = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 OffsetWest = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Vector2 MuzzleOffset = Vector2.Zero;

    [DataField, AutoNetworkedField]
    public Angle AngleOffset = Angle.Zero;

    [DataField, AutoNetworkedField]
    public bool UseContainerOwner = true;

    [DataField, AutoNetworkedField]
    public bool UseAimDirection = false;
}
