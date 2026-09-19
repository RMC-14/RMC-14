using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Tutorial;

[RegisterComponent]
public sealed partial class RMCTutorialSpawnerComponent: Component
{
    // The entity proto you want to spawn when triggered
    [DataField]
    public EntProtoId SpawnPrototype;

    // Amount of the entity to spawn when triggered
    [DataField]
    public int SpawnCount = 1;

    // Offset to apply from spawner position
    [DataField]
    public int SpawnOffsetX = 0;
    [DataField]
    public int SpawnOffsetY = 0;

    [DataField]
    public bool HasTriggered = false;
}
