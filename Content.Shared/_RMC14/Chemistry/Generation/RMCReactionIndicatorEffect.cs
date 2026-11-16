using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Generation;

public sealed partial class RMCReactionIndicatorEffect : EventEntityEffect<RMCReactionIndicatorEffect>
{
    [DataField(required: true)]
    public ReactionIndicator Indicator;

    [DataField]
    public float BubblingRange = 1.5f;

    [DataField]
    public FixedPoint2 SplashAmount = 5;

    [DataField]
    public int FireRange = 1;

    [DataField]
    public TimeSpan EndothermicDuration = TimeSpan.FromSeconds(2);

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}
