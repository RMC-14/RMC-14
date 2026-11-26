namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapGrantedComponent : Component
{
    [ViewVariables]
    public bool User;

    [ViewVariables]
    public bool Tracked;
}
