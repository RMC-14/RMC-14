using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Tantrum;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TantrumingComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ArmorGain = 15;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireAt;
}
