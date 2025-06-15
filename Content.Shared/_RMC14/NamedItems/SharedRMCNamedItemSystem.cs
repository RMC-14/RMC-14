using Content.Shared.Verbs;

namespace Content.Shared._RMC14.NamedItems;

public abstract class SharedRMCNamedItemSystem : EntitySystem
{
    public static readonly int TypeCount = Enum.GetValues<RMCNamedItemType>().Length;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCNameItemOnVendComponent, GetVerbsEvent<AlternativeVerb>>(OnNameItemGetVerbs);
    }

    private void OnNameItemGetVerbs(Entity<RMCNameItemOnVendComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp(args.User, out RMCUserNamedItemsComponent? named))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Reapply custom name",
            Act = () =>
            {
                TryNameItem((user, named), ent, ent.Comp.Item);
            },
            Priority = -100,
        });
    }

    protected virtual bool TryNameItem(Entity<RMCUserNamedItemsComponent> user, EntityUid item, RMCNamedItemType type)
    {
        return false;
    }
}
