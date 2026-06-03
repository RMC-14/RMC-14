using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCRodentBehaviorComponent : Component
{
    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public float SleepChance = 0.005f;

    [DataField]
    public float WakeChance = 0.01f;

    [DataField]
    public TimeSpan SleepDurationMin = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan SleepDurationMax = TimeSpan.FromSeconds(60);

    [DataField]
    public float SnuffleChance = 0.05f;

    [DataField]
    public TimeSpan SnuffleCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public float SqueakOnCollideChance = 0.05f;

    [DataField]
    public TimeSpan SqueakCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier SqueakSound = new SoundPathSpecifier("/Audio/Animals/mouse_squeak.ogg", AudioParams.Default.WithVolume(-3));

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [ViewVariables]
    public TimeSpan SleepUntil;

    [ViewVariables]
    public TimeSpan NextSnuffleAt;

    [ViewVariables]
    public TimeSpan NextSqueakAt;

    [ViewVariables]
    public bool Sleeping;
}
