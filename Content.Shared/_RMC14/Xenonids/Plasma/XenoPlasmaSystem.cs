using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Plasma;

public sealed class XenoPlasmaSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;

    private EntityQuery<XenoPlasmaComponent> _xenoPlasmaQuery;

    public override void Initialize()
    {
        _xenoPlasmaQuery = GetEntityQuery<XenoPlasmaComponent>();

        SubscribeLocalEvent<XenoPlasmaComponent, MapInitEvent>(OnXenoPlasmaMapInit);
        SubscribeLocalEvent<XenoPlasmaComponent, ComponentRemove>(OnXenoPlasmaRemove);
        SubscribeLocalEvent<XenoPlasmaComponent, RejuvenateEvent>(OnXenoRejuvenate);
        SubscribeLocalEvent<XenoPlasmaComponent, XenoTransferPlasmaActionEvent>(OnXenoTransferPlasmaAction);
        SubscribeLocalEvent<XenoPlasmaComponent, XenoTransferPlasmaDoAfterEvent>(OnXenoTransferDoAfter);
        SubscribeLocalEvent<XenoPlasmaComponent, NewXenoEvolvedEvent>(OnNewXenoEvolved);
        SubscribeLocalEvent<XenoPlasmaComponent, XenoDevolvedEvent>(OnXenoDevolved);

        SubscribeLocalEvent<XenoActionPlasmaComponent, RMCActionUseAttemptEvent>(OnXenoActionEnergyUseAttempt);
        SubscribeLocalEvent<XenoActionPlasmaComponent, RMCActionUseEvent>(OnXenoActionEnergyUse);
    }

    private void OnXenoPlasmaMapInit(Entity<XenoPlasmaComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnXenoPlasmaRemove(Entity<XenoPlasmaComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }

    private void OnXenoRejuvenate(Entity<XenoPlasmaComponent> xeno, ref RejuvenateEvent args)
    {
        RegenPlasma((xeno, xeno), xeno.Comp.MaxPlasma);
    }

    private void OnXenoTransferPlasmaAction(Entity<XenoPlasmaComponent> xeno, ref XenoTransferPlasmaActionEvent args)
    {
        if (xeno.Owner == args.Target)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-plasma-cannot-self"), xeno, xeno);
            return;
        }

        if (HasComp<XenoAttachedOvipositorComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-plasma-ovipositor"), xeno, xeno);
            return;
        }

        if (!TryComp(args.Target, out XenoPlasmaComponent? targetPlasma) ||
            targetPlasma.MaxPlasma == 0)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-plasma-other-max-zero", ("target", args.Target)), xeno, xeno);
            return;
        }

        if (!HasPlasmaPopup((xeno, xeno), args.Amount))
            return;

        args.Handled = true;

        var ev = new XenoTransferPlasmaDoAfterEvent(args.Amount);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.PlasmaTransferDelay, ev, xeno, args.Target)
        {
            BreakOnMove = true,
            DistanceThreshold = args.Range,
            TargetEffect = "RMCEffectHealPlasma",
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoTransferDoAfter(Entity<XenoPlasmaComponent> self, ref XenoTransferPlasmaDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (self.Owner == target ||
            HasComp<XenoAttachedOvipositorComponent>(args.Target) ||
            !TryComp(target, out XenoPlasmaComponent? otherXeno) ||
            !TryRemovePlasma((self, self), args.Amount))
        {
            return;
        }

        args.Handled = true;
        RegenPlasma(target, args.Amount);

        _jitter.DoJitter(target, TimeSpan.FromSeconds(1), true, 80, 8, true);

        // for some reason the popup will sometimes not show for the predicting client here
        if (_net.IsClient)
            return;

        _popup.PopupEntity(Loc.GetString("cm-xeno-plasma-transferred-to-other", ("plasma", args.Amount), ("target", target), ("total", self.Comp.Plasma)), self, self);
        _popup.PopupEntity(Loc.GetString("cm-xeno-plasma-transferred-to-self", ("plasma", args.Amount), ("target", self.Owner), ("total", otherXeno.Plasma)), target, target);

        _audio.PlayPredicted(self.Comp.PlasmaTransferSound, self, self);
        args.Repeat = true;
    }

    private void OnNewXenoEvolved(Entity<XenoPlasmaComponent> newXeno, ref NewXenoEvolvedEvent args)
    {
        EvolutionTransferPlasma(args.OldXeno, newXeno);
    }

    private void OnXenoDevolved(Entity<XenoPlasmaComponent> newXeno, ref XenoDevolvedEvent args)
    {
        EvolutionTransferPlasma(args.OldXeno, newXeno);
    }

    private void OnXenoActionEnergyUseAttempt(Entity<XenoActionPlasmaComponent> action, ref RMCActionUseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasPlasmaPopup(args.User, action.Comp.Cost))
            args.Cancelled = true;
    }

    private void OnXenoActionEnergyUse(Entity<XenoActionPlasmaComponent> action, ref RMCActionUseEvent args)
    {
        if (!TryComp(args.User, out XenoPlasmaComponent? plasma))
            return;

        RemovePlasma((args.User, plasma), action.Comp.Cost);
    }

    private void EvolutionTransferPlasma(EntityUid oldXeno, Entity<XenoPlasmaComponent> newXeno)
    {
        if (!TryComp(oldXeno, out XenoPlasmaComponent? oldXenoPlasma))
            return;

        var newMax = newXeno.Comp.MaxPlasma;
        FixedPoint2 newPlasma = newMax;
        if (oldXenoPlasma.MaxPlasma > 0)
            newPlasma *= oldXenoPlasma.Plasma / oldXenoPlasma.MaxPlasma;

        SetPlasma(newXeno, newPlasma);
    }

    private void UpdateAlert(Entity<XenoPlasmaComponent> xeno)
    {
        if (xeno.Comp.MaxPlasma != 0)
        {
            var level = MathF.Max(0f, xeno.Comp.Plasma.Float());
            var max = _alerts.GetMaxSeverity(xeno.Comp.Alert);
            var severity = max - ContentHelpers.RoundToLevels(level, xeno.Comp.MaxPlasma, max + 1);
            string? plasmaResourceMessage = (int)xeno.Comp.Plasma + " / " + xeno.Comp.MaxPlasma;
            _alerts.ShowAlert(xeno, xeno.Comp.Alert, (short)severity, dynamicMessage: plasmaResourceMessage);
        }
    }

    public bool HasPlasma(Entity<XenoPlasmaComponent> xeno, FixedPoint2 plasma)
    {
        return xeno.Comp.Plasma >= plasma;
    }

    public bool HasPlasmaPopup(Entity<XenoPlasmaComponent?> xeno, FixedPoint2 plasma, bool predicted = true)
    {
        void DoPopup()
        {
            var popup = Loc.GetString("cm-xeno-not-enough-plasma");
            if (predicted)
                _popup.PopupClient(popup, xeno, xeno);
            else
                _popup.PopupEntity(popup, xeno, xeno);
        }

        if (!Resolve(xeno, ref xeno.Comp, false))
        {
            DoPopup();
            return false;
        }

        if (!HasPlasma((xeno, xeno.Comp), plasma))
        {
            DoPopup();
            return false;
        }

        return true;
    }

    public void RegenPlasma(Entity<XenoPlasmaComponent?> xeno, FixedPoint2 amount)
    {
        if (!_xenoPlasmaQuery.Resolve(xeno, ref xeno.Comp, false))
            return;

        var old = xeno.Comp.Plasma;
        xeno.Comp.Plasma = FixedPoint2.Min(xeno.Comp.Plasma + amount, xeno.Comp.MaxPlasma);

        if (old == xeno.Comp.Plasma)
            return;

        Dirty(xeno);
        UpdateAlert((xeno, xeno.Comp));
    }

    public void RemovePlasma(Entity<XenoPlasmaComponent> xeno, FixedPoint2 plasma)
    {
        xeno.Comp.Plasma = FixedPoint2.Max(FixedPoint2.Zero, xeno.Comp.Plasma - plasma);
        Dirty(xeno);
        UpdateAlert(xeno);
    }

    public void SetPlasma(Entity<XenoPlasmaComponent> xeno, FixedPoint2 plasma)
    {
        xeno.Comp.Plasma = plasma;
        Dirty(xeno);
        UpdateAlert(xeno);
    }

    public bool TryRemovePlasma(Entity<XenoPlasmaComponent?> xeno, FixedPoint2 plasma)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return false;

        if (!HasPlasma((xeno, xeno.Comp), plasma))
            return false;

        RemovePlasma((xeno, xeno.Comp), plasma);
        return true;
    }

    public bool TryRemovePlasmaPopup(Entity<XenoPlasmaComponent?> xeno, FixedPoint2 plasma)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return false;

        if (TryRemovePlasma((xeno, xeno.Comp), plasma))
            return true;

        _popup.PopupClient(Loc.GetString("cm-xeno-not-enough-plasma"), xeno, xeno);
        return false;
    }
}
