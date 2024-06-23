using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoGrowOvipositorActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AttachCooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan DetachCooldown = TimeSpan.FromMinutes(5);
}
