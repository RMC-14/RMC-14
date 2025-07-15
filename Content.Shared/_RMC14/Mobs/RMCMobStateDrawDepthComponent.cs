using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Mobs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMMobStateSystem))]
public sealed partial class RMCMobStateDrawDepthComponent : Component
{
    [DataField, AutoNetworkedField]
    public DrawDepth.DrawDepth Default = DrawDepth.DrawDepth.Mobs;

    [DataField, AutoNetworkedField]
    public Dictionary<MobState, DrawDepth.DrawDepth> DrawDepths = new()
    {
        [MobState.Dead] = DrawDepth.DrawDepth.DeadMobs,
    };
}
