using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared._RMC14.Xenonids.Projectile.Parasite;

public abstract class SharedXenoParasiteThrowerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;

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

    public void AddParasites(Entity<XenoParasiteThrowerComponent> xeno, int count)
    {
        xeno.Comp.CurParasites = Math.Min(xeno.Comp.MaxParasites, xeno.Comp.CurParasites + count);

        UpdateParasiteClingers(xeno); // Dirties count
    }

    private List<int> GetVisualIndexes(bool[] bools, bool visible)
    {
        List<int> visualIndexes = new();
        for (int i = 0; i < bools.Length; i++)
        {
            if (bools[i] == visible)
                visualIndexes.Add(i);
        }
        return visualIndexes;
    }

    protected void UpdateParasiteClingers(Entity<XenoParasiteThrowerComponent> xeno)
    {
        var parasiteNumber = Math.Min(Math.Ceiling((((double)xeno.Comp.CurParasites / xeno.Comp.MaxParasites) * xeno.Comp.NumPositions)), xeno.Comp.NumPositions);

        var overlayNumbers = xeno.Comp.VisiblePositions.Count(position => position == true);

        if (overlayNumbers > parasiteNumber)
        {
            var visibleIndexes = GetVisualIndexes(xeno.Comp.VisiblePositions, true);
            for (var i = 0; i < overlayNumbers - parasiteNumber; i++)
            {
                var index = _random.PickAndTake(visibleIndexes);
                xeno.Comp.VisiblePositions[index] = false;
            }
        }
        else
        {
            var invisibleIndexes = GetVisualIndexes(xeno.Comp.VisiblePositions, false);
            for (var i = 0; i < parasiteNumber - overlayNumbers; i++)
            {
                var index = _random.PickAndTake(invisibleIndexes);
                xeno.Comp.VisiblePositions[index] = true;
            }
        }

        Dirty(xeno);

        //Need to clone the array for it to dirty properly
        _appearance.SetData(xeno, ParasiteOverlayVisuals.States, xeno.Comp.VisiblePositions.Clone());
    }
}
