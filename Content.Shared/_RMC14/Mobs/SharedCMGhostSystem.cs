using Content.Shared.Actions;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Mobs;

public abstract class SharedCMGhostSystem : EntitySystem
{
    [Dependency] protected readonly SharedActionsSystem Actions = default!;
    [Dependency] protected readonly IConfigurationManager Config = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    protected void SetPostDeathChatMutedUntil(Entity<CMGhostComponent?> ent, TimeSpan? value)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.PostDeathChatMutedUntil == value)
            return;

        ent.Comp.PostDeathChatMutedUntil = value;
        Dirty(ent);
    }
}
