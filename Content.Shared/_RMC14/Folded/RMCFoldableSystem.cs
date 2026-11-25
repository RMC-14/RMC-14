using Content.Shared.Foldable;

namespace Content.Shared._RMC14.Folded;

public sealed partial class RMCFoldableSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

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

    public bool TryLockFold(EntityUid uid, bool locked, FoldableComponent? foldableComp = null)
    {
        if (!Resolve(uid, ref foldableComp, false))
            return false;

        foldableComp.IsLocked = locked;

        return true;
    }
}
