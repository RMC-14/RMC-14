using Content.Server.Popups;
using Content.Shared._RMC14.Chat.Events;
using Content.Shared._RMC14.Speech.WhisperOnly;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Speech.WhisperOnly;

/// <summary>
/// System that forces entities with RMCWhisperOnlyComponent to only whisper instead of speaking normally.
/// When trying to speak (Say), it will be converted to Whisper and a popup will be shown.
/// Also handles adding/removing component when status effect is applied/removed.
/// </summary>
public sealed class RMCWhisperOnlySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    private static readonly EntProtoId RMCWhisperOnlyStatusEffect = "RMCStatusEffectWhisperOnly";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCWhisperOnlyComponent, RMCChatTypeModifyEvent>(OnChatTypeModify);
        SubscribeLocalEvent<StatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<StatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
    }

    private void OnChatTypeModify(Entity<RMCWhisperOnlyComponent> ent, ref RMCChatTypeModifyEvent args)
    {
        // Only convert Speak (0) to Whisper (2), leave other types unchanged
        if (args.DesiredType != 0) // 0 = InGameICChatType.Speak
            return;

        args.ModifiedType = 2; // 2 = InGameICChatType.Whisper

        _popupSystem.PopupEntity(Loc.GetString("rmc-whisper-only-can-only-whisper"), ent, ent);
    }

    private void OnStatusEffectApplied(Entity<StatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        // Check if this is the RMCWhisperOnly status effect
        var meta = MetaData(ent);
        if (meta.EntityPrototype?.ID != RMCWhisperOnlyStatusEffect.Id)
            return;

        EnsureComp<RMCWhisperOnlyComponent>(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<StatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        // Check if this is the RMCWhisperOnly status effect
        var meta = MetaData(ent);
        if (meta.EntityPrototype?.ID != RMCWhisperOnlyStatusEffect.Id)
            return;

        if (ent.Comp.AppliedTo == null)
            return;

        // Check if there are other RMCWhisperOnly status effects active
        if (!TryComp<StatusEffectContainerComponent>(ent.Comp.AppliedTo, out var container))
            return;

        foreach (var effect in container.ActiveStatusEffects)
        {
            if (effect == ent.Owner)
                continue;

            var effectMeta = MetaData(effect);
            if (effectMeta.EntityPrototype?.ID == RMCWhisperOnlyStatusEffect.Id)
                return; // Another RMCWhisperOnly effect is still active
        }

        // Remove RMCWhisperOnlyComponent from the target entity
        RemComp<RMCWhisperOnlyComponent>(ent.Comp.AppliedTo.Value);
    }
}
