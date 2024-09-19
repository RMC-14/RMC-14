using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Xenonids.Egg.EggRetriever;

public sealed partial class XenoEggRetrieverSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoRetrieveEggActionEvent>(OnXenoRetrieveEgg);
        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoEggUseInHandEvent>(OnXenoRetrieverUseInHand);
        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
        SubscribeLocalEvent<XenoEggRetrieverComponent, XenoDevolveBuiMsg>(OnXenoDevolveDoAfter);
        SubscribeLocalEvent<XenoEggRetrieverComponent, MobStateChangedEvent>(OnDeathMobStateChanged);
    }

    private void OnXenoRetrieveEgg(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoRetrieveEggActionEvent args)
    {
        var (ent, comp) = eggRetriever;

        var target = args.Target;
        args.Handled = true;

        // If none of the entities on the selected, in-range tile are eggs, try to pull an egg out of inventory
        if (_interact.InRangeUnobstructed(ent, target))
        {
            var clickedEntities = _lookup.GetEntitiesIntersecting(target);
            var tileHasEggs = false;

            foreach (var possibleEgg in clickedEntities)
            {
                if (!HasComp<XenoEggComponent>(possibleEgg) ||
                    Transform(possibleEgg).Anchored)
                {
                    continue;
                }

                tileHasEggs = true;

                if (comp.CurEggs >= comp.MaxEggs)
                {
                    _popup.PopupEntity(Loc.GetString("cm-xeno-retrieve-egg-too-many-eggs"), ent, ent);
                    return;
                }

                AddEgg(possibleEgg, eggRetriever);
            }

            if (tileHasEggs)
            {
                var stashMsg = Loc.GetString("cm-xeno-retrieve-egg-stash-egg", ("cur_eggs", comp.CurEggs), ("max_eggs", comp.MaxEggs));
                _popup.PopupEntity(stashMsg, ent, ent);
                return;
            }
        }

        if (comp.CurEggs == 0)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-retrieve-egg-no-eggs"), ent, ent);
            return;
        }

        if (RemoveEgg(eggRetriever) is not EntityUid newEgg)
        {
            return;
        }
        _hands.TryPickupAnyHand(ent, newEgg);

        var unstashMsg = Loc.GetString("cm-xeno-retrieve-egg-unstash-egg", ("cur_eggs", comp.CurEggs), ("max_eggs", comp.MaxEggs));
        _popup.PopupEntity(unstashMsg, ent, ent);
    }

    private void OnXenoRetrieverUseInHand(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoEggUseInHandEvent args)
    {
        var (ent, comp) = eggRetriever;
        if (args.Handled)
        {
            return;
        }

        if (comp.CurEggs >= comp.MaxEggs)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-retrieve-egg-too-many-eggs"), ent, ent);
            return;
        }


        AddEgg(_entities.GetEntity(args.UsedEgg), eggRetriever);

        var msg = Loc.GetString("cm-xeno-retrieve-egg-stash-egg", ("cur_eggs", comp.CurEggs), ("max_eggs", comp.MaxEggs));
        _popup.PopupEntity(msg, ent, ent);
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
        if (args.NewMobState != MobState.Dead)
            return;
        DropAllStoredEggs(eggRetriever);
    }

    private bool DropAllStoredEggs(Entity<XenoEggRetrieverComponent> xeno)
    {
        for (var i = 0; i < xeno.Comp.CurEggs; ++i)
        {
            var newEgg = Spawn(xeno.Comp.EggPrototype);
            _transform.DropNextTo(newEgg, xeno.Owner);
        }
        return true;
    }

    /// <summary>
    /// Delete the egg provided, increment XenoEggRetriever Component's CurEggs
    /// Does not peform any checks.
    /// </summary>
    private void AddEgg(EntityUid egg, Entity<XenoEggRetrieverComponent> xeno)
    {
        xeno.Comp.CurEggs++;

        QueueDel(egg);
    }

    /// <summary>
    /// Spawn a egg, decrement XenoEggRetriever Component's CurEggs, and return the new egg.
    /// Does not peform any checks.
    /// </summary>
    private EntityUid? RemoveEgg(Entity<XenoEggRetrieverComponent> xeno)
    {
        xeno.Comp.CurEggs--;

        return Spawn(xeno.Comp.EggPrototype);
    }
}
