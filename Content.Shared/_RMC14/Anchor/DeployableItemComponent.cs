using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Anchor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DeployableItemSystem))]
public sealed partial class DeployableItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public DeployableItemPosition Position;

    [DataField, AutoNetworkedField]
    public FixedPoint2 AlmostEmptyThreshold = FixedPoint2.New(0.33);

    [DataField, AutoNetworkedField]
    public FixedPoint2 HalfFullThreshold = FixedPoint2.New(0.66);

    [DataField, AutoNetworkedField]
    public bool LeftClickPickup;
}

[Serializable, NetSerializable]
public enum DeployableItemPosition
{
    None,
    Lower,
    Upper,
}

[Serializable, NetSerializable]
public enum DeployableItemVisuals
{
    Deployed,
}
