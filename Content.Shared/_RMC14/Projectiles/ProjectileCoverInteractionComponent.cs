using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectileCoverInteractionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IgnoreCover;

    [DataField, AutoNetworkedField]
    public bool StoppedByCover;
}
