using Robust.Shared.Localization;

namespace Content.Shared._RMC14.Requisitions.Components;

/// <summary>
/// Vendor inventory available to supply requests. Independent of automatic dispensing onto a table.
/// </summary>
[RegisterComponent]
public sealed partial class RequisitionsStockSourceComponent : Component
{
    [DataField]
    public LocId Category = "rmc-requisitions-category-gear";
}
