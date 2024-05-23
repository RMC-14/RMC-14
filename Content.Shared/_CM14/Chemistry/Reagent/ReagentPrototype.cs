using Content.Shared._CM14.Prototypes;

// ReSharper disable CheckNamespace

namespace Content.Shared.Chemistry.Reagent
{
    public sealed partial class ReagentPrototype : ICMSpecific
    {
        [DataField]
        public bool IsCM { get; set; }
    }
}
