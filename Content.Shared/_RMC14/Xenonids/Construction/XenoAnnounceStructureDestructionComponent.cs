using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction;
/// <summary>
/// Announces to the hive when a certain structure is destroyed in the following format (by default)
/// "A [structureName] has been [destructionVerb] at [location]!"
/// [structureName] defaults to the name of the prototype of the entity, but may be overriden but the (StructureName) field in this component
/// [destructionVerb] defaults to "destroyed"
/// [location] is the location that the entity is in; if it is not in an location, default to "Unknown"
/// </summary>
[RegisterComponent]
public sealed partial class XenoAnnounceStructureDestructionComponent : Component
{
    [DataField]
    public LocId MessageID = "rmc-xeno-construction-structure-destroyed";

    [DataField]
    public string? StructureName;

    [DataField]
    public string DestructionVerb = "destroyed";

    [DataField]
    public Color MessageColor = Color.FromHex("#2A623D");
}
