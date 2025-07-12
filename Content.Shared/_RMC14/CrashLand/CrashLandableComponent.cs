using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.CrashLand;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
[Access(typeof(SharedCrashLandSystem))]
public sealed partial class CrashLandableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float CrashDuration = 0.75f;

    [DataField, AutoNetworkedField]
    public float FallHeight = 20;

    [DataField, AutoNetworkedField]
    public SoundSpecifier CrashSound = new SoundPathSpecifier("/Audio/Weapons/punch1.ogg");

    [DataField, AutoNetworkedField]
    public TimeSpan? LastCrash;
}
