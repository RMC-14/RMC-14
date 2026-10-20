using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Effects.Buildup;

[Access(typeof(RMCKnockdownOnBuildupSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCKnockdownOnBuildupComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool Refresh;
}
