using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

public abstract class SharedXenoParasiteThrowerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoParasiteThrowerComponent, ExaminedEvent>(OnParasiteThrowerExamine);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoChangeParasiteReserveMessage>(OnParasiteReserveChange);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoReserveParasiteActionEvent>(OnSetReserve);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    private void OnParasiteThrowerExamine(Entity<XenoParasiteThrowerComponent> thrower, ref ExaminedEvent args)
    {

        if (!HasComp<XenoComponent>(args.Examiner) && !HasComp<GhostComponent>(args.Examiner))
            return;

        // Allow ghosts to see free reserves since they may want to join as a parasite
        if (HasComp<GhostComponent>(args.Examiner))
        {
            var paras = Math.Max(thrower.Comp.CurParasites - thrower.Comp.ReservedParasites, 0);

            using (args.PushGroup(nameof(XenoParasiteThrowerComponent)))
            {
                args.PushMarkup(Loc.GetString("rmc-xeno-throw-parasite-reserves", ("xeno", thrower),
                    ("rev_paras", paras)));
            }
            return;
        }

        using (args.PushGroup(nameof(XenoParasiteThrowerComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-xeno-throw-parasite-current", ("xeno", thrower),
                ("cur_paras", thrower.Comp.CurParasites), ("max_paras", thrower.Comp.MaxParasites)));
        }
    }

    private void OnParasiteReserveChange(Entity<XenoParasiteThrowerComponent> thrower, ref XenoChangeParasiteReserveMessage args)
    {
        var newVal = Math.Clamp(args.NewReserve, 0, thrower.Comp.MaxParasites);
        thrower.Comp.ReservedParasites = newVal;
        Dirty(thrower);
    }

    private void OnSetReserve(Entity<XenoParasiteThrowerComponent> xeno, ref XenoReserveParasiteActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _ui.OpenUi(xeno.Owner, XenoReserveParasiteChangeUI.Key, xeno);

        args.Handled = true;
    }

    private void OnGetVerbs(Entity<XenoParasiteThrowerComponent> xeno, ref GetVerbsEvent<ActivationVerb> args)
    {
        var uid = args.User;

        if (!HasComp<ActorComponent>(uid) || !HasComp<GhostComponent>(uid))
            return;

        if (xeno.Comp.CurParasites == 0 || xeno.Comp.ReservedParasites >= xeno.Comp.CurParasites)
            return;

        var parasiteVerb = new ActivationVerb
        {
            Text = Loc.GetString("rmc-xeno-egg-ghost-verb"),
            Act = () =>
            {
                _ui.TryOpenUi(xeno.Owner, XenoParasiteGhostUI.Key, uid);
            },

            Impact = LogImpact.High,
        };

        args.Verbs.Add(parasiteVerb);
    }
}
