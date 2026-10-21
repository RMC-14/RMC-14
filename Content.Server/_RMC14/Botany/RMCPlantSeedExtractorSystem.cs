using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Botany;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

public sealed class RMCPlantSeedExtractorSystem : EntitySystem
{
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSeedExtractorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<RMCSeedExtractorComponent> ent, ref InteractUsingEvent args)
    {
        var (uid, seedExtractor) = ent;
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!TryComp(args.Used, out RMCProduceComponent? produce))
            return;

        args.Handled = true;

        if (!TryComp(args.Used, out RMCPlantHarvestComponent? harvest) || harvest.Seedless)
        {
            _popup.PopupCursor(Loc.GetString("seed-extractor-component-no-seeds", ("name", args.Used)),
                args.User, PopupType.MediumCaution);
            return;
        }

        _popup.PopupCursor(Loc.GetString("seed-extractor-component-interact-message", ("name", args.Used)),
            args.User, PopupType.Medium);

        args.Handled = true;

        var amount = _random.Next(seedExtractor.BaseMinSeeds, seedExtractor.BaseMaxSeeds + 1);
        var coords = Transform(uid).Coordinates;

        for (var i = 0; i < amount; i++)
        {
            _plantSeed.SpawnSeedPacket(args.Used, coords, args.User, null);
        }

        QueueDel(args.Used);
    }
}
