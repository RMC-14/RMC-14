using Content.Shared._RMC14.Line;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCFlamerChainComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCTileFire";

    [DataField, AutoNetworkedField]
    public List<LineTile> Tiles = new();

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "RMCNapalmUT";

    [DataField, AutoNetworkedField]
    public int MaxIntensity = 20;

    [DataField, AutoNetworkedField]
    public int MaxDuration = 24;
}
