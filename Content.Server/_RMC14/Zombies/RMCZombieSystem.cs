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
        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnZombieStartup);
    }

    private void OnZombified(Entity<ZombifyOnDeathComponent> ent, ref EntityZombifiedEvent args)
    {
        var target = ent.Owner;

        RemoveRMCZombieIncompatibleComponents(target, true);
        ApplyRMCZombie(target);
    }

    private void OnZombieStartup(Entity<ZombieComponent> ent, ref ComponentStartup args)
    {
        RemoveRMCZombieIncompatibleComponents(ent.Owner, false);
        ApplyRMCZombie(ent.Owner);
    }

    private void RemoveRMCZombieIncompatibleComponents(EntityUid target, bool stripGhostRole)
    {
        RemComp<MarineOrdersComponent>(target);
        RemComp<ScoutWhitelistComponent>(target);
        RemComp<SniperWhitelistComponent>(target);
        RemComp<PyroWhitelistComponent>(target);
        RemComp<InfectableComponent>(target);

        if (!stripGhostRole)
            return;

        RemComp<GhostRoleComponent>(target);
        RemComp<GhostTakeoverAvailableComponent>(target);
    }

    private void ApplyRMCZombie(EntityUid target)
    {
        EnsureComp<NightVisionComponent>(target);
        _faction.AddFaction(target, "RMCDumb");

        if (TryComp<ZombieComponent>(target, out var zombieComponent))
        {
            zombieComponent.PassiveHealing = new()
            {
                DamageDict = new()
                {
                    { "Blunt", -10 },
                    { "Slash", -10 },
                    { "Piercing", -10 },
                    { "Shock", -2 }
                }
            };
            zombieComponent.HealingOnBite = new()
            {
                DamageDict = new()
                {
                    { "Blunt", -20 },
                    { "Slash", -20 },
                    { "Piercing", -20 }
                }
            };
            zombieComponent.PassiveHealingCritMultiplier = 1.5f;
            zombieComponent.ZombieMovementSpeedDebuff = 0.80f;
        }

        var accentType = "RMCZombie";
        if (TryComp<ZombieAccentOverrideComponent>(target, out var accent))
            accentType = accent.Accent;

        EnsureComp<ReplacementAccentComponent>(target).Accent = accentType;
    }
}
