using Content.Shared._CM14.Comtech.Barbed.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Shared._CM14.Barbed;

public sealed class BarbedSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BarbedComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<BarbedComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<BarbedComponent, InteractUsingEvent>(OnInteractUsing);
    }
    private void OnInit(EntityUid uid, BarbedComponent component, ComponentStartup args)
    {
        component.BarbedSlot = _containerSystem.EnsureContainer<ContainerSlot>(uid, "barbed_slot");
    }
    private void OnInteractUsing(EntityUid uid, BarbedComponent component, InteractUsingEvent args)
    {
        if (!HasComp<BarbedwireComponent>(args.Used))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-fail"), uid, args.User);
            return;
        }

        if (Exists(component.BarbedSlot.ContainedEntity))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-already-has-jar"), uid, args.User);
            return;
        }

        component.BarbedSlot.Insert(args.Used);
        _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-success"), uid, args.User, PopupType.Medium);
    }
    
    private void OnAttacked(EntityUid uid, BarbedComponent component, AttackedEvent args)
    {
        if (Exists(component.BarbedSlot.ContainedEntity))
        {
            _damageableSystem.TryChangeDamage(args.User, component.thornsDamage);
            return;
        }
        return;
    }
}
