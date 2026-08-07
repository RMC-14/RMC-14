using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo;

public sealed class RMCEmptyMagSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCEmptyMagComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(SharedGunSystem) });
    }

    private void OnMapInit(Entity<RMCEmptyMagComponent> ent, ref MapInitEvent args)
    {
        if (TryComp(ent, out BallisticAmmoProviderComponent? ballistic))
            _gun.SetBallisticUnspawned((ent, ballistic), 0);
    }
}
