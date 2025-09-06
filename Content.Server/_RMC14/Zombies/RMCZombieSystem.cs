using Content.Server.Zombies;
using Content.Server.Speech.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Zombies;
using Content.Shared.NPC.Systems;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Marines.Orders;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;
using Content.Shared._RMC14.Xenonids.Parasite;

namespace Content.Server.Zombies;

public sealed partial class RMCZombieSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ZombifyOnDeathComponent, EntityZombifiedEvent>(OnZombified);
    }

    private void OnZombified(Entity<ZombifyOnDeathComponent> ent, ref EntityZombifiedEvent args)
    {
        var target = ent.Owner;

        RemComp<MarineOrdersComponent>(target);
        RemComp<ScoutWhitelistComponent>(target);
        RemComp<SniperWhitelistComponent>(target);
        RemComp<InfectableComponent>(target);
        RemComp<GhostRoleComponent>(target);
        RemComp<GhostTakeoverAvailableComponent>(target);

        EnsureComp<NightVisionComponent>(target);
        _faction.AddFaction(target, "RMCDumb");

        if (TryComp<ZombieComponent>(target, out var zombieComponent))
        {
            zombieComponent.PassiveHealing = new()
            {
                DamageDict = new ()
                {
                    { "Blunt", -10 },
                    { "Slash", -10 },
                    { "Piercing", -10 },
                    { "Shock", -2 }
                }
            };
            zombieComponent.HealingOnBite = new()
            {
                DamageDict = new ()
                {
                    { "Blunt", -20 },
                    { "Slash", -20 },
                    { "Piercing", -20 }
                }
            };
            zombieComponent.PassiveHealingCritMultiplier = 1.5f;
            zombieComponent.ZombieMovementSpeedDebuff = 0.80f;
        };

        var accentType = "RMCZombie";
        if (TryComp<ZombieAccentOverrideComponent>(target, out var accent))
            accentType = accent.Accent;

        EnsureComp<ReplacementAccentComponent>(target).Accent = accentType;
    }
}
