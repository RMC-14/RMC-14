using Content.Server.Popups;
using Content.Server._RMC14.Chat.Events;
using Content.Shared._RMC14.Speech.Hushed;
using Content.Server.Chat.Systems;

namespace Content.Server._RMC14.Speech.Hushed;

/// <summary>
/// Server-side system that handles chat type modification for hushed entities.
/// When trying to speak (Say), it will be converted to Whisper and a popup will be shown.
/// </summary>
public sealed class RMCServerHushedSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCHushedComponent, RMCChatTypeModifyEvent>(OnChatTypeModify);
    }

    private void OnChatTypeModify(Entity<RMCHushedComponent> ent, ref RMCChatTypeModifyEvent args)
    {
        // Only convert Speak to Whisper, leave other types unchanged
        if (args.DesiredType != InGameICChatType.Speak)
            return;

        args.ModifiedType = InGameICChatType.Whisper;
        _popupSystem.PopupEntity(Loc.GetString("rmc-hushed-can-only-whisper"), ent, ent);
    }
}
