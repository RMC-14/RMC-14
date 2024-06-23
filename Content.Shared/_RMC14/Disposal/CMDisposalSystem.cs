using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Disposal;

public sealed class CMDisposalSystem : EntitySystem
{
    [Dependency] private readonly SharedDisposalUnitSystem _disposal = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UndisposableComponent, ContainerGettingInsertedAttemptEvent>(OnUndisposableInsertedAttempt);
    }

    private void OnUndisposableInsertedAttempt(Entity<UndisposableComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        SharedDisposalUnitComponent? unit = null;
        if (_disposal.ResolveDisposals(args.Container.Owner, ref unit) &&
            args.Container.ID == unit.Container.ID)
        {
            args.Cancel();
        }
    }
}
