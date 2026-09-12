using Content.Shared.Interaction.Events;
using Content.Shared._RMC14.Dialog;
using Content.Shared.Examine;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly DialogSystem _dialog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMegaphoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCMegaphoneComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInHand(Entity<RMCMegaphoneComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        var ev = new MegaphoneInputEvent(
            GetNetEntity(args.User),
            VoiceRangeMultiplier: ent.Comp.VoiceRangeMultiplier);
        _dialog.OpenInput(args.User, Loc.GetString("rmc-megaphone-ui-text"), ev, largeInput: false, characterLimit: 150);
    }

    private void OnExamined(Entity<RMCMegaphoneComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-megaphone-examine"));
    }
}

[Serializable, NetSerializable]
public sealed record MegaphoneInputEvent(
    NetEntity Actor,
    string Message = "",
    float VoiceRangeMultiplier = 1.5f) : DialogInputEvent(Message);
