using Content.Server.Popups;
using Content.Shared._RMC14.Chat.Events;
using Content.Shared._RMC14.Speech.Hushed;

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
        // Only convert Speak (0) to Whisper (2), leave other types unchanged
        if (args.DesiredType != 0) // 0 = InGameICChatType.Speak
            return;

        args.ModifiedType = 2; // 2 = InGameICChatType.Whisper

        if (args.ShowPopup)
            _popupSystem.PopupEntity(Loc.GetString("rmc-hushed-can-only-whisper"), ent, ent);
    }
}
