using Content.Shared._CM14.Comtech.Barbed.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.DoAfter;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;
using Content.Shared.Tools.Components;

namespace Content.Shared._CM14.Barbed;

public sealed class BarbedSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BarbedComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<BarbedComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BarbedComponent, BarbedDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BarbedComponent, CutBarbedDoAfterEvent>(WireCutterOnDoAfter);
    }
    public void OnInteractUsing(EntityUid uid, BarbedComponent component, InteractUsingEvent args)
    {
        if (!component.IsBarbed && HasComp<BarbedwireComponent>(args.Used))
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.WireTime, new BarbedDoAfterEvent(), uid, used: args.Used)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-wiring"), uid, args.User, PopupType.Small);
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
            return;
        }

        if (component.IsBarbed == true && HasComp<BarbedwireComponent>(args.Used))
        {
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-full"), uid, args.User, PopupType.Small);
            return;
        }

        if (component.IsBarbed == true && TryComp<ToolComponent>(args.Used, out var tool))
        {
            if (_toolSystem.HasQuality(args.Used, "Cutting", tool))
            {
                _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-begin"), uid, args.User, PopupType.Small);
                var wirecutterDoAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.CutTime, new CutBarbedDoAfterEvent(), uid, used: args.Used)
                {
                    BreakOnUserMove = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                };
                _doAfterSystem.TryStartDoAfter(wirecutterDoAfterEventArgs);
                return;
            }
        }
        return;
    }
    private void OnDoAfter(EntityUid uid, BarbedComponent component, BarbedDoAfterEvent args)
    {
        if (args.Used == null || args.Cancelled)
            return;

        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.Use(args.Used.Value, 1, stackComp);
        }

        component.IsBarbed = true;
        _appearance.SetData(uid, BarbedWireVisuals.Wired, true);
        _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-success"), uid, args.User, PopupType.Small);
        return;
    }
    private void WireCutterOnDoAfter(EntityUid uid, BarbedComponent component, CutBarbedDoAfterEvent args)
    {
        //EntityManager.SpawnEntity(component.Spawn, Transform(uid).Coordinates); Not sure how to make it so when wirecut spawns a metal rod
        component.IsBarbed = false;
        _appearance.SetData(uid, BarbedWireVisuals.Wired, false);
        _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-finish"), uid, args.User, PopupType.Small);
        return;
    }
    private void OnAttacked(EntityUid uid, BarbedComponent component, AttackedEvent args)
    {
        if (component.IsBarbed == true)
        {
            _damageableSystem.TryChangeDamage(args.User, component.ThornsDamage); //not sure how to add prediction here
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-damage"), uid, args.User, PopupType.SmallCaution);
            return;
        }
    }
}
