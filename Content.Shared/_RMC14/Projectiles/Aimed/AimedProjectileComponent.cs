using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles.Aimed;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AimedProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    [DataField, AutoNetworkedField]
    public EntityUid Source;
}
