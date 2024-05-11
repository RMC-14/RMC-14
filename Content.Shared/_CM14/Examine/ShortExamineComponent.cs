using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Examine;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMExamineSystem))]
public sealed partial class ShortExamineComponent : Component;
