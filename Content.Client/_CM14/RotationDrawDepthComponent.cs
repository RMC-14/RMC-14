using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client._CM14;

[RegisterComponent]
public sealed partial class RotationDrawDepthComponent : Component
{
    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepth>))]
    public int DefaultDrawDepth;

    [DataField(customTypeSerializer: typeof(ConstantSerializer<DrawDepth>))]
    public int SouthDrawDepth;
}
