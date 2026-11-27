using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Melee.OmaeWaMouShindeiru;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OmaeWaMouShindeiruSystem))]
public sealed partial class AlreadyDeadTargetComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan TriggerTime;

    [DataField, AutoNetworkedField]
    public int NumberOfCuts;

    [DataField, AutoNetworkedField]
    public bool BypassResistances;

    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public EntityUid User;

    [DataField]
    public SoundSpecifier BoneBreakSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/alien_knockdown.ogg"); //TODO RMC14 Bonebreak sound bone_break1.ogg

    [ViewVariables]
    public EntityUid? AudioStream;
}
