using Content.Shared._RMC14.Barricade.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._RMC14.Construction.Conditions;

/// <summary>
/// Checks that the structure is barbed
/// </summary>
[DataDefinition]
public sealed partial class IsBarbed : IGraphCondition
{
    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        return (entMan.TryGetComponent(uid, out BarbedComponent? barbedComp) &&
            barbedComp.IsBarbed);
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var entity = args.Examined;

        if (Condition(entity, entMan))
        {
            return false;
        }
        args.PushMarkup(Loc.GetString("construction-examine-condition-missing-barbed"));

        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-condition-missing-barbed"
        };
    }
}
