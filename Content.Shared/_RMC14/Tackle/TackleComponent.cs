using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TackleSystem))]
public sealed partial class TackleComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Min = 2;

    [DataField, AutoNetworkedField]
    public int Max = 6;

    [DataField, AutoNetworkedField]
    public float Chance = 0.35f;

    [DataField, AutoNetworkedField]
    public TimeSpan StunMin = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan StunMax = TimeSpan.FromSeconds(3);
}
