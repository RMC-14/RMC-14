namespace Content.Shared._CM14.Marines.Squads;

public sealed class SquadSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SquadMemberComponent, GetMarineIconEvent>(OnSquadRoleGetIcon,
            before: new[] { typeof(SharedMarineSystem) });
    }

    private void OnSquadRoleGetIcon(Entity<SquadMemberComponent> ent, ref GetMarineIconEvent args)
    {
        args.Icons.Add(ent.Comp.Background);
    }

    public void SetSquad(EntityUid marine, Entity<SquadTeamComponent?> team)
    {
        if (!Resolve(team, ref team.Comp))
            return;

        var member = EnsureComp<SquadMemberComponent>(marine);
        member.Squad = team;
        member.Background = team.Comp.Background;

        Dirty(marine, member);
    }
}
