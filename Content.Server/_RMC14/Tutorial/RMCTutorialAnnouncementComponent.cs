using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Tutorial;

[RegisterComponent]
public sealed partial class RMCTutorialAnnouncementComponent: Component
{
    // Announcement Body Text
    [DataField]
    public string Text = "";

    // Announcement Sender Text (For Marine announcements)
    [DataField]
    public string Sender = "Lt. Urist R. McHands";

    public bool HasTriggered = false;
}
