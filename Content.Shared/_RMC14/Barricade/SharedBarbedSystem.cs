using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Barricade.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Barricade;

public abstract class SharedBarbedSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BarbedComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<BarbedComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<BarbedComponent, DoAfterAttemptEvent<BarbedDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<BarbedComponent, BarbedDoAfterEvent>(OnDoAfter);


        SubscribeLocalEvent<BarbedComponent, CutBarbedDoAfterEvent>(WireCutterOnDoAfter);
        SubscribeLocalEvent<BarbedComponent, DoorStateChangedEvent>(OnDoorStateChanged);
        SubscribeLocalEvent<BarbedComponent, AttemptClimbEvent>(OnClimbAttempt);
        SubscribeLocalEvent<BarbedComponent, CMGetArmorPiercingEvent>(OnGetArmorPiercing);
    }

    public void OnInteractUsing(EntityUid uid, BarbedComponent component, InteractUsingEvent args)
    {
        if (!component.IsBarbed && HasComp<BarbedWireComponent>(args.Used))
        {
            var ev = new BarbedDoAfterEvent();
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.WireTime, ev, uid, used: args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                AttemptFrequency = AttemptFrequency.EveryTick,
                CancelDuplicate = false,
                DuplicateCondition = DuplicateConditions.None
            };
            if (_doAfterSystem.TryStartDoAfter(doAfterEventArgs))
            {
                _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-wiring"), uid, args.User);
            }
            return;
        }

        if (component.IsBarbed && HasComp<BarbedWireComponent>(args.Used))
        {
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-full"), uid, args.User);
            return;
        }

        if (component.IsBarbed && TryComp<ToolComponent>(args.Used, out var tool))
        {
            if (_toolSystem.HasQuality(args.Used, component.RemoveQuality, tool))
            {
                _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-begin"), uid, args.User);
                var wirecutterDoAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.CutTime, new CutBarbedDoAfterEvent(), uid, used: args.Used)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                };
                _doAfterSystem.TryStartDoAfter(wirecutterDoAfterEventArgs);
            }
        }
    }

    private void OnDoAfterAttempt(Entity<BarbedComponent> barbed, ref DoAfterAttemptEvent<BarbedDoAfterEvent> args)
    {
        // If the targeted entity gets barbed during the doafter, end the doafter
        if (barbed.Comp.IsBarbed)
        {
            args.Cancel();
        }
    }

    private void OnDoAfter(Entity<BarbedComponent> barbed, ref BarbedDoAfterEvent args)
    {
        if (args.Used == null || args.Cancelled || args.Handled)
            return;

        // If the targeted entity gets barbed during the doafter, don't use up a barbed wire
        if (barbed.Comp.IsBarbed)
        {
            return;
        }

        args.Handled = true;

        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.Use(args.Used.Value, 1, stackComp);
        }

        barbed.Comp.IsBarbed = true;
        Dirty(barbed);

        UpdateAppearance(barbed);

        _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-success"), barbed.Owner, args.User);
    }

    private void WireCutterOnDoAfter(Entity<BarbedComponent> barbed, ref CutBarbedDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (_netManager.IsClient)
            return;

        var coordinates = _transform.GetMoverCoordinates(barbed);
        EntityManager.SpawnEntity(barbed.Comp.Spawn, coordinates);

        barbed.Comp.IsBarbed = false;
        Dirty(barbed);

        UpdateAppearance(barbed);

        _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-finish"), barbed.Owner, args.User);
    }

    private void OnAttacked(Entity<BarbedComponent> barbed, ref AttackedEvent args)
    {
        if (barbed.Comp.IsBarbed)
        {
            _damageableSystem.TryChangeDamage(args.User, barbed.Comp.ThornsDamage, origin: barbed, tool: barbed);
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-damage"), barbed, args.User, PopupType.SmallCaution);
        }
    }

    private void OnDoorStateChanged(Entity<BarbedComponent> barbed, ref DoorStateChangedEvent args)
    {
        UpdateAppearance(barbed);
    }

    private void OnClimbAttempt(Entity<BarbedComponent> barbed, ref AttemptClimbEvent args)
    {
        if (barbed.Comp.IsBarbed)
        {
            args.Cancelled = true;
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-cant-climb"), barbed.Owner, args.User);
        }
    }

    private void OnGetArmorPiercing(Entity<BarbedComponent> barbed, ref CMGetArmorPiercingEvent args)
    {
        if (barbed.Comp.IsBarbed)
            args.Piercing = 1000;
    }

    protected void UpdateAppearance(Entity<BarbedComponent> barbed)
    {
        var open = TryComp(barbed, out DoorComponent? door) && door.State == DoorState.Open;

        var visual = (barbed.Comp.IsBarbed, open) switch
        {
            (true, true) => BarbedWireVisuals.WiredOpen,
            (true, false) => BarbedWireVisuals.WiredClosed,
            _ => BarbedWireVisuals.UnWired,
        };

        _appearance.SetData(barbed, BarbedWireVisualLayers.Wire, visual);
    }
}
