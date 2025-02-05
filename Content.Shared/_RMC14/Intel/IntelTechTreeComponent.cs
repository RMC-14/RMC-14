using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(IntelSystem), typeof(TechSystem))]
public sealed partial class IntelTechTreeComponent : Component
{
    [DataField, AutoNetworkedField]
    public IntelTechTree Tree = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 ColonyCommunicationsPoints = FixedPoint2.New(0.7);
}
