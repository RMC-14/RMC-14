using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

[Serializable, NetSerializable]
public sealed class SolutionComponentState(Solution solution) : ComponentState
{
    public Solution Solution = solution;
}
