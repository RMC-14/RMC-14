using Content.Shared._RMC14.Weapons.Ranged.Ammo;
using Content.Shared.Damage;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class BulletholeSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    // Bullethole overlays
    private const int BulletholeStates = 1;
    private const int MaxBulletholeCount = 8;
    private const string BulletholeRsiPath = "/Textures/_RMC14/Effects/bullethole.rsi";

    public override void Initialize()
    {
        SubscribeLocalEvent<BulletholeComponent, DamageChangedEvent>(OnVisualsDamageChangedEvent);
    }

    private void OnVisualsDamageChangedEvent(Entity<BulletholeComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp(args.Tool, out BulletholeGeneratorComponent? bulletholeGeneratorComponent))
            return;

        ent.Comp.BulletholeCount++;

        if (!TryComp<AppearanceComponent>(ent, out var app))
            return;

        var stateString = $"bhole_reference_{(ent.Comp.BulletholeCount >= 8 ? 8 : ent.Comp.BulletholeCount):00}";
        _appearance.SetData(ent, BulletholeVisuals.State, stateString, app);
    }
}
