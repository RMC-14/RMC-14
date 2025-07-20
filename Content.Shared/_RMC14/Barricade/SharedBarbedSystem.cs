using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Barricade.Components;
using Content.Shared._RMC14.Construction.Upgrades;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.Barricade;

public abstract class SharedBarbedSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
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
        SubscribeLocalEvent<BarbedComponent, RMCConstructionUpgradedEvent>(OnConstructionUpgraded);
        SubscribeLocalEvent<BarbedComponent, XenoLeapHitAttempt>(OnXenoLeapHitAttempt, after: new[] { typeof(XenoLeapSystem) });
    }

    private void OnAttacked(Entity<BarbedComponent> barbed, ref AttackedEvent args)
    {
        if (!barbed.Comp.IsBarbed)
            return;

        _damageableSystem.TryChangeDamage(args.User, barbed.Comp.ThornsDamage, origin: barbed, tool: barbed);
        _popupSystem.PopupClient(Loc.GetString("barbed-wire-damage"), barbed, args.User, PopupType.SmallCaution);
    }

    private void OnInteractUsing(Entity<BarbedComponent> ent, ref InteractUsingEvent args)
    {
        args.Handled = true;
        if (!ent.Comp.IsBarbed && HasComp<BarbedWireComponent>(args.Used))
        {
            var ev = new BarbedDoAfterEvent();
            var barbDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.WireTime, ev, ent, ent, used: args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                AttemptFrequency = AttemptFrequency.EveryTick,
                CancelDuplicate = false,
                DuplicateCondition = DuplicateConditions.SameTarget,
            };

            if (_doAfterSystem.TryStartDoAfter(barbDoAfter))
            {
                _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-wiring"), ent, args.User);
            }

            return;
        }

        if (ent.Comp.IsBarbed && HasComp<BarbedWireComponent>(args.Used))
        {
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-full"), ent, args.User);
            return;
        }

        if (!ent.Comp.IsBarbed || !TryComp<ToolComponent>(args.Used, out var tool))
            return;

        if (!_toolSystem.HasQuality(args.Used, ent.Comp.RemoveQuality, tool))
            return;

        _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-begin"), ent, args.User);
        var cutDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.CutTime, new CutBarbedDoAfterEvent(), ent, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfterSystem.TryStartDoAfter(cutDoAfter);
    }

    private void OnDoAfterAttempt(Entity<BarbedComponent> barbed, ref DoAfterAttemptEvent<BarbedDoAfterEvent> args)
    {
        // If the targeted entity gets barbed during the doafter, end the doafter
        if (barbed.Comp.IsBarbed)
            args.Cancel();
    }

    private void OnDoAfter(Entity<BarbedComponent> barbed, ref BarbedDoAfterEvent args)
    {
        if (args.Used == null || args.Cancelled || args.Handled)
            return;

        // If the targeted entity gets barbed during the doafter, don't use up a barbed wire
        if (barbed.Comp.IsBarbed)
            return;

        args.Handled = true;

        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
            _stacks.Use(args.Used.Value, 1, stackComp);

        barbed.Comp.IsBarbed = true;
        Dirty(barbed);
        UpdateBarricade(barbed);

        _audio.PlayPredicted(barbed.Comp.BarbSound, barbed.Owner, args.User);
        _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-success"), barbed.Owner, args.User);
    }

    private void WireCutterOnDoAfter(Entity<BarbedComponent> barbed, ref CutBarbedDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        barbed.Comp.IsBarbed = false;
        Dirty(barbed);
        UpdateBarricade(barbed);

        _audio.PlayPredicted(barbed.Comp.CutSound, barbed.Owner, args.User);
        _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-finish"), barbed.Owner, args.User);

        if (_netManager.IsClient)
            return;

        var coordinates = _transform.GetMoverCoordinates(barbed);
        EntityManager.SpawnEntity(barbed.Comp.Spawn, coordinates);
    }

    private void OnDoorStateChanged(Entity<BarbedComponent> barbed, ref DoorStateChangedEvent args)
    {
        UpdateBarricade(barbed);
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

    private void OnConstructionUpgraded(Entity<BarbedComponent> barbed, ref RMCConstructionUpgradedEvent args)
    {
        var newComp = EnsureComp<BarbedComponent>(args.New);
        newComp.IsBarbed = barbed.Comp.IsBarbed;

        Dirty(args.New, newComp);
        UpdateBarricade((args.New, newComp));
    }

    private void OnXenoLeapHitAttempt(Entity<BarbedComponent> ent, ref XenoLeapHitAttempt args)
    {
        if (!ent.Comp.IsBarbed)
            return;

        _damageableSystem.TryChangeDamage(args.Leaper, ent.Comp.ThornsDamage, origin: ent, tool: ent);
    }

    protected void UpdateBarricade(Entity<BarbedComponent> barbed)
    {
        var open = TryComp(barbed, out DoorComponent? door) && door.State == DoorState.Open;

        var visual = (barbed.Comp.IsBarbed, open) switch
        {
            (true, true) => BarbedWireVisuals.WiredOpen,
            (true, false) => BarbedWireVisuals.WiredClosed,
            _ => BarbedWireVisuals.UnWired,
        };

        var ev = new BarbedStateChangedEvent();
        RaiseLocalEvent(barbed, ref ev);

        // Set fixtures
        if (_fixture.GetFixtureOrNull(barbed, barbed.Comp.FixtureId) is { } fixture)
        {
            if (barbed.Comp.IsBarbed)
                _physics.AddCollisionLayer(barbed, barbed.Comp.FixtureId, fixture, (int)CollisionGroup.BarbedBarricade);
            else
                _physics.RemoveCollisionLayer(barbed, barbed.Comp.FixtureId, fixture, (int)CollisionGroup.BarbedBarricade);
        }

        _appearance.SetData(barbed, BarbedWireVisualLayers.Wire, visual);
    }
}

[ByRefEvent]
public record struct BarbedStateChangedEvent;
