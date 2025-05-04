using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Hands;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCHandsSystem))]
public sealed partial class RMCStorageEjectHandComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCStorageEjectState State = RMCStorageEjectState.Unequip;

    [DataField, AutoNetworkedField]
    public bool CanToggleStorage = true;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}

[Serializable, NetSerializable]
public enum RMCStorageEjectState : byte
{
    Last,
    First,
    Unequip,
    Open
}
