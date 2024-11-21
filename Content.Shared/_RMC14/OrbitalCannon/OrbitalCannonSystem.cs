using System.Linq;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.PowerLoader;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.OrbitalCannon;

public sealed class OrbitalCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OrbitalCannonComponent, MapInitEvent>(OnOrbitalCannonMapInit);
        SubscribeLocalEvent<OrbitalCannonComponent, PowerLoaderGrabEvent>(OnOrbitalCannonPowerLoaderGrab);

        SubscribeLocalEvent<OrbitalCannonWarheadComponent, PowerLoaderInteractEvent>(OnWarheadPowerLoaderInteract);

        SubscribeLocalEvent<OrbitalCannonFuelComponent, PowerLoaderInteractEvent>(OnFuelPowerLoaderInteract);

        SubscribeLocalEvent<OrbitalCannonComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeActivatableUIOpen);

        Subs.BuiEvents<OrbitalCannonComputerComponent>(OrbitalCannonComputerUI.Key,
            subs =>
            {
                subs.Event<OrbitalCannonComputerLoadBuiMsg>(OnComputerLoad);
                subs.Event<OrbitalCannonComputerUnloadBuiMsg>(OnComputerUnload);
                subs.Event<OrbitalCannonComputerChamberBuiMsg>(OnComputerChamber);
            });
    }

    private void OnOrbitalCannonMapInit(Entity<OrbitalCannonComponent> ent, ref MapInitEvent args)
    {
        var possibleFuels = ent.Comp.PossibleFuelRequirements.ToList();
        foreach (var warhead in ent.Comp.WarheadTypes)
        {
            if (possibleFuels.Count <= 0)
                possibleFuels = ent.Comp.PossibleFuelRequirements.ToList();

            if (possibleFuels.Count <= 0)
            {
                Log.Error($"No possible fuel choice found for {warhead}");
                return;
            }

            var fuel = _random.PickAndTake(possibleFuels);
            ent.Comp.FuelRequirements.Add(new WarheadFuelRequirement(warhead, fuel));
        }

        Dirty(ent);
    }

    private void OnOrbitalCannonPowerLoaderGrab(Entity<OrbitalCannonComponent> ent, ref PowerLoaderGrabEvent args)
    {
        if (_container.TryGetContainer(ent, ent.Comp.FuelContainer, out var fuel) &&
            fuel.ContainedEntities.Count > 0)
        {
            args.ToGrab = fuel.ContainedEntities[^1];
            args.Handled = true;
        }

        if (_container.TryGetContainer(ent, ent.Comp.WarheadContainer, out var warhead) &&
            warhead.ContainedEntities.Count > 0)
        {
            args.ToGrab = warhead.ContainedEntities[^1];
            args.Handled = true;
        }
    }

    private void OnWarheadPowerLoaderInteract(Entity<OrbitalCannonWarheadComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (!TryComp(args.Target, out OrbitalCannonComponent? cannon))
            return;

        args.Handled = true;
        var container = _container.EnsureContainer<ContainerSlot>(ent, cannon.WarheadContainer);
        if (container.ContainedEntity != null)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("There is already a warhead loaded!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (cannon.Status != CannonStatus.Unloaded)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("The cannon isn't unloaded!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (!_container.Insert(args.Used, container))
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"You can't insert {Name(args.Used)} into {Name(args.Target)}!", args.Target, buckled, PopupType.MediumCaution);
            }
        }
    }

    private void OnFuelPowerLoaderInteract(Entity<OrbitalCannonFuelComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (!TryComp(args.Target, out OrbitalCannonComponent? cannon))
            return;

        args.Handled = true;
        var container = _container.EnsureContainer<ContainerSlot>(ent, cannon.WarheadContainer);
        if (container.ContainedEntity != null)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("There is already a warhead loaded!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (cannon.Status != CannonStatus.Unloaded)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("The cannon isn't unloaded!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (!_container.Insert(args.Used, container))
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"You can't insert {Name(args.Used)} into {Name(args.Target)}!", args.Target, buckled, PopupType.MediumCaution);
            }
        }
    }

    private void OnComputerBeforeActivatableUIOpen(Entity<OrbitalCannonComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        ent.Comp.Warhead = _container.TryGetContainer(cannon, cannon.Comp.WarheadContainer, out var warheadContainer) &&
                           warheadContainer.ContainedEntities.Count > 0
            ? Name(warheadContainer.ContainedEntities[0])
            : null;
        ent.Comp.Fuel = _container.TryGetContainer(cannon, cannon.Comp.FuelContainer, out var fuelContainer)
            ? fuelContainer.ContainedEntities.Count
            : 0;
        ent.Comp.FuelRequirements = cannon.Comp.FuelRequirements;
        ent.Comp.Status = cannon.Comp.Status;

        Dirty(ent);
    }

    private void OnComputerLoad(Entity<OrbitalCannonComputerComponent> ent, ref OrbitalCannonComputerLoadBuiMsg args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        if (cannon.Comp.Status != CannonStatus.Unloaded)
            return;

        cannon.Comp.Status = CannonStatus.Loaded;
        Dirty(ent);
    }

    private void OnComputerUnload(Entity<OrbitalCannonComputerComponent> ent, ref OrbitalCannonComputerUnloadBuiMsg args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        if (cannon.Comp.Status != CannonStatus.Loaded)
            return;

        cannon.Comp.Status = CannonStatus.Unloaded;
        Dirty(ent);
    }

    private void OnComputerChamber(Entity<OrbitalCannonComputerComponent> ent, ref OrbitalCannonComputerChamberBuiMsg args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        if (cannon.Comp.Status != CannonStatus.Loaded)
            return;

        cannon.Comp.Status = CannonStatus.Chambered;
        Dirty(ent);
    }

    private bool TryGetClosestCannon(EntityUid to, out Entity<OrbitalCannonComponent> cannon)
    {
        cannon = default;
        if (!TryComp(to, out TransformComponent? transform))
            return false;

        var last = float.MaxValue;
        var query = EntityQueryEnumerator<OrbitalCannonComponent, TransformComponent>();
        while (query.MoveNext(out var cannonId, out var cannonComp, out var cannonTransform))
        {
            if (transform.Coordinates.TryDistance(EntityManager,
                    _transform,
                    cannonTransform.Coordinates,
                    out var distance) &&
                distance < last)
            {
                last = distance;
                cannon = (cannonId, cannonComp);
            }
        }

        return cannon != default;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var explosions = EntityQueryEnumerator<OrbitalCannonExplosionComponent>();
        while (explosions.MoveNext(out var uid, out var explosion))
        {
            if (explosion.Current == default && explosion.LastStepAt == default)
            {
                explosion.LastStepAt = time;
                Dirty(uid, explosion);
            }

            if (explosion.Current >= explosion.Steps.Count)
            {
                QueueDel(uid);
                continue;
            }

            var step = explosion.Steps[explosion.Current];
            if (time >= explosion.LastStepAt + step.Delay)
            {
                var coordinates = _transform.GetMapCoordinates(uid);
                _rmcExplosion.QueueExplosion(coordinates, step.Type, step.Total, step.Slope, step.Max, uid);
                explosion.Current++;
                Dirty(uid, explosion);
            }
        }
    }
}
