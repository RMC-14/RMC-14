using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Entrenching;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(BarricadeSystem))]
public sealed partial class FullSandbagComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan BuildDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public int StackRequired = 5;

    [DataField, AutoNetworkedField]
    public EntProtoId Builds = "CMBarricadeSandbag";
}
