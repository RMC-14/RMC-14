using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Actions;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Egg.EggRetriever;

public sealed partial class XenoEggRetrieverSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
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

    }

    private void OnXenoRetrieveEgg(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoRetrieveEggActionEvent args)
    {
        var (ent, comp) = eggRetriever;

        var target = args.Target;

        if (!_container.TryGetContainer(ent, XenoEggRetrieverComponent.EggContainerId, out var eggContainer))
        {
            return;
        }

        // If none of the entities on the selected, in-range tile are eggs, try to pull an egg out of inventory
        if (_interact.InRangeUnobstructed(ent, target))
        {
            var clickedEntities = _lookup.GetEntitiesIntersecting(target);
            var tileHasEggs = false;

            foreach (var possibleEgg in clickedEntities)
            {
                if (!HasComp<XenoEggComponent>(possibleEgg))
                {
                    continue;
                }

                tileHasEggs = true;

                if (eggContainer.Count >= comp.MaxEggs)
                {
                    _popup.PopupClient(Loc.GetString("cm-xeno-retrieve-egg-too-many-eggs"), ent, ent);
                    return;
                }

                _container.Insert(possibleEgg, eggContainer);
            }

            if (tileHasEggs)
            {
                return;
            }
        }

        if (eggContainer.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-retrieve-egg-no-eggs"), ent, ent);
            return;
        }

        if (!eggContainer.ContainedEntities.TryFirstOrNull(out var egg))
        {
            return;
        }

        _hands.TryPickupAnyHand(ent, egg.Value);
    }

    private void OnXenoRetrieverUseInHand(Entity<XenoEggRetrieverComponent> eggRetriever, ref XenoEggUseInHandEvent args)
    {
        var (ent, comp) = eggRetriever;
        if (args.Handled)
        {
            return;
        }

        if (!_container.TryGetContainer(ent, XenoEggRetrieverComponent.EggContainerId, out var eggContainer))
        {
            return;
        }

        if (eggContainer.Count >= comp.MaxEggs)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-retrieve-egg-too-many-eggs"), ent, ent);
            return;
        }

        _container.Insert(_entities.GetEntity(args.UsedEgg), eggContainer);
        _popup.PopupClient(Loc.GetString("cm-xeno-retrieve-egg-stash-egg"), ent, ent);
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

    private bool DropAllStoredEggs(Entity<XenoEggRetrieverComponent> xeno)
    {
        if (!_container.TryGetContainer(xeno.Owner, XenoEggRetrieverComponent.EggContainerId, out var eggContainer))
        {
            return false;
        }
        foreach (var egg in eggContainer.ContainedEntities)
        {
            _transform.PlaceNextTo(xeno.Owner, egg);
        }
        return true;
    }

}
