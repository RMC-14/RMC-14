using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Word;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoWordQueenSystem))]
public sealed partial class XenoWordQueenActionComponent : Component;
