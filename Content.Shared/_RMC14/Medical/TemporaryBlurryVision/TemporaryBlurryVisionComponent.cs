using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.TemporaryBlurryVision;

/// <summary>
/// Component used for the blurry vision status effect.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class TemporaryBlurryVisionComponent : Component
{
    [ViewVariables, Access(typeof(TemporaryBlurryVisionSystem))]
    public List<TemporaryBlurModificator> TemporaryBlurModificators = [];
}

[DataDefinition]
public sealed partial class TemporaryBlurModificator
{
    public TimeSpan Duration;
    public int EffectStrength;

    public TemporaryBlurModificator(TimeSpan duration, int strength)
    {
        Duration = duration;
        EffectStrength = strength;
    }
}
