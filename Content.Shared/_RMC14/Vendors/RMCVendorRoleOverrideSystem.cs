using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;

namespace Content.Shared._RMC14.Vendors;

public sealed class VendorRoleOverrideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCVendorRoleOverrideComponent, GetMarineIconEvent>(OnGetMarineIcon, after: [typeof(SharedMarineSystem), typeof(SquadSystem)]);
        SubscribeLocalEvent<RMCVendorRoleOverrideComponent, GetMarineSquadNameEvent>(OnGetSquadTitle, after: [typeof(SquadSystem)]);
    }

    private void OnGetMarineIcon(Entity<RMCVendorRoleOverrideComponent> ent, ref GetMarineIconEvent args)
    {
        if (HasComp<SquadLeaderComponent>(ent))
            return;

        if (ent.Comp.Icon == null)
            return;

        args.Icon = ent.Comp.Icon;
    }

    private void OnGetSquadTitle(Entity<RMCVendorRoleOverrideComponent> ent, ref GetMarineSquadNameEvent args)
    {
        if (ent.Comp.RoleName == null)
            return;

        if (ent.Comp.IsAppendTitle)
            args.RoleName = $"{args.RoleName} {Loc.GetString(ent.Comp.RoleName)}";
        else
            args.RoleName = Loc.GetString(ent.Comp.RoleName);
    }
}