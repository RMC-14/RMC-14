using Content.Shared.Foldable;

namespace Content.Shared._RMC14.Folded;

public sealed class RMCFoldableSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FoldableComponent, FoldAttemptEvent>(OnFolded);
    }

    private void OnFolded(Entity<FoldableComponent> ent, ref FoldAttemptEvent args)
    {
        if (!ent.Comp.AnchorOnUnfold || args.Cancelled)
            return;

        if (args.Comp.IsFolded)
            _transform.AnchorEntity(ent);
        else
        {
            _transform.Unanchor(ent);
        }
    }
}
