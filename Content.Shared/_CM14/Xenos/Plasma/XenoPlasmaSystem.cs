using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;

namespace Content.Shared._CM14.Xenos.Plasma;

public sealed class XenoPlasmaSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoComponent, RejuvenateEvent>(OnXenoRejuvenate);
        SubscribeLocalEvent<XenoComponent, XenoTransferPlasmaActionEvent>(OnXenoTransferPlasmaAction);
        SubscribeLocalEvent<XenoComponent, XenoTransferPlasmaDoAfterEvent>(OnXenoTransferDoAfter);
    }

    private void OnXenoRejuvenate(Entity<XenoComponent> ent, ref RejuvenateEvent args)
    {
        RegenPlasma((ent, ent), ent.Comp.MaxPlasma);
    }

    private void OnXenoTransferPlasmaAction(Entity<XenoComponent> ent, ref XenoTransferPlasmaActionEvent args)
    {
        if (ent.Owner == args.Target ||
            !HasComp<XenoComponent>(args.Target) ||
            !HasPlasma(ent, args.Amount))
        {
            return;
        }

        args.Handled = true;

        var ev = new XenoTransferPlasmaDoAfterEvent(args.Amount);
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.PlasmaTransferDelay, ev, ent, args.Target)
        {
            BreakOnUserMove = true,
            DistanceThreshold = args.Range
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoTransferDoAfter(Entity<XenoComponent> self, ref XenoTransferPlasmaDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (self.Owner == target ||
            !TryComp(target, out XenoComponent? otherXeno) ||
            !TryRemovePlasma(self, args.Amount))
        {
            return;
        }

        RegenPlasma(target, args.Amount);

        // for some reason the popup will sometimes not show for the predicting client here
        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString("cm-xeno-plasma-transferred-to-other", ("plasma", args.Amount), ("target", target), ("total", self.Comp.Plasma)), self, self);
        _popup.PopupEntity(Loc.GetString("cm-xeno-plasma-transferred-to-self", ("plasma", args.Amount), ("target", self.Owner), ("total", otherXeno.Plasma)), target, target);

        _audio.PlayPredicted(self.Comp.PlasmaTransferSound, self, self);
    }

    public bool HasPlasma(Entity<XenoComponent> xeno, FixedPoint2 plasma)
    {
        return xeno.Comp.Plasma >= plasma;
    }

    public bool HasPlasmaPopup(Entity<XenoComponent> xeno, FixedPoint2 plasma, bool predicted = true)
    {
        if (!HasPlasma(xeno, plasma))
        {
            var popup = Loc.GetString("cm-xeno-not-enough-plasma");
            if (predicted)
                _popup.PopupClient(popup, xeno, xeno);
            else
                _popup.PopupEntity(popup, xeno, xeno);

            return false;
        }

        return true;
    }

    public void RegenPlasma(Entity<XenoComponent?> xeno, FixedPoint2 amount)
    {
        if (!_xenoQuery.Resolve(xeno, ref xeno.Comp))
            return;

        xeno.Comp.Plasma = FixedPoint2.Min(xeno.Comp.Plasma + amount, xeno.Comp.MaxPlasma);
        Dirty(xeno, xeno.Comp);
    }

    public bool TryRemovePlasma(Entity<XenoComponent> xeno, FixedPoint2 plasma)
    {
        if (!HasPlasma(xeno, plasma))
            return false;

        RemovePlasma(xeno, plasma);
        return true;
    }

    public bool TryRemovePlasmaPopup(Entity<XenoComponent?> xeno, FixedPoint2 plasma)
    {
        if (!Resolve(xeno, ref xeno.Comp))
            return false;

        if (TryRemovePlasma((xeno, xeno.Comp), plasma))
            return true;

        _popup.PopupClient(Loc.GetString("cm-xeno-not-enough-plasma"), xeno, xeno);
        return false;
    }

    public void RemovePlasma(Entity<XenoComponent> xeno, FixedPoint2 plasma)
    {
        xeno.Comp.Plasma = FixedPoint2.Max(xeno.Comp.Plasma - plasma, FixedPoint2.Zero);
        Dirty(xeno);
    }
}
