using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Vendors;

public abstract class SharedCMAutomatedVendorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMAutomatedVendorComponent, CMVendorVendBuiMessage>(OnVendBui);
    }

    protected virtual void OnVendBui(Entity<CMAutomatedVendorComponent> vendor, ref CMVendorVendBuiMessage args)
    {
        var sections = vendor.Comp.Sections.Count;
        if (args.Section < 0 || args.Section >= sections)
        {
            Log.Error($"Player {args.Session.Name} sent an invalid vend section: {args.Section}. Max: {sections}");
            return;
        }

        var section = vendor.Comp.Sections[args.Section];
        var entries = section.Entries.Count;
        if (args.Entry < 0 || args.Entry >= entries)
        {
            Log.Error($"Player {args.Session.Name} sent an invalid vend entry: {args.Entry}. Max: {entries}");
            return;
        }

        var entry = section.Entries[args.Entry];
        if (entry.Amount is <= 0)
            return;

        if (!_prototypes.TryIndex(entry.Id, out var entity))
        {
            Log.Error($"Tried to vend non-existent entity: {entry.Id}");
            return;
        }

        if (entry.Points != null)
        {
            if (!TryComp(args.Session.AttachedEntity, out CMMarinePointsComponent? points))
            {
                Log.Error($"Player {args.Session.Name} tried to buy {entry.Id} for {entry.Points} points without having points.");
                return;
            }

            if (points.Points < entry.Points)
            {
                Log.Error($"Player {args.Session.Name} with {points.Points} tried to buy {entry.Id} for {entry.Points} points without having enough points.");
                return;
            }

            points.Points -= entry.Points.Value;
            Dirty(args.Session.AttachedEntity.Value, points);
        }

        if (entry.Amount != null)
        {
            entry.Amount--;
            Dirty(vendor);
        }

        if (entity.TryGetComponent(out CMVendorBundleComponent? bundle))
        {
            foreach (var bundled in bundle.Bundle)
            {
                SpawnNextToOrDrop(bundled, vendor);
            }
        }
        else
        {
            SpawnNextToOrDrop(entry.Id, vendor);
        }
    }
}
