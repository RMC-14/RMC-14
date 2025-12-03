using Content.Shared.Chat.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids.Emote;

/// <summary>
/// Triggers an emote on valid hit entities when this projectile hits it. Chance is determined by damage dealt.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoEmoteSystem))]
public sealed partial class CauseEmoteOnProjectileHitComponent : Component
{
    /// <summary>
    /// The chance for the emote to be the non-rare one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EmoteChance = 0.7f;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "Scream";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> RareEmote = "Growl";

    /// <summary>
    /// Entity whitelist for valid targets to emote. Not required.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Entity blacklist for targets that shouldn't emote. Not required.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;
}
