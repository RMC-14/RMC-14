using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParasiteDropCooldownComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextDropTime;
}
