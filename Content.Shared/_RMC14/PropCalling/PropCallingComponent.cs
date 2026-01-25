using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PropCalling;
/// <summary>
/// Makes ghosts able to sign up for being called over to entities with the <see cref="PropCallerComponent"/>
/// </summary>
[RegisterComponent, AutoGenerateComponentState, Access(typeof(SharedPropCallingSystem))]
public sealed partial class PropCallingComponent : Component
{
    [DataField]
    public EntProtoId TogglePropCalling = "ActionTogglePropCalling";

    [DataField, AutoNetworkedField]
    public EntityUid? TogglePropCallingEntity;
}
