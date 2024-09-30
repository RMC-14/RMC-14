using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

public abstract partial class SharedXenoParasiteThrowerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoParasiteThrowerComponent, ExaminedEvent>(OnParasiteThrowerExamine);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoChangeParasiteReserveMessage>(OnParasiteReserveChange);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoReserveParasiteActionEvent>(OnSetReserve);
    }

    private void OnParasiteThrowerExamine(Entity<XenoParasiteThrowerComponent> thrower, ref ExaminedEvent args)
    {
        // Allow ghosts to see since they may want to join as a parasite
        if (!HasComp<XenoComponent>(args.Examiner) && !HasComp<GhostComponent>(args.Examiner))
            return;

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
}
