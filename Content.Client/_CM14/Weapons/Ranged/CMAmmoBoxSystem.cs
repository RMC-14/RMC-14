using Content.Shared._CM14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._CM14.Weapons.Ranged;

public sealed class CMAmmoBoxSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMAmmoBoxComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CMAmmoBoxComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnStartup(Entity<CMAmmoBoxComponent> box, ref ComponentStartup args)
    {
        if (!TryComp(box, out SpriteComponent? sprite) ||
            !TryComp(box, out AppearanceComponent? appearance))
        {
            return;
        }

        UpdateAppearance((box, box, sprite, appearance));
    }

    private void OnAppearanceChange(Entity<CMAmmoBoxComponent> box, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateAppearance( (box, box, args.Sprite, args.Component));
    }

    private void UpdateAppearance(Entity<CMAmmoBoxComponent, SpriteComponent, AppearanceComponent> box)
    {
        _appearance.TryGetData(box, AmmoVisuals.AmmoCount, out int count, box);
        box.Comp2.LayerSetVisible(box.Comp1.AmmoLayer, count > 0);
    }
}
