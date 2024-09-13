using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Shared._RMC14.Connection;

public sealed class MindCheckSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MindCheckComponent, MindAddedMessage>(OnMindChanged);
        SubscribeLocalEvent<MindCheckComponent, MindRemovedMessage>(OnMindChanged);
    }

    private void OnMindChanged<T>(Entity<MindCheckComponent> ent, ref T args) where T : MindEvent
    {
        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mindComp) || mindComp.Session == null)
            ent.Comp.HasMindOrGhost = false;
        else
            ent.Comp.HasMindOrGhost = true;

        Dirty(ent);
    }
}
