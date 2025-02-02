using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class CraftsIntoMolotovComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SolutionId = "drink";

    [DataField, AutoNetworkedField]
    public FixedPoint2 MinIntensity = 10;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxIntensity = 40;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public EntProtoId Spawns = "RMCGrenadeMolotov";
}
