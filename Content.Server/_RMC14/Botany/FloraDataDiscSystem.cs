using Content.Shared._RMC14.Botany;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Botany;

public sealed class FloraDataDiscSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly SoundSpecifier WipeSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");

    public override void Initialize()
    {
        SubscribeLocalEvent<FloraDataDiscComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<FloraDataDiscComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    private void OnExamined(Entity<FloraDataDiscComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FloraDataDiscComponent)))
        {
            if (ent.Comp.Genes.Count == 0)
            {
                args.PushMarkup("The disc is blank.");
                return;
            }

            if (ent.Comp.GeneSource != null)
                args.PushMarkup($"Gene source: {ent.Comp.GeneSource}");

            foreach (var gene in ent.Comp.Genes)
            {
                args.PushMarkup($"• [color=cyan]{gene.GetLabel()}[/color]");
            }
        }
    }

    private void OnGetVerbs(Entity<FloraDataDiscComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (ent.Comp.Genes.Count == 0)
            return;

        var user = args.User;
        var uid = ent.Owner;
        args.Verbs.Add(new ActivationVerb
        {
            Text = "Wipe disc",
            Act = () => WipeDisc(uid, ent.Comp, user),
        });
    }

    private void WipeDisc(EntityUid uid, FloraDataDiscComponent comp, EntityUid user)
    {
        comp.Genes.Clear();
        comp.GeneSource = null;
        _popup.PopupEntity("You wipe the disc clean.", uid, user);
        _audio.PlayPvs(WipeSound, uid);
    }
}
