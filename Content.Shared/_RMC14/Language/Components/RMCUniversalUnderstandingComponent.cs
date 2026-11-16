using Content.Shared._RMC14.Language.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Language.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(RMCUniversalUnderstandingSystem))]
public sealed partial class RMCUniversalUnderstandingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ExpiresAt;
}
