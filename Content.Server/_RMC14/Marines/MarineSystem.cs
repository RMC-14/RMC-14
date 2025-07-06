using Content.Shared._RMC14.Marines;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Marines;

public sealed class MarineSystem : SharedMarineSystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(Entity<MarineComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        if (args.JobId is not { } jobId)
            return;

        if (!_prototypes.TryIndex<JobPrototype>(jobId, out var job) || !job.IsCM)
            return;

        SpriteSpecifier? icon = null;
        if (job.HasIcon && _prototypes.TryIndex(job.Icon, out var jobIcon))
            icon = jobIcon.Icon;

        MakeMarine(args.Mob, icon);
    }
}
