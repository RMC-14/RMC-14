using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Tutorial;

[RegisterComponent]
public sealed partial class RMCTutorialDummyComponent: Component
{
    // List of components to strip from the owning Entity. (Used to make entity immortable/unaffectable)
    [DataField]
    public ComponentRegistry? RemoveComponents;

    // Factions allowed to trigger Voicelines.
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
}
