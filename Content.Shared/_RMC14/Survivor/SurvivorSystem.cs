using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.EntityPreset;
using Content.Shared.GameTicking;

namespace Content.Shared._RMC14.Survivor;

public sealed class SurvivorSystem : EntitySystem
{
    [Dependency] private readonly EntityPresetSystem _preset = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EquipSurvivorPresetComponent, PlayerSpawnCompleteEvent>(OnPresetPlayerSpawnComplete, after: [typeof(CMArmorSystem)]);
    }

    private void OnPresetPlayerSpawnComplete(Entity<EquipSurvivorPresetComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        _preset.ApplyPreset(ent, ent.Comp.Preset);
    }
}
