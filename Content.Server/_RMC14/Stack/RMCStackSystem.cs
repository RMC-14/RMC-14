using Content.Server.Stack;
using Content.Shared._RMC14.Stack;
using Content.Shared.Stacks;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Stack;

public sealed class RMCStackSystem : SharedRMCStackSystem
{
    [Dependency] private readonly StackSystem _stack = default!;

    public override EntityUid? Split(Entity<StackComponent?> stack, int amount, EntityCoordinates spawnPosition)
    {
        return _stack.Split(stack, amount, spawnPosition, stack);
    }
}
