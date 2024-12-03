using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Examine;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMExamineSystem))]
public sealed partial class BlockIdExamineComponent : Component {}
