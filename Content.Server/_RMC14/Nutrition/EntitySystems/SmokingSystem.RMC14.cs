// ReSharper disable CheckNamespace

using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;

namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        private void InitializeRMC()
        {
            SubscribeLocalEvent<SmokableComponent, ActivateInWorldEvent>(OnCigaretteActivatedEvent);
        }

        private void OnCigaretteActivatedEvent(Entity<SmokableComponent> entity, ref ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!TryComp(entity, out SmokableComponent? smokable))
                return;

            if (smokable.State != SmokableState.Lit)
                return;

            SetSmokableState(entity, SmokableState.Burnt, smokable);
            args.Handled = true;
        }
    }
}

