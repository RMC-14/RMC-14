using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Neurotoxin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NeurotoxinComponent : Component
{
    [DataField, AutoNetworkedField]
    public float NeurotoxinAmount = 0;

    [DataField, AutoNetworkedField]
    public float DepletionPerSecond = 1;

    [DataField, AutoNetworkedField]
    public float StaminaDamagePerSecond = 7;

    [DataField, AutoNetworkedField]
    public TimeSpan DizzyStrength = TimeSpan.FromSeconds(12);

    [DataField, AutoNetworkedField]
    public TimeSpan DizzyStrengthOnStumble = TimeSpan.FromSeconds(55);

    [DataField, AutoNetworkedField]
    public TimeSpan LastMessage;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenMessages = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan AccentTime = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan JitterTime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public TimeSpan StumbleJitterTime = TimeSpan.FromSeconds(25);

    [DataField, AutoNetworkedField]
    public TimeSpan LastStumbleTime;

    [DataField, AutoNetworkedField]
    public TimeSpan BlurTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan BlindTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public TimeSpan DeafenTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan MinimumDelayBetweenEvents = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan LastAccentTime;

    [DataField, AutoNetworkedField]
    public DamageSpecifier ToxinDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier OxygenDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier CoughDamage = new();

    [DataField, AutoNetworkedField]
    public TimeSpan DazeLength = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> CoughId = "Cough";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> PainId = "Scream"; // TODO custom pain emote

    [DataField, AutoNetworkedField]
    public TimeSpan BloodCoughDuration = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan NextGasInjectionAt;
}
