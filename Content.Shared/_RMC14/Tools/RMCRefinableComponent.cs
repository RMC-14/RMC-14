using Content.Shared.Storage;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCToolSystem))]
public sealed partial class RMCRefinableComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Amount = 4;

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> Tool = "Welding";

    [DataField, AutoNetworkedField]
    public TimeSpan Delay;

    [DataField, AutoNetworkedField]
    public float Fuel;

    [DataField(required: true)]
    public List<EntitySpawnEntry> Spawn = new();
}
