using System.Linq;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Weapons.Ranged.Magazine;

public sealed class RMCMagazineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMagazineVisualsComponent, MapInitEvent>(OnMagazineInit);
        SubscribeLocalEvent<RMCMagazineVisualsComponent, GunShotEvent>(OnMagazineGunShot);
        SubscribeLocalEvent<RMCMagazineVisualsComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotInserted);
        SubscribeLocalEvent<RMCMagazineVisualsComponent, EntRemovedFromContainerMessage>(OnMagazineSlotRemoved);
    }

    private void OnMagazineInit(Entity<RMCMagazineVisualsComponent> ent, ref MapInitEvent args)
    {
        UpdateMagazine(ent, ent.Comp.ContainerId);
    }

    private void OnMagazineGunShot(Entity<RMCMagazineVisualsComponent> ent, ref GunShotEvent args)
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

        var ammoCountEvent = new GetAmmoCountEvent();
        RaiseLocalEvent(ent, ref ammoCountEvent);

        _appearance.SetData(ent, RMCMagazineVisuals.SlideOpen, ammoCountEvent.Count <= 0, appearance);
    }
}
