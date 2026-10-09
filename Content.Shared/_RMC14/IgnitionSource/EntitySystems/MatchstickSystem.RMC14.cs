// ReSharper disable CheckNamespace
using Content.Shared.IgnitionSource.Components;
using Content.Shared.Interaction;
using Content.Shared.Smoking;

namespace Content.Shared.IgnitionSource.EntitySystems;

public sealed partial class MatchstickSystem : EntitySystem
{
    private void InitializeRMC14()
    {
        SubscribeLocalEvent<MatchstickComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(Entity<MatchstickComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp(entity, out MatchstickComponent? matchstick))
            return;

        if (matchstick.State != SmokableState.Lit)
            return;

        SetState(entity, SmokableState.Burnt);
        args.Handled = true;
    }
}
