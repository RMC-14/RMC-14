using Content.Shared.Disposal.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Disposal;

public sealed class CMDisposalSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<UndisposableComponent, ContainerGettingInsertedAttemptEvent>(OnUndisposableInsertedAttempt);
    }

    private void OnUndisposableInsertedAttempt(Entity<UndisposableComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (TryComp(args.Container.Owner, out DisposalUnitComponent? unit) &&
            args.Container.ID == unit.Container.ID)
        {
            args.Cancel();
        }
    }
}
