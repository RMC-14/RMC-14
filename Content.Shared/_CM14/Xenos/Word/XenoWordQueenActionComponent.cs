using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Word;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoWordQueenSystem))]
public sealed partial class XenoWordQueenActionComponent : Component;
