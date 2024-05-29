using Content.Shared._CM14.Xenos.Rest;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Movement.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Weeds;

public abstract class SharedXenoWeedsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();

    private EntityQuery<XenoWeedsComponent> _weedsQuery;

    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        _weedsQuery = GetEntityQuery<XenoWeedsComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<XenoWeedsComponent, AnchorStateChangedEvent>(OnWeedsAnchorChanged);

        SubscribeLocalEvent<XenoWeedableComponent, AnchorStateChangedEvent>(OnWeedableAnchorStateChanged);

        SubscribeLocalEvent<DamageOffWeedsComponent, MapInitEvent>(OnDamageOffWeedsMapInit);

        //TODO: Is there maybe a more narrow component to filter for?
        SubscribeLocalEvent<MobMoverComponent, MapInitEvent>(OnMapInitWeedsCheck);

        SubscribeLocalEvent<XenoWeedsComponent, StartCollideEvent>(OnWeedsStartCollide);
        SubscribeLocalEvent<XenoWeedsComponent, EndCollideEvent>(OnWeedsEndCollide);
    }

    private void OnWeedsAnchorChanged(Entity<XenoWeedsComponent> weeds, ref AnchorStateChangedEvent args)
    {
        if (_net.IsServer && !args.Anchored)
            QueueDel(weeds);
    }

    private void OnWeedableAnchorStateChanged(Entity<XenoWeedableComponent> weedable, ref AnchorStateChangedEvent args)
    {
        if (_net.IsServer && !args.Anchored)
            QueueDel(weedable.Comp.Entity);
    }

    private void OnDamageOffWeedsMapInit(Entity<DamageOffWeedsComponent> damage, ref MapInitEvent args)
    {
        damage.Comp.DamageAt = _timing.CurTime + damage.Comp.Every;
    }

    private void OnMapInitWeedsCheck(Entity<MobMoverComponent> mobMover, ref MapInitEvent args)
    {
        if (!IsOnWeeds(mobMover.Owner))
            return;

        //TODO: Add and set  OnWeedsComponent
        //xeno.Comp.OnWeeds = _xenoWeeds.IsOnWeeds(xeno.Owner);
    }

    public bool IsOnWeeds(Entity<MapGridComponent> grid, EntityCoordinates coordinates)
    {
        var position = _mapSystem.LocalToTile(grid, grid, coordinates);
        var enumerator = _mapSystem.GetAnchoredEntitiesEnumerator(grid, grid, position);

        while (enumerator.MoveNext(out var anchored))
        {
            if (_weedsQuery.HasComponent(anchored))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsOnWeeds(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var coordinates = _transform.GetMoverCoordinates(entity, entity.Comp).SnapToGrid(EntityManager, _map);

        if (coordinates.GetGridUid(EntityManager) is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid))
        {
            return false;
        }

        return IsOnWeeds((gridUid, grid), coordinates);
    }

    private void OnWeedsStartCollide(Entity<XenoWeedsComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!HasComp<MobMoverComponent>(other) ||
            TryComp<OnXenoWeedsComponent>(other, out var weeds) && weeds.OnXenoWeeds)
            return;

        _toUpdate.Add(other);
    }

    private void OnWeedsEndCollide(Entity<XenoWeedsComponent> ent, ref EndCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!HasComp<MobMoverComponent>(other) ||
            TryComp<OnXenoWeedsComponent>(other, out var weeds) && !weeds.OnXenoWeeds)
            return;

        _toUpdate.Add(other);
    }

    public override void Update(float frameTime)
    {
        // Update OnWeeds status of mobs
        foreach (var mobId in _toUpdate)
        {
            //if (!_xenoQuery.TryGetComponent(mobId, out var xeno))
            //    continue;

            var any = false;
            foreach (var contact in _physics.GetContactingEntities(mobId))
            {
                if (HasComp<XenoWeedsComponent>(contact))
                {
                    any = true;
                    break;
                }
            }

            EnsureComp<OnXenoWeedsComponent>(mobId);
            //TODO there is probably a cleaner way to do this, in one step
            if (!TryComp<OnXenoWeedsComponent>(mobId, out var weeds) || weeds.OnXenoWeeds == any)
                continue;

            weeds.OnXenoWeeds = any;
            Dirty(mobId, weeds);

            var ev = new XenoOnWeedsChangedEvent(weeds.OnXenoWeeds);
            RaiseLocalEvent(mobId, ref ev);
        }

        _toUpdate.Clear();

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<DamageOffWeedsComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var damage, out var damageable))
        {
            //EnsureComp<OnXenoWeedsComponent>(uid);
            if (TryComp<OnXenoWeedsComponent>(uid, out var weeds) && weeds.OnXenoWeeds)
            {
                if (damage.DamageAt != null)
                {
                    damage.DamageAt = null;
                    Dirty(uid, damage);
                }

                continue;
            }
            else if (damage.DamageAt == null)
            {
                damage.DamageAt = time + damage.Every;
                Dirty(uid, damage);
            }

            if (time < damage.DamageAt)
                continue;

            damage.DamageAt = time + damage.Every;

            if (!damage.RestingStopsDamage ||
                !HasComp<XenoRestingComponent>(uid))
            {
                _damageable.TryChangeDamage(uid, damage.Damage, damageable: damageable);
            }
        }
    }
}
