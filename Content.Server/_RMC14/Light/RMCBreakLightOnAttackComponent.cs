using Robust.Shared.Audio;

namespace Content.Server._RMC14.Light;

[RegisterComponent]
[Access(typeof(RMCLightBulbSystem))]
public sealed partial class RMCBreakLightOnAttackComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("GlassBreak");
}
