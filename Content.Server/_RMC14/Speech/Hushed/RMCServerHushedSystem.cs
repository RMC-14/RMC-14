using Content.Server.Popups;
using Content.Server._RMC14.Chat.Events;
using Content.Server.Speech.EntitySystems;
using Content.Shared._RMC14.Speech.Hushed;
using Content.Shared.Chat.Prototypes;
using Content.Server.Chat.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Speech.Hushed;

/// <summary>
/// Server-side system that handles chat type modification for hushed entities.
/// When trying to speak (Say), it will be converted to Whisper and a popup will be shown.
/// Also blocks vocal emotes except Scream and Cough.
/// </summary>
public sealed class RMCServerHushedSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";
    private static readonly ProtoId<EmotePrototype> CoughEmote = "Cough";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCHushedComponent, RMCChatTypeModifyEvent>(OnChatTypeModify);
        SubscribeLocalEvent<RMCHushedComponent, EmoteEvent>(OnEmote, before: new[] { typeof(VocalSystem), typeof(MumbleAccentSystem) });
    }

    private void OnChatTypeModify(Entity<RMCHushedComponent> ent, ref RMCChatTypeModifyEvent args)
    {
        // Only convert Speak to Whisper, leave other types unchanged
        if (args.DesiredType != InGameICChatType.Speak)
            return;

        args.ModifiedType = InGameICChatType.Whisper;
        _popupSystem.PopupEntity(Loc.GetString("rmc-hushed-can-only-whisper"), ent, ent);
    }

    private void OnEmote(Entity<RMCHushedComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        //still leaves the text so it looks like they are pantomiming a laugh
        if (!args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        if (args.Emote.ID == ScreamEmote || args.Emote.ID == CoughEmote)
            return;

        args.Handled = true;
    }
}
