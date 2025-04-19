using Content.Shared._RMC14.IconLabel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._RMC14.IconLabel;

public sealed partial class RMCIconLabelSystem : SharedRMCIconLabelSystem
{
    public void SetText(Entity<IconLabelComponent> ent, LocId newLocId, params (string, object)[] newParams)
    {
        ent.Comp.LabelTextLocId = newLocId;
        ent.Comp.LabelTextParams = new List<(string, object)>(newParams);
        Dirty(ent);
    }
}
