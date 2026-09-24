using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.RMCMedicalRecords;
/* TODO RMC14 Medical Records Console
/// <summary>
///     Medical record stored in the station record set.
///     Created automatically when a general record is created.
/// </summary>
/// <remarks>
///     Static medical data (blood type, disabilities, etc.) lives here.
///     Dynamic scan/autodoc data lives in <see cref="RMCLastBodyScanResultComponent"/> directly
///     on the entity, since the body scanner and autodoc need direct entity access.
/// </remarks>
[Serializable, NetSerializable, DataRecord]
public sealed partial record RMCMedicalRecord
{
    // TODO RMC-14 actual blood types — update when blood type system is implemented
    [DataField]
    public string BloodType = "O-";

    [DataField]
    public string MinorDisability = "None";

    [DataField]
    public string MinorDisabilityDetails = "No minor disabilities have been declared.";

    [DataField]
    public string MajorDisability = "None";

    [DataField]
    public string MajorDisabilityDetails = "No major disabilities have been diagnosed.";

    [DataField]
    public string Allergies = "None";

    [DataField]
    public string AllergiesDetails = "No allergies have been detected in this patient.";

    [DataField]
    public string Diseases = "None";

    [DataField]
    public string DiseasesDetails = "No diseases have been diagnosed at the moment.";

    [DataField]
    public List<RMCMedicalComment> Comments = [];
}

[Serializable, NetSerializable]
public record struct RMCMedicalComment(TimeSpan AddTime, string Comment, string? AuthorName);
*/
