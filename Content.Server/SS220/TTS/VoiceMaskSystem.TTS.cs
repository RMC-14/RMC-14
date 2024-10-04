using Content.Server.Speech.Components;
using Content.Server.SS220.TTS;
using Content.Shared.Inventory;
using Content.Shared.VoiceMask;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private void InitializeTTS()
    {
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerVoiceEvent>(OnSpeakerVoiceTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVoiceMessage>(OnChangeVoice);
    }

    private void OnSpeakerVoiceTransform(EntityUid uid, VoiceMaskComponent component, TransformSpeakerVoiceEvent args)
    {
        args.VoiceId = component.VoiceId;
    }

    private void OnChangeVoice(Entity<VoiceMaskComponent> ent, ref VoiceMaskChangeVoiceMessage message)
    {
        ent.Comp.VoiceId = message.Voice;

        _popupSystem.PopupCursor(Loc.GetString("voice-mask-voice-popup-success"), message.Actor);

        TrySetLastKnownVoice(message.Actor, message.Voice);

        UpdateUI(ent);
    }

    private void TrySetLastKnownVoice(EntityUid maskWearer, string? voiceId)
    {
        if (!TryComp<VoiceOverrideComponent>(maskWearer, out var comp))
        {
            return;
        }

        comp.LastSetVoice = voiceId;
    }
}
