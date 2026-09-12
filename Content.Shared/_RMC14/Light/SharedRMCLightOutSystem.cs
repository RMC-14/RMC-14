using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Light;

public abstract class SharedRMCLightOutSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly AttachableToggleableSystem _attachableToggleable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCLightsOutOnDeathComponent, XenoParasiteInfectEvent>(OnInfect);
        SubscribeLocalEvent<RMCLightsOutOnDeathComponent, MobStateChangedEvent>(OnDeath);
        SubscribeLocalEvent<RMCLightsOutOnDeathComponent, DropHandItemsEvent>(OnDropItems, before: [typeof(SharedHandsSystem)]);
    }

    private void OnInfect(Entity<RMCLightsOutOnDeathComponent> ent, ref XenoParasiteInfectEvent args)
    {
        TurnOffLights(ent);
    }

    private void OnDeath(Entity<RMCLightsOutOnDeathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            TurnOffLights(ent);
    }

    //For guns etc as they drop too late on immediate death
    private void OnDropItems(Entity<RMCLightsOutOnDeathComponent> ent, ref DropHandItemsEvent args)
    {
        if (_mob.IsDead(ent))
            TurnOffLights(ent);
    }

    private void TurnOffLights(Entity<RMCLightsOutOnDeathComponent> ent)
    {
        var entsToCheck = new HashSet<EntityUid>();

        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            entsToCheck.Add(held);
        }

        var slots = _inventory.GetSlotEnumerator(ent.Owner);

        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } contained)
                entsToCheck.Add(contained);
        }

        foreach (var invEnt in entsToCheck)
        {
            if (TryComp<HandheldLightComponent>(invEnt, out var handLight))
            {
                _handheldLight.TurnOff((invEnt, handLight));
                Dirty(invEnt, handLight);
            }

            if (TryComp<AttachableHolderComponent>(invEnt, out var holder))
            {
                foreach (var slot in holder.Slots.Keys)
                {
                    if (_container.TryGetContainer(invEnt, slot, out var container))
                    {
                        foreach (var contained in container.ContainedEntities)
                        {
                            if (TryComp<HandheldLightComponent>(contained, out var attachHandlight) &&
                                TryComp<AttachableToggleableComponent>(contained, out var handLightAttach) &&
                                handLightAttach.Active == true)
                            {
                                _attachableToggleable.Toggle((contained, handLightAttach), null);
                                _actions.SetToggled(handLightAttach.Action, false);
                                _handheldLight.TurnOff((contained, attachHandlight));
                            }
                        }
                    }
                }
                Dirty(invEnt, holder);
            }
        }
    }

    protected virtual void ExtinguishFlare(EntityUid ent)
    {

    }
}
