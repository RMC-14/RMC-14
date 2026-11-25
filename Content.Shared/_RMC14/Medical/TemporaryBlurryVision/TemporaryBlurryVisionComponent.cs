using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.TemporaryBlurryVision;

/// <summary>
/// Component used for the blurry vision status effect.
/// </summary>
[Access(typeof(TemporaryBlurryVisionSystem))]
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TemporaryBlurryVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime = new(0);

    [DataField, AutoNetworkedField]
    public List<TemporaryBlurModifier> TemporaryBlurModifiers = [];
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class TemporaryBlurModifier
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ExpireAt;
    public int EffectStrength;

    public TemporaryBlurModifier(TimeSpan expireAt, int strength)
    {
        ExpireAt = expireAt;
        EffectStrength = strength;
    }
}
