using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSizeStunSystem))]
public sealed partial class RMCStunOnTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 7;

    [DataField, AutoNetworkedField]
    public float MidRange = 5;

    [DataField, AutoNetworkedField]
    public float CloseRange = 2;

    [DataField, AutoNetworkedField]
    public TimeSpan Stun = TimeSpan.FromSeconds(0);

    [DataField, AutoNetworkedField]
    public TimeSpan Paralyze = TimeSpan.FromSeconds(0);

    [DataField, AutoNetworkedField]
    public TimeSpan Deafen = TimeSpan.FromSeconds(0);

    [DataField, AutoNetworkedField]
    public TimeSpan Flash = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan CloseAdditionalStunTime = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan CloseAdditionalParalyzeTime = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public TimeSpan CloseAdditionalDeafenTime = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan MidAdditionalStunTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public TimeSpan MidAdditionalParalyzeTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan MidAdditionalDeafenTime = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public float FlashSlowTo = 0.8f;

    [DataField, AutoNetworkedField]
    public float EarProtectionMultiplier = 0.85f;

    [DataField, AutoNetworkedField]
    public TimeSpan FlashAdditionalStunTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan FlashAdditionalParalyzeTime = TimeSpan.FromSeconds(20);

    [DataField]
    public EntityWhitelist? TrainedWhitelist;
}
