using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Body.Organ;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Medical.Stasis;

public sealed class CMStasisBagSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly MobStateSystem _mobstate = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<OrganComponent> _organQuery;

    public override void Initialize()
    {
        base.Initialize();

        _organQuery = GetEntityQuery<OrganComponent>();

        SubscribeLocalEvent<CMStasisBagComponent, ContainerIsInsertingAttemptEvent>(OnStasisInsert);
        SubscribeLocalEvent<CMStasisBagComponent, ContainerIsRemovingAttemptEvent>(OnStasisRemove);
        SubscribeLocalEvent<CMStasisBagComponent, ExaminedEvent>(OnStasisExamine);

        SubscribeLocalEvent<CMInStasisComponent, CMMetabolizeAttemptEvent>(OnBloodstreamMetabolizeAttempt);
        SubscribeLocalEvent<CMInStasisComponent, MapInitEvent>(OnInStasisMapInit);
        SubscribeLocalEvent<CMInStasisComponent, ComponentRemove>(OnInStasisRemove);
        SubscribeLocalEvent<CMInStasisComponent, GetInfectedIncubationMultiplierEvent>(OnInStasisGetInfectedIncubationMultiplier);
        SubscribeLocalEvent<CMInStasisComponent, CMBleedAttemptEvent>(OnInStasisBleedAttempt);
    }

    private void OnStasisInsert(Entity<CMStasisBagComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        OnInsert(ent, args.EntityUid);
    }

    private void OnStasisRemove(Entity<CMStasisBagComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        OnRemove(ent, args.EntityUid);
    }

    private void OnStasisExamine(Entity<CMStasisBagComponent> ent, ref ExaminedEvent args)
    {
        string msg = "rmc-stasis-new";

        if (ent.Comp.StasisLeft / ent.Comp.StasisMaxTime < 0.33f)
            msg = "rmc-stasis-very-used";
        else if (ent.Comp.StasisLeft / ent.Comp.StasisMaxTime < 0.66f)
            msg = "rmc-stasis-used";

        args.PushMarkup(Loc.GetString(msg));

    }

    private void OnBloodstreamMetabolizeAttempt(Entity<CMInStasisComponent> ent, ref CMMetabolizeAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInStasisMapInit(Entity<CMInStasisComponent> ent, ref MapInitEvent args)
    {
        _parasite.RefreshIncubationMultipliers(ent.Owner);
    }

    private void OnInStasisRemove(Entity<CMInStasisComponent> ent, ref ComponentRemove args)
    {
        _parasite.RefreshIncubationMultipliers(ent.Owner);
    }

    private void OnInStasisGetInfectedIncubationMultiplier(Entity<CMInStasisComponent> ent, ref GetInfectedIncubationMultiplierEvent args)
    {
        if (ent.Comp.Running)
        {
            // less effective in late stages
            var multiplier = ent.Comp.IncubationMultiplier;
            if (args.stage >= ent.Comp.LessEffectiveStage)
                multiplier += (multiplier / 3);

            args.Multiply(multiplier);
        }
    }

    private void OnInStasisBleedAttempt(Entity<CMInStasisComponent> ent, ref CMBleedAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInsert(Entity<CMStasisBagComponent> bag, EntityUid target)
    {
        EnsureComp<CMInStasisComponent>(target);
    }

    private void OnRemove(Entity<CMStasisBagComponent> bag, EntityUid target)
    {
        RemCompDeferred<CMInStasisComponent>(target);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var stasisQuery = EntityQueryEnumerator<CMStasisBagComponent>();

        while (stasisQuery.MoveNext(out var uid, out var bag))
        {
            if (!_container.TryGetContainer(uid, "entity_storage", out var container))
                continue;

            if (container.ContainedEntities.Count <= 0)
                continue;

            bool inStasis = false;
            foreach (var ent in container.ContainedEntities)
            {
                if(_mobstate.IsDead(ent))
                {
                    _entStorage.OpenStorage(uid);
                    _popup.PopupEntity(Loc.GetString("rmc-stasis-reject-dead"), uid, PopupType.SmallCaution);
                    continue;
                }

                if (HasComp<CMInStasisComponent>(ent))
                    inStasis = true;
            }

            if (!inStasis)
                continue;

            bag.StasisLeft -= TimeSpan.FromSeconds(frameTime);

            if(bag.StasisLeft <= TimeSpan.Zero)
            {
                _entStorage.EmptyContents(uid);
                SpawnAtPosition(bag.UsedBag, uid.ToCoordinates());
                QueueDel(uid);
            }
        }
    }

    public bool CanBodyMetabolize(EntityUid body)
    {
        // TODO RMC14 for now we need to call this manually from upstream code become upstream metabolism code is a sad joke
        var ev = new CMMetabolizeAttemptEvent();
        RaiseLocalEvent(body, ref ev);
        return !ev.Cancelled;
    }

    public bool CanOrganMetabolize(Entity<OrganComponent?> organ)
    {
        // TODO RMC14 for now we need to call this manually from upstream code become upstream metabolism code is a sad joke
        if (!_organQuery.Resolve(organ, ref organ.Comp, false) ||
            organ.Comp.Body is not { } body)
        {
            return true;
        }

        var ev = new CMMetabolizeAttemptEvent();
        RaiseLocalEvent(body, ref ev);
        return !ev.Cancelled;
    }
}
