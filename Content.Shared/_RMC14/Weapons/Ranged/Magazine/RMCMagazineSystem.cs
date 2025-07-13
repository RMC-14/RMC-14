using System.Linq;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Weapons.Ranged.Magazine;

public sealed class RMCMagazineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMagazineVisualsComponent, ComponentInit>(OnMagazineInit);
        SubscribeLocalEvent<RMCMagazineVisualsComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotInserted);
        SubscribeLocalEvent<RMCMagazineVisualsComponent, EntRemovedFromContainerMessage>(OnMagazineSlotRemoved);
    }

    private void OnMagazineInit(Entity<RMCMagazineVisualsComponent> ent, ref ComponentInit args)
    {
        UpdateMagazine(ent, ent.Comp.ContainerId);
    }

    private void OnMagazineSlotInserted(Entity<RMCMagazineVisualsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateMagazine(ent, args.Container.ID);
    }

    private void OnMagazineSlotRemoved(Entity<RMCMagazineVisualsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateMagazine(ent, args.Container.ID);
    }

    public void UpdateMagazine(Entity<RMCMagazineVisualsComponent> ent, string containerID)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (ent.Comp.ContainerId != containerID)
            return;

        if (TryComp<BallisticAmmoProviderComponent>(ent, out var ballisticProvider) && ballisticProvider.UnspawnedCount > 0)
        {
            _appearance.SetData(ent, RMCMagazineVisuals.SlideOpen, false, appearance);
            return;
        }

        var ammoEnt = GetAmmoEntity(ent, containerID);
        _appearance.SetData(ent, RMCMagazineVisuals.SlideOpen, ammoEnt == null, appearance);
    }

    public EntityUid? GetAmmoEntity(EntityUid uid, string containerID)
    {
        if (_container.TryGetContainer(uid, containerID, out var container)
            && container.ContainedEntities.Count > 0
            && container.ContainedEntities.FirstOrDefault() is { } containedEntity)
        {
            return containedEntity;
        }

        return null;
    }
}
