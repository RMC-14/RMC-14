using Content.Shared.Interaction.Events;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Content.Shared.Examine;

namespace Content.Shared._RMC14.Megaphone;

public sealed class RMCMegaphoneSystem : EntitySystem
{
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMegaphoneComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCMegaphoneComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCMegaphoneComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnUseInHand(Entity<RMCMegaphoneComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        var ev = new MegaphoneInputEvent(GetNetEntity(args.User), VoiceRange: ent.Comp.VoiceRange, Amplifying: ent.Comp.Amplifying, HushedEffectDuration: ent.Comp.HushedEffectDuration);
        _dialog.OpenInput(args.User, Loc.GetString("rmc-megaphone-ui-text"), ev, largeInput: false, characterLimit: 150);
    }

    private void OnExamined(Entity<RMCMegaphoneComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("rmc-megaphone-examine"));
        args.PushMarkup(Loc.GetString(ent.Comp.Amplifying
            ? "rmc-megaphone-examine-amplifying-enabled"
            : "rmc-megaphone-examine-amplifying-disabled"));
    }

    private void OnGetVerbs(Entity<RMCMegaphoneComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = ent.Comp.Amplifying
                ? Loc.GetString("rmc-megaphone-verb-disable-amplifying")
                : Loc.GetString("rmc-megaphone-verb-enable-amplifying"),
            Message = ent.Comp.Amplifying
                ? Loc.GetString("rmc-megaphone-verb-disable-amplifying-desc")
                : Loc.GetString("rmc-megaphone-verb-enable-amplifying-desc"),
            Act = () =>
            {
                ent.Comp.Amplifying = !ent.Comp.Amplifying;
                Dirty(ent, ent.Comp);

                if (ent.Comp.ToggleSound != null)
                    _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
            }
        });
    }
}

[Serializable, NetSerializable]
public sealed record MegaphoneInputEvent(NetEntity Actor, string Message = "", float VoiceRange = 15f, bool Amplifying = true, TimeSpan HushedEffectDuration = default) : DialogInputEvent(Message);
