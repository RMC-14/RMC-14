using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Components;

namespace Content.Server._RMC14.Light;

public sealed class RMCLightOutSystem : SharedRMCLightOutSystem
{
    [Dependency] private readonly ExpendableLightSystem _expend = default!;

    protected override void ExtinguishFlare(EntityUid ent)
    {
        if (!TryComp<ExpendableLightComponent>(ent, out var flare))
            return;

        //Make it burn out quick with no light
        flare.StateExpiryTime = 0;
        flare.GlowDuration = TimeSpan.FromSeconds(0);
        flare.PhaseOneDuration = TimeSpan.FromSeconds(0);
        flare.PhaseTwoDuration = TimeSpan.FromSeconds(0);
        flare.PhaseThreeDuration = TimeSpan.FromSeconds(0);
        flare.PhaseFourDuration = TimeSpan.FromSeconds(0);
        flare.PhaseFiveDuration = TimeSpan.FromSeconds(0);
        flare.FadeOutDuration = TimeSpan.FromSeconds(0);

        _expend.TryActivate((ent, flare));
        Dirty(ent, flare);
    }
}
