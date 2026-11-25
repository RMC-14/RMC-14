using Content.Shared.Actions;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Mobs;

public abstract partial class SharedCMGhostSystem : EntitySystem
{
    [Dependency] protected SharedActionsSystem Actions = default!;
    [Dependency] protected IConfigurationManager Config = default!;
    [Dependency] protected IGameTiming GameTiming = default!;

    protected void SetPostDeathChatMutedUntil(Entity<CMGhostComponent?> ent, TimeSpan? value)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.PostDeathChatMutedUntil == value)
            return;

        ent.Comp.PostDeathChatMutedUntil = value;
        Dirty(ent);
    }
}
