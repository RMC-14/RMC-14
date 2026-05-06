using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Tutorial;

[RegisterComponent]
public sealed partial class RMCTutorialAnnouncementComponent: Component
{
    // List of components to strip from the owning Entity. (Used to make entity immortable/unaffectable)
    [DataField]
    public ComponentRegistry? RemoveComponents;

    // Factions allowed to trigger Voicelines.
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();

    // Announcement Body Text
    [DataField]
    public string Text = "";

    // Announcement Sender Text (For Marine announcements)
    [DataField]
    public string Sender = "Lt. Urist R. McHands";

    public bool HasTriggered = false;
}
