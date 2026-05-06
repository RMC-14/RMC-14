using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Tutorial;

[RegisterComponent]
public sealed partial class RMCTutorialNPCComponent: Component
{
    // List of components to strip from the owning Entity. (Used to make entity immortable/unaffectable)
    [DataField]
    public ComponentRegistry? RemoveComponents;

    // Factions allowed to trigger Voicelines.
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();

    // List of messages for the NPC to state
    [DataField]
    public List<string> Voicelines = [];

    // Current voiceline index from Voicelines
    public int LineIndex = -1;

    // Set the seconds delay between the next voiceline, set to 0 to require manually retriggering.
    [DataField]
    public int LineDelay = 3;

    // Should the NPC automatically move onto the next voiceline as long as the player is near.
    [DataField]
    public bool AutoLine = true;

    public TimeSpan TimeSinceLastLine = TimeSpan.Zero;

    // Set the seconds delay before allowing the voicelines to be repeated, set to 0 to disable repeats.
    [DataField]
    public int ResetDelay = 10;

    public TimeSpan TimeSinceEnd = TimeSpan.Zero;
}
