using Content.Shared.Stacks;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Stack;

public abstract class SharedRMCStackSystem : EntitySystem
{
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public virtual EntityUid? Split(Entity<StackComponent?> stack, int amount, EntityCoordinates spawnPosition)
    {
        _stack.Use(stack, amount);
        return null;
    }
}
