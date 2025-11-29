using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Weapons.Ranged.Magazine;

public sealed class RMCMagazineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMagazineVisualsComponent, MapInitEvent>(OnMagazineInit,
            after: new[] { typeof(SharedGunSystem) });
        SubscribeLocalEvent<RMCMagazineVisualsComponent, GunShotEvent>(OnMagazineGunShot);
        SubscribeLocalEvent<RMCMagazineVisualsComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotInserted);
        SubscribeLocalEvent<RMCMagazineVisualsComponent, EntRemovedFromContainerMessage>(OnMagazineSlotRemoved);
    }

    private void OnMagazineInit(Entity<RMCMagazineVisualsComponent> ent, ref MapInitEvent args)
    {
        UpdateMagazine(ent);
    }

    private void OnMagazineGunShot(Entity<RMCMagazineVisualsComponent> ent, ref GunShotEvent args)
    {
        UpdateMagazine(ent);
    }

    private void OnMagazineSlotInserted(Entity<RMCMagazineVisualsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateMagazine(ent);
    }

    private void OnMagazineSlotRemoved(Entity<RMCMagazineVisualsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateMagazine(ent);
    }

    public void UpdateMagazine(EntityUid uid)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var ammoCountEvent = new GetAmmoCountEvent();
        RaiseLocalEvent(uid, ref ammoCountEvent);

        _appearance.SetData(uid, RMCMagazineVisuals.SlideOpen, ammoCountEvent.Count <= 0, appearance);
    }
}
