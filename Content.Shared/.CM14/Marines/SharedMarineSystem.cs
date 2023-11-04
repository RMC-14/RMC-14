using Content.Shared.CM14.Marines.Squads;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.CM14.Marines;

public abstract class SharedMarineSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineComponent, GetMarineIconEvent>(OnMarineGetIcon,
            after: new[] { typeof(SquadSystem) });
    }

    private void OnMarineGetIcon(Entity<MarineComponent> ent, ref GetMarineIconEvent args)
    {
        if (_minds.TryGetMind(ent, out var mindId, out _) &&
            _jobs.MindTryGetJob(mindId, out _, out var job) &&
            _prototypes.TryIndex(job.Icon, out StatusIconPrototype? icon))
        {
            args.Icons.Add(icon.Icon);
        }
    }

    public void GetMarineIcons(EntityUid uid, List<SpriteSpecifier> icons)
    {
        var ev = new GetMarineIconEvent(icons);
        RaiseLocalEvent(uid, ref ev);
    }
}
