using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Temperature;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Campfire;

public abstract class SharedCampfireSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CampfireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CampfireComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CampfireComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<CampfireComponent, CampfireExtinguishDoAfterEvent>(OnExtinguishDoAfter);
    }

    private void OnStartup(Entity<CampfireComponent> ent, ref ComponentStartup args)
    {
        // Start with full fuel
        ent.Comp.Fuel = ent.Comp.MaxFuel;
        Dirty(ent);

        UpdateAppearance(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CampfireComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Lit || comp.LitAt == null)
                continue;

            var elapsed = _timing.CurTime - comp.LitAt.Value;
            if (elapsed >= comp.BurnDuration)
            {
                // Consume one fuel
                comp.Fuel--;
                Dirty(uid, comp);

                if (comp.Fuel > 0)
                {
                    // Reset timer for next fuel unit
                    comp.LitAt = _timing.CurTime;
                }
                else
                {
                    // Out of fuel, extinguish
                    SetLit((uid, comp), false);
                    if (_net.IsServer)
                        _popup.PopupEntity("The campfire goes out.", uid);
                }
            }
        }
    }

    private void OnActivateInWorld(Entity<CampfireComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !ent.Comp.Lit)
            return;

        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.ExtinguishDelay, new CampfireExtinguishDoAfterEvent(), ent, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            if (_net.IsServer)
                _popup.PopupEntity("You start extinguishing the campfire...", ent, args.User);
        }
    }

    private void OnExtinguishDoAfter(Entity<CampfireComponent> ent, ref CampfireExtinguishDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        SetLit(ent, false, args.User);

        if (_net.IsServer)
            _popup.PopupEntity("You extinguish the campfire.", ent, args.User);
    }

    private void OnInteractUsing(Entity<CampfireComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Check if it's wood for refueling
        if (TryComp<StackComponent>(args.Used, out var stack) && stack.StackTypeId == "RMCWood")
        {
            if (ent.Comp.Fuel >= ent.Comp.MaxFuel)
            {
                if (_net.IsServer)
                    _popup.PopupEntity("The campfire is already fully fueled.", ent, args.User);
                return;
            }

            args.Handled = true;

            var fuelToAdd = Math.Min(ent.Comp.FuelPerWood, ent.Comp.MaxFuel - ent.Comp.Fuel);
            ent.Comp.Fuel += fuelToAdd;
            Dirty(ent);

            _stack.Use(args.Used, 1, stack);

            if (_net.IsServer)
                _popup.PopupEntity("You add wood to the campfire.", ent, args.User);

            return;
        }

        // Check if it's an ignition source
        if (ent.Comp.Lit)
            return;

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(args.Used, isHotEvent);

        if (!isHotEvent.IsHot)
            return;

        if (ent.Comp.Fuel <= 0)
        {
            if (_net.IsServer)
                _popup.PopupEntity("The campfire needs fuel. Add some wood first.", ent, args.User);
            return;
        }

        args.Handled = true;
        SetLit(ent, true, args.User);
    }

    public void SetLit(Entity<CampfireComponent> ent, bool lit, EntityUid? user = null)
    {
        if (ent.Comp.Lit == lit)
            return;

        ent.Comp.Lit = lit;

        if (lit)
        {
            ent.Comp.LitAt = _timing.CurTime;
        }
        else
        {
            ent.Comp.LitAt = null;
        }

        Dirty(ent);

        if (_net.IsClient)
            return;

        if (lit)
        {
            if (ent.Comp.LitSound != null)
                _audio.PlayPvs(ent.Comp.LitSound, ent);

            if (user != null)
                _popup.PopupEntity("You light the campfire.", ent, user.Value);
        }

        UpdateAppearance(ent);
    }

    protected virtual void UpdateAppearance(Entity<CampfireComponent> ent)
    {
    }
}
