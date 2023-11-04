namespace Content.Shared.CM14.Marines.Squads;

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
        if (!TryComp(ent.Comp.Squad, out SquadTeamComponent? team))
            return;

        args.Icons.Add(team.Background);
    }

    public void SetSquad(EntityUid marine, Entity<SquadTeamComponent?> team)
    {
        if (!Resolve(team, ref team.Comp))
            return;

        var member = EnsureComp<SquadMemberComponent>(marine);
        member.Squad = team;
        Dirty(marine, member);
    }
}
