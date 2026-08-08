using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Projectiles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Mutiny;

public abstract class SharedMutinySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MutinyParticipantComponent, GetMarineIconEvent>(OnGetMarineIcon,
            after: [typeof(SquadSystem)]);
        SubscribeLocalEvent<ProjectileIFFAddedEvent>(OnProjectileIffAdded);
        SubscribeLocalEvent<MutinyProjectileComponent, ProjectileIFFCheckEvent>(OnProjectileIffCheck);
    }

    private void OnGetMarineIcon(Entity<MutinyParticipantComponent> participant, ref GetMarineIconEvent args)
    {
        if (TryComp(participant, out MutineerLeaderComponent? leader) && leader.Active)
        {
            args.Icon = leader.Icon;
            return;
        }

        args.Icon = participant.Comp.Side switch
        {
            MutinySide.Mutineer => participant.Comp.MutineerIcon,
            MutinySide.Loyalist => participant.Comp.LoyalistIcon,
            MutinySide.NonCombatant => participant.Comp.NonCombatantIcon,
            _ => args.Icon,
        };
    }

    private void OnProjectileIffAdded(ref ProjectileIFFAddedEvent args)
    {
        EntityUid rule;
        MutinySide side;
        EntProtoId<IFFFactionComponent> iffFaction;

        if (TryComp(args.Source, out MutinyParticipantComponent? participant))
        {
            rule = participant.Rule;
            side = participant.Side;
            iffFaction = participant.IffFaction;
        }
        else if (TryComp(args.Source, out MutinyProjectileComponent? projectile))
        {
            rule = projectile.Rule;
            side = projectile.ShooterSide;
            iffFaction = projectile.IffFaction;
        }
        else if (TryComp(args.Projectile, out ProjectileComponent? shot) &&
                 shot.Shooter is { } shooter &&
                 TryComp(shooter, out MutinyParticipantComponent? shooterParticipant))
        {
            rule = shooterParticipant.Rule;
            side = shooterParticipant.Side;
            iffFaction = shooterParticipant.IffFaction;
        }
        else
        {
            return;
        }

        var mutinyProjectile = EnsureComp<MutinyProjectileComponent>(args.Projectile);
        mutinyProjectile.Rule = rule;
        mutinyProjectile.ShooterSide = side;
        mutinyProjectile.IffFaction = iffFaction;
        Dirty(args.Projectile, mutinyProjectile);
    }

    private void OnProjectileIffCheck(Entity<MutinyProjectileComponent> projectile, ref ProjectileIFFCheckEvent args)
    {
        if (!args.IffEnabled ||
            args.Faction != projectile.Comp.IffFaction ||
            !TryComp(args.Target, out MutinyParticipantComponent? target) ||
            target.Rule != projectile.Comp.Rule)
        {
            return;
        }

        args.IgnoreProtection =
            projectile.Comp.ShooterSide == MutinySide.Mutineer && target.Side == MutinySide.Loyalist ||
            projectile.Comp.ShooterSide == MutinySide.Loyalist && target.Side == MutinySide.Mutineer;
    }
}
