using System.Linq;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Crest;
using Content.Shared._RMC14.Xenonids.Headbutt;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Explosion;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Physics.Systems;
using static Content.Shared._RMC14.Xenonids.Fortify.XenoFortifyComponent;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._RMC14.Xenonids.Fortify;

public sealed class XenoFortifySystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        // TODO RMC14 resist knockback from small explosives
        SubscribeLocalEvent<XenoFortifyComponent, XenoFortifyActionEvent>(OnXenoFortifyAction);

        SubscribeLocalEvent<XenoFortifyComponent, CMGetArmorEvent>(OnXenoFortifyGetArmor);
        SubscribeLocalEvent<XenoFortifyComponent, BeforeStatusEffectAddedEvent>(OnXenoFortifyBeforeStatusAdded);
        SubscribeLocalEvent<XenoFortifyComponent, GetExplosionResistanceEvent>(OnXenoFortifyGetExplosionResistance);

        SubscribeLocalEvent<XenoFortifyComponent, ChangeDirectionAttemptEvent>(OnXenoFortifyCancel);
        SubscribeLocalEvent<XenoFortifyComponent, UpdateCanMoveEvent>(OnXenoFortifyCancel);

        SubscribeLocalEvent<XenoFortifyComponent, AttackAttemptEvent>(OnXenoFortifyCancel);
        SubscribeLocalEvent<XenoFortifyComponent, XenoHeadbuttAttemptEvent>(OnXenoFortifyHeadbuttAttempt);
        SubscribeLocalEvent<XenoFortifyComponent, XenoRestAttemptEvent>(OnXenoFortifyRestAttempt);
        SubscribeLocalEvent<XenoFortifyComponent, XenoTailSweepAttemptEvent>(OnXenoFortifyTailSweepAttempt);
        SubscribeLocalEvent<XenoFortifyComponent, XenoToggleCrestAttemptEvent>(OnXenoFortifyToggleCrestAttempt);
        SubscribeLocalEvent<XenoFortifyComponent, MobStateChangedEvent>(OnXenoFortifyMobStateChanged);
    }

    private void OnXenoFortifyAction(Entity<XenoFortifyComponent> xeno, ref XenoFortifyActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoFortifyAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;

        if (xeno.Comp.Fortified)
            Unfortify(xeno);
        else
            Fortify(xeno);
    }

    private void OnXenoFortifyGetArmor(Entity<XenoFortifyComponent> xeno, ref CMGetArmorEvent args)
    {
        if (xeno.Comp.Fortified)
        {
            args.Armor += xeno.Comp.Armor;
            args.FrontalArmor += xeno.Comp.FrontalArmor;
        }
    }

    private void OnXenoFortifyBeforeStatusAdded(Entity<XenoFortifyComponent> xeno, ref BeforeStatusEffectAddedEvent args)
    {
        if (xeno.Comp.Fortified && xeno.Comp.ImmuneToStatuses.Contains(args.Key))
            args.Cancelled = true;
    }

    private void OnXenoFortifyGetExplosionResistance(Entity<XenoFortifyComponent> xeno, ref GetExplosionResistanceEvent args)
    {
        if (xeno.Comp.Fortified)
        {
            args.DamageCoefficient *= xeno.Comp.ExplosionMultiplier;
        }
    }

    private void OnXenoFortifyCancel<T>(Entity<XenoFortifyComponent> xeno, ref T args) where T : CancellableEntityEventArgs
    {
        if (xeno.Comp.Fortified)
            args.Cancel();
    }

    private void OnXenoFortifyHeadbuttAttempt(Entity<XenoFortifyComponent> xeno, ref XenoHeadbuttAttemptEvent args)
    {
        if (xeno.Comp.Fortified)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fortify-cant-headbutt"), xeno, xeno);
            args.Cancelled = true;
        }
    }

    private void OnXenoFortifyRestAttempt(Entity<XenoFortifyComponent> xeno, ref XenoRestAttemptEvent args)
    {
        if (xeno.Comp.Fortified)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fortify-cant-rest"), xeno, xeno);
            args.Cancelled = true;
        }
    }

    private void OnXenoFortifyTailSweepAttempt(Entity<XenoFortifyComponent> xeno, ref XenoTailSweepAttemptEvent args)
    {
        if (xeno.Comp.Fortified)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fortify-cant-tail-sweep"), xeno, xeno);
            args.Cancelled = true;
        }
    }

    private void OnXenoFortifyToggleCrestAttempt(Entity<XenoFortifyComponent> xeno, ref XenoToggleCrestAttemptEvent args)
    {
        if (xeno.Comp.Fortified)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-fortify-cant-toggle-crest"), xeno, xeno);
            args.Cancelled = true;
        }
    }

    private void OnXenoFortifyMobStateChanged(Entity<XenoFortifyComponent> xeno, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            Unfortify(xeno);
    }

    private void Fortify(Entity<XenoFortifyComponent> xeno)
    {
        xeno.Comp.Fortified = true;

        if (TryComp<RMCSizeComponent>(xeno, out var size))
        {
            xeno.Comp.OriginalSize = size.Size;
            size.Size = xeno.Comp.FortifySize;
            Dirty(xeno.Owner, size);
        }

        _fixtures.TryCreateFixture(xeno, xeno.Comp.Shape, FixtureId, hard: true, collisionLayer: (int) WallLayer);
        _transform.AnchorEntity((xeno, Transform(xeno)));

        FortifyUpdated(xeno);
    }

    private void Unfortify(Entity<XenoFortifyComponent> xeno)
    {
        xeno.Comp.Fortified = false;

        if (TryComp<RMCSizeComponent>(xeno, out var size))
        {
            size.Size = xeno.Comp.OriginalSize ?? RMCSizes.Xeno;
            Dirty(xeno.Owner, size);
        }

        _fixtures.DestroyFixture(xeno, FixtureId);
        _transform.Unanchor(xeno, Transform(xeno));

        FortifyUpdated(xeno);
    }

    private void FortifyUpdated(Entity<XenoFortifyComponent> xeno)
    {
        _actionBlocker.UpdateCanMove(xeno);
        _appearance.SetData(xeno, XenoVisualLayers.Fortify, xeno.Comp.Fortified);

        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoFortifyActionEvent)
                _actions.SetToggled(actionId, xeno.Comp.Fortified);
        }

        Dirty(xeno);

        var ev = new XenoFortifiedEvent(xeno.Comp.Fortified);
        RaiseLocalEvent(xeno, ref ev);
    }
}
