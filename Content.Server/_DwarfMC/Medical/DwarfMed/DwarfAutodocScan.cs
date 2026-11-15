using System.Linq;
using Content.Shared._DwarfMC.Medical.DwarfMed;
using Content.Shared._RMC14.RMCMedicalRecords;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.Server._DwarfMC.Medical.DwarfMed;

public static class DwarfAutodocScan
{
    public static void Append(EntityUid target, List<RMCAutodocScanData> autodocData, IEntityManager entities)
    {
        var scans = entities.System<DwarfBodyPartScanSystem>();

        var parts = scans.Build(target);
        if (parts.Any(part => part.InternalBleeding))
            autodocData.Add(new RMCAutodocScanData(RMCAutodocProcedures.InternalBleeding, Loc.GetString("rmc-records-autodoc-internal-bleeding")));

        if (parts.Any(part => part.Fractured))
            autodocData.Add(new RMCAutodocScanData(RMCAutodocProcedures.BrokenBone, Loc.GetString("rmc-records-autodoc-broken-bone")));

        var organs = scans.BuildOrgans(target);
        if (organs.Any(organ => organ.Damage > FixedPoint2.Zero || organ.Ruptured))
            autodocData.Add(new RMCAutodocScanData(RMCAutodocProcedures.OrganDamage, Loc.GetString("rmc-records-autodoc-organ-damage")));
    }
}
