namespace Content.Shared._RMC14.RMCMedicalRecords;

/// <summary>
///     Identifiers used to bridge body scanner results with the autodoc's import feature.
/// </summary>
/// <remarks>These <see cref="RMCAutodocScanData.Procedure"/> values must match between the body scanner and the autodoc import scan.</remarks>
public static class RMCAutodocProcedures
{
    public const string Brute = "brute";
    public const string Burn = "burn";
    public const string CloseIncisions = "close_incisions";
    public const string RemoveShrapnel = "shrapnel";
    public const string Blood = "blood";
    public const string Dialysis = "dialysis";
    public const string Toxin = "toxin";
    public const string InternalBleeding = "internal_bleeding";
    public const string BrokenBone = "broken_bone";
    public const string OrganDamage = "organ_damage";
    public const string Larva = "larva";
}
