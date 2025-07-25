using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PropCalling;
/// <summary>
/// Makes you able to call over entities with the <see cref="PropCallingComponent"/>. 
/// </summary>
[RegisterComponent, AutoGenerateComponentState, Access(typeof(SharedPropCallingSystem))]
public sealed partial class PropCallerComponent : Component
{
    [DataField]
    public EntProtoId CallProps = "ActionCallProps";

    [DataField, AutoNetworkedField]
    public EntityUid? CallPropsEntity;
}
