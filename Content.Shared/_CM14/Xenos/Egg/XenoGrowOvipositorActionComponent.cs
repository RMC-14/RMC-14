using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoGrowOvipositorActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AttachCooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan DetachCooldown = TimeSpan.FromMinutes(5);
}
