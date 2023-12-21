using Content.Shared._CM14.Comtech.Barbed.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.DoAfter;

namespace Content.Shared._CM14.Barbed;

public sealed class BarbedSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BarbedComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<BarbedComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BarbedComponent, BarbedDoAfterEvent>(OnDoAfter);
    }
    public void OnInteractUsing(EntityUid uid, BarbedComponent component, InteractUsingEvent args)
    {
        if (!HasComp<BarbedwireComponent>(args.Used))
        {
            return;
        }

        if (component.IsBarbed == true)
        {
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-full"), uid, args.User, PopupType.Small);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.WireTime, new BarbedDoAfterEvent(), uid, used: args.Used)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-wiring"), uid, args.User, PopupType.Small);
        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
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
