using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoParasiteSystem))]

public sealed partial class ParasiteAIDelayAddComponent : Component
{
    [DataField]
    public TimeSpan DelayTime = TimeSpan.FromSeconds(420);

    [DataField]
    public TimeSpan TimeToAI;
}
