using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Effects;

[RegisterComponent, NetworkedComponent]
public sealed partial class PreventMetabolismComponent : Component
{
    [DataField]
    public HashSet<string> PreventedReagents = [];
}
