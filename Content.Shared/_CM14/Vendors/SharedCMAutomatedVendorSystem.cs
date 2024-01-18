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
        var playerName = args.Session.Name;
        if (args.Section < 0 || args.Section >= sections)
        {
            Log.Error($"Player {playerName} sent an invalid vend section: {args.Section}. Max: {sections}");
            return;
        }

        var section = vendor.Comp.Sections[args.Section];
        var entries = section.Entries.Count;
        if (args.Entry < 0 || args.Entry >= entries)
        {
            Log.Error($"Player {playerName} sent an invalid vend entry: {args.Entry}. Max: {entries}");
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

        var playerEnt = args.Session.AttachedEntity;
        var user = CompOrNull<CMVendorUserComponent>(playerEnt);
        if (section.Choices is { } choices)
        {
            if (playerEnt == null)
            {
                Log.Error($"Player {playerName} tried to buy {entry.Id} without an attached entity.");
                return;
            }

            user = EnsureComp<CMVendorUserComponent>(playerEnt.Value);
            if (!user.Choices.TryGetValue(choices.Id, out var playerChoices))
            {
                playerChoices = 0;
                user.Choices[choices.Id] = playerChoices;
            }

            if (playerChoices >= choices.Amount)
            {
                Log.Error($"Player {playerName} tried to buy too many choices.");
                return;
            }

            user.Choices[choices.Id] = ++playerChoices;
        }

        if (entry.Points != null)
        {
            if (playerEnt == null || user == null)
            {
                Log.Error($"Player {playerName} tried to buy {entry.Id} for {entry.Points} points without having points.");
                return;
            }

            if (user.Points < entry.Points)
            {
                Log.Error($"Player {playerName} with {user.Points} tried to buy {entry.Id} for {entry.Points} points without having enough points.");
                return;
            }

            user.Points -= entry.Points.Value;
            Dirty(playerEnt.Value, user);
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
