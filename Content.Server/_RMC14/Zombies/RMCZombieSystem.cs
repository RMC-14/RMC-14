using Content.Server.Zombies;
using Content.Server.Speech.Components;
using Content.Shared.Zombies;
using Content.Shared.NPC.Systems;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Marines.Orders;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;

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

        EnsureComp<NightVisionComponent>(target);
        _faction.AddFaction(target, "RMCDumb");

        var accentType = "RMCZombie";
        if (TryComp<ZombieAccentOverrideComponent>(target, out var accent))
            accentType = accent.Accent;

        EnsureComp<ReplacementAccentComponent>(target).Accent = accentType;
    }
}
