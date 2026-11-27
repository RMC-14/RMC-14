using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Components
{
    /// <summary>
    /// Used for construction graphs in building computers.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ComputerBoardComponent : Component
    {
        [DataField("prototype")]
        public EntProtoId? Prototype { get; private set; }
    }
}
