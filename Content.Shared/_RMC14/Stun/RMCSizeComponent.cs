using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSizeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public RMCSizes Size = RMCSizes.Xeno;
}


[Serializable, NetSerializable]
public enum RMCSizes : byte
{
    Small,
    Humanoid,
    VerySmallXeno,
    SmallXeno,
    Xeno,
    Big,
    Immobile
}
