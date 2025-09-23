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

        if (ent.Comp.GiveIcon == null)
            return;

        args.Icon = ent.Comp.GiveIcon;
    }

    private void OnGetSquadTitle(Entity<RMCVendorRoleOverrideComponent> ent, ref GetMarineSquadNameEvent args)
    {
        if (ent.Comp.GiveSquadRoleName == null)
            return;

        if (ent.Comp.IsAppendSquadRoleName)
            args.RoleName = $"{args.RoleName} {Loc.GetString(ent.Comp.GiveSquadRoleName)}";
        else
            args.RoleName = Loc.GetString(ent.Comp.GiveSquadRoleName);
    }
}
