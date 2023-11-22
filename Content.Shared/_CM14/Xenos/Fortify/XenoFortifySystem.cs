using Content.Shared._CM14.Xenos.Armor;
using Content.Shared._CM14.Xenos.Crest;
using Content.Shared._CM14.Xenos.Headbutt;
using Content.Shared._CM14.Xenos.Rest;
using Content.Shared._CM14.Xenos.Sweep;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Explosion;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Physics.Systems;
using static Content.Shared._CM14.Xenos.Fortify.XenoFortifyComponent;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._CM14.Xenos.Fortify;

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
        // TODO CM14 resist knockback from small explosives
        SubscribeLocalEvent<XenoFortifyComponent, XenoFortifyActionEvent>(OnXenoFortifyAction);

        SubscribeLocalEvent<XenoFortifyComponent, XenoGetArmorEvent>(OnXenoFortifyGetArmor);
        SubscribeLocalEvent<XenoFortifyComponent, BeforeStatusEffectAddedEvent>(OnXenoFortifyBeforeStatusAdded);
        SubscribeLocalEvent<XenoFortifyComponent, GetExplosionResistanceEvent>(OnXenoFortifyGetExplosionResistance);

        SubscribeLocalEvent<XenoFortifyComponent, ChangeDirectionAttemptEvent>(OnXenoFortifyCancel);
        SubscribeLocalEvent<XenoFortifyComponent, UpdateCanMoveEvent>(OnXenoFortifyCancel);

        SubscribeLocalEvent<XenoFortifyComponent, AttackAttemptEvent>(OnXenoFortifyCancel);
        SubscribeLocalEvent<XenoFortifyComponent, XenoHeadbuttAttemptEvent>(OnXenoFortifyHeadbuttAttempt);
        SubscribeLocalEvent<XenoFortifyComponent, XenoRestAttemptEvent>(OnXenoFortifyRestAttempt);
        SubscribeLocalEvent<XenoFortifyComponent, XenoTailSweepAttemptEvent>(OnXenoFortifyTailSweepAttempt);
        SubscribeLocalEvent<XenoFortifyComponent, XenoToggleCrestAttemptEvent>(OnXenoFortifyToggleCrestAttempt);

        SubscribeLocalEvent<XenoFortifyActionComponent, XenoFortifyToggledEvent>(OnXenoFortifyActionToggled);
    }

    private void OnXenoFortifyAction(Entity<XenoFortifyComponent> xeno, ref XenoFortifyActionEvent args)
    {
        if (args.Handled || !TryComp(xeno, out TransformComponent? transform))
            return;

        var attempt = new XenoFortifyAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        args.Handled = true;

        xeno.Comp.Fortified = !xeno.Comp.Fortified;
        Dirty(xeno);

        if (xeno.Comp.Fortified)
        {
            _fixtures.TryCreateFixture(xeno, xeno.Comp.Shape, FixtureId, hard: true, collisionLayer: (int) WallLayer);
            _transform.AnchorEntity((xeno, transform));
        }
        else
        {
            _fixtures.DestroyFixture(xeno, FixtureId);
            _transform.Unanchor(xeno, transform);
        }

        _actionBlocker.UpdateCanMove(xeno);
        _appearance.SetData(xeno, XenoVisualLayers.Fortify, xeno.Comp.Fortified);

        var ev = new XenoFortifyToggledEvent(xeno.Comp.Fortified);
        foreach (var (id, _) in _actions.GetActions(xeno))
        {
            RaiseLocalEvent(id, ref ev);
        }
    }

    private void OnXenoFortifyGetArmor(Entity<XenoFortifyComponent> xeno, ref XenoGetArmorEvent args)
    {
        if (xeno.Comp.Fortified)
        {
            args.Armor += xeno.Comp.Armor;
            args.FrontalArmor += xeno.Comp.FrontalArmor;
        }
    }

    private void OnXenoFortifyBeforeStatusAdded(Entity<XenoFortifyComponent> xeno, ref BeforeStatusEffectAddedEvent args)
    {
        if (xeno.Comp.Fortified && args.Key == "Stun")
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

    private void OnXenoFortifyActionToggled(Entity<XenoFortifyActionComponent> xeno, ref XenoFortifyToggledEvent args)
    {
        _actions.SetToggled(xeno, args.Fortified);
    }
}
