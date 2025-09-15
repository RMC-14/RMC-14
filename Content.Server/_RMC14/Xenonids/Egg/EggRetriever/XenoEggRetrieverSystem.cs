using System.Numerics;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Egg.EggRetriever;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Egg.EggRetriever;

public sealed partial class XenoEggRetrieverSystem : SharedXenoEggRetrieverSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _announce = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEggRetrieverComponent, MapInitEvent>(OnXenoRetrieverInit);
        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoRetrieveEggActionEvent>(OnXenoRetrieveEgg);
        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoEggUseInHandEvent>(OnXenoRetrieverUseInHand);
        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoDevolveBuiMsg>(OnXenoDevolveDoAfter);
        SubscribeLocalEvent<XenoEggRetrieverComponent, MobStateChangedEvent>(OnDeathMobStateChanged);
    }

    private void OnXenoRetrieverInit(Entity<XenoEggRetrieverComponent> eggRetriever, ref MapInitEvent args)
    {
        _appearance.SetData(eggRetriever, XenoEggStorageVisuals.Number, eggRetriever.Comp.CurEggs);
    }

    private void OnXenoRetrieveEgg(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoRetrieveEggActionEvent args)
    {
       var target = args.Target;
        args.Handled = true;

        // If none of the entities on the selected, in-range tile are eggs, try to pull an egg out of inventory
        if (_interact.InRangeUnobstructed(eggRetriever, target))
        {
            var clickedEntities = _lookup.GetEntitiesIntersecting(target);
            var tileHasEggs = false;

            foreach (var possibleEgg in clickedEntities)
            {
                if (!TryComp<XenoEggComponent>(possibleEgg, out var egg) ||
                    egg.State != XenoEggState.Item)
                    continue;

                tileHasEggs = true;

                if (eggRetriever.Comp.CurEggs >= eggRetriever.Comp.MaxEggs)
                {
                    _popup.PopupEntity(Loc.GetString("cm-xeno-retrieve-egg-too-many-eggs"), eggRetriever, eggRetriever);
                    return;
                }

                AddEgg(possibleEgg, eggRetriever);
            }

            if (tileHasEggs)
            {
                var stashMsg = Loc.GetString("cm-xeno-retrieve-egg-stash-egg", ("cur_eggs", eggRetriever.Comp.CurEggs), ("max_eggs", eggRetriever.Comp.MaxEggs));
                _popup.PopupEntity(stashMsg, eggRetriever, eggRetriever);
                return;
            }
        }

        if (eggRetriever.Comp.CurEggs == 0)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-retrieve-egg-no-eggs"), eggRetriever, eggRetriever);
            return;
        }

        if (!_hands.TryGetEmptyHand(eggRetriever.Owner, out _))
            return;

        if (RemoveEgg(eggRetriever) is not { } newEgg)
            return;

        _hive.SetSameHive(eggRetriever.Owner, newEgg);

        _hands.TryPickupAnyHand(eggRetriever, newEgg);

        var unstashMsg = Loc.GetString("cm-xeno-retrieve-egg-unstash-egg", ("cur_eggs", eggRetriever.Comp.CurEggs), ("max_eggs", eggRetriever.Comp.MaxEggs));
        _popup.PopupEntity(unstashMsg, eggRetriever, eggRetriever);
    }

    private void OnXenoRetrieverUseInHand(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoEggUseInHandEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        if (eggRetriever.Comp.CurEggs >= eggRetriever.Comp.MaxEggs)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-retrieve-egg-too-many-eggs"), eggRetriever, eggRetriever);
            return;
        }

        AddEgg(_entities.GetEntity(args.UsedEgg), eggRetriever);

        var msg = Loc.GetString("cm-xeno-retrieve-egg-stash-egg", ("cur_eggs", eggRetriever.Comp.CurEggs), ("max_eggs", eggRetriever.Comp.MaxEggs));
        _popup.PopupEntity(msg, eggRetriever, eggRetriever);

        args.Handled = true;
    }

    private void OnXenoEvolveDoAfter(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoEvolutionDoAfterEvent args)
    {
        DropAllStoredEggs(eggRetriever);
    }

    private void OnXenoDevolveDoAfter(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoDevolveBuiMsg args)
    {
        DropAllStoredEggs(eggRetriever);
    }

    private void OnDeathMobStateChanged(Entity<XenoEggRetrieverComponent> eggRetriever, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.NewMobState != MobState.Dead)
            return;
        DropAllStoredEggs(eggRetriever, 0.75f);
    }

    private bool DropAllStoredEggs(Entity<XenoEggRetrieverComponent> xeno, float chance = 1.0f)
    {
        XenoComponent? xenComp = null;
        TryComp(xeno, out xenComp);
        bool eggDropped = false;
        var hive = _hive.GetHive(xeno.Owner);

        for (var i = 0; i < xeno.Comp.CurEggs; ++i)
        {
            if (chance != 1.0 && !_random.Prob(chance))
                continue;
            eggDropped = true;
            var newEgg = Spawn(xeno.Comp.EggPrototype);
            _hive.SetHive(newEgg, hive);
            _transform.DropNextTo(newEgg, xeno.Owner);
            _throw.TryThrow(newEgg, _random.NextAngle().RotateVec(Vector2.One) * _random.NextFloat(0.15f, 0.7f), 3);
        }
        xeno.Comp.CurEggs = 0; // Just in case
        _appearance.SetData(xeno, XenoEggStorageVisuals.Number, xeno.Comp.CurEggs);
        if (chance != 1.0 && eggDropped)
            _announce.AnnounceSameHive(xeno.Owner, Loc.GetString("rmc-xeno-egg-carrier-death", ("xeno", xeno)));

        Dirty(xeno);
        return true;
    }

    /// <summary>
    /// Delete the egg provided, increment XenoEggRetriever Component's CurEggs
    /// Does not peform any checks.
    /// </summary>
    private void AddEgg(EntityUid egg, Entity<XenoEggRetrieverComponent> xeno)
    {
        xeno.Comp.CurEggs++;
        _appearance.SetData(xeno, XenoEggStorageVisuals.Number, xeno.Comp.CurEggs);

        Dirty(xeno);

        QueueDel(egg);
    }

    /// <summary>
    /// Spawn a egg, decrement XenoEggRetriever Component's CurEggs, and return the new egg.
    /// Does not peform any checks.
    /// </summary>
    private EntityUid? RemoveEgg(Entity<XenoEggRetrieverComponent> xeno)
    {
        xeno.Comp.CurEggs--;
        _appearance.SetData(xeno, XenoEggStorageVisuals.Number, xeno.Comp.CurEggs);

        Dirty(xeno);

        return Spawn(xeno.Comp.EggPrototype);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<XenoEggRetrieverComponent, XenoGenerateEggsComponent>();

        while(query.MoveNext(out var uid, out var egg, out var produce))
        {
            if (!produce.Active)
                continue;

            if (egg.CurEggs >= egg.MaxEggs)
            {
                //Has to 'pause' when full
                if ((produce.NextEgg != null && produce.NextDrain != null))
                {
                    produce.NextDrain = null;
                    produce.NextEgg = null;
                }
                continue;
            }

            if (produce.NextDrain == null || produce.NextEgg == null)
            {
                produce.NextDrain = time + produce.DrainEvery;
                produce.NextEgg = time + produce.EggEvery;
                continue;
            }

            if (time >= produce.NextDrain)
            {
                if (!_plasma.TryRemovePlasma(uid, produce.PlasmaDrain))
                {
                    _popup.PopupEntity(Loc.GetString("rmc-xeno-produce-eggs-no-plasma"), uid, uid, PopupType.SmallCaution);
                    ToggleProduceEggs(uid, produce);
                }

                produce.NextDrain = time + produce.DrainEvery;
            }

            if (time >= produce.NextEgg)
            {
                egg.CurEggs++;
                _appearance.SetData(uid, XenoEggStorageVisuals.Number, egg.CurEggs);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-produce-eggs-new-egg", ("cur_eggs", egg.CurEggs), ("max_eggs", egg.MaxEggs)), uid, uid);

                Dirty(uid, egg);
                produce.NextEgg = time + produce.EggEvery;
            }
        }
    }
}
