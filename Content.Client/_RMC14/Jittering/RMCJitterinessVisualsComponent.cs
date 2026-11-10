using System.Numerics;

namespace Content.Client._RMC14.Jittering;

[RegisterComponent]
[Access(typeof(RMCJitterinessVisualsSystem))]
public sealed partial class RMCJitterinessVisualsComponent : Component
{
    [ViewVariables]
    public Vector2 Applied;

    [ViewVariables]
    public Vector2 Expected;

    [ViewVariables]
    public Vector2 From;

    [ViewVariables]
    public Vector2 To;

    [ViewVariables]
    public float Progress;
}
