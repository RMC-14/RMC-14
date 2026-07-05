using Content.Server.Light.EntitySystems;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Marines;
using Content.Shared.Hands.Components;
using Content.Shared.Light.Components;
using Prometheus.DotNetRuntime.EventListening;

namespace Content.Server._RMC14.Light;

public sealed class RMCLightOutSystem : SharedRMCLightOutSystem
{
    [Dependency] private readonly ExpendableLightSystem _expend = default!;

    protected override void TurnOffLights(Entity<RMCLightsOutOnDeathComponent> ent)
    {
        base.TurnOffLights(ent);

        //Recheck flares only

        var entsToCheck = new HashSet<EntityUid>();

        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            entsToCheck.Add(held);
        }

        var slots = _inventory.GetSlotEnumerator(ent.Owner);

        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } contained)
                entsToCheck.Add(contained);
        }

        foreach (var invEnt in entsToCheck)
        {
            if (TryComp<ExpendableLightComponent>(invEnt, out var flare))
            {
                //Make it burn out quick with no light
                flare.StateExpiryTime = 0;
                flare.GlowDuration = TimeSpan.FromSeconds(0);
                flare.PhaseOneDuration = TimeSpan.FromSeconds(0);
                flare.PhaseTwoDuration = TimeSpan.FromSeconds(0);
                flare.PhaseThreeDuration = TimeSpan.FromSeconds(0);
                flare.PhaseFourDuration = TimeSpan.FromSeconds(0);
                flare.PhaseFiveDuration = TimeSpan.FromSeconds(0);
                flare.FadeOutDuration = TimeSpan.FromSeconds(0);

                _expend.TryActivate((invEnt, flare));
                Dirty(invEnt, flare);
            }
        }
    }
}
