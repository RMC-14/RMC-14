using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Emote;

[RegisterComponent]
[Access(typeof(RMCEmoteSystem))]
public sealed partial class RecentlyEmotedComponent : Component
{
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.8);

    [DataField]
    public Dictionary<ProtoId<EmotePrototype>, TimeSpan> Emotes = new();
}
