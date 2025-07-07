using System.Linq;

namespace Content.Shared._RMC14.IconLabel;

public abstract class SharedRMCIconLabelSystem : EntitySystem
{
    public void Label(Entity<IconLabelComponent?> ent, LocId newLocId, List<(string, object)> newParams)
    {
        ent.Comp = EnsureComp<IconLabelComponent>(ent);
        ent.Comp.LabelTextLocId = newLocId;
        ent.Comp.LabelTextParams = new List<(string, object)>(newParams);
        Dirty(ent);
    }

    public void Label(Entity<IconLabelComponent?> ent, LocId newLocId, params (string, object)[] newParams)
    {
        Label(ent, newLocId, newParams.ToList());
    }
}
