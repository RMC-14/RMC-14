using System.Linq;
using Content.Shared._RMC14.Power;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Botany;

public sealed class LysisCentrifugeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LysisCentrifugeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LysisCentrifugeComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<LysisCentrifugeComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<LysisCentrifugeComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<LysisCentrifugeComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<LysisCentrifugeComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        Subs.BuiEvents<LysisCentrifugeComponent>(LysisCentrifugeUiKey.Key, subs =>
        {
            subs.Event<LysisCentrifugeProcessSeedBuiMsg>(OnProcessSeed);
            subs.Event<LysisCentrifugeEjectSeedBuiMsg>(OnEjectSeed);
            subs.Event<LysisCentrifugeExtractGeneBuiMsg>(OnExtractGene);
            subs.Event<LysisCentrifugeClearBufferBuiMsg>(OnClearBuffer);
            subs.Event<LysisCentrifugeEjectDiscBuiMsg>(OnEjectDisc);
        });
    }

    private void OnMapInit(Entity<LysisCentrifugeComponent> ent, ref MapInitEvent args)
    {
        foreach (var type in Enum.GetValues<PlantGeneType>())
        {
            ent.Comp.ObfuscationCodes[type] = $"{_random.Next(0, 256):X2} {_random.Next(0, 256):X2}";
        }

        Dirty(ent);
    }

    private void OnInteractUsing(Entity<LysisCentrifugeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var slotId = GetSlotFor(ent, args.Used);
        if (slotId == null)
            return;

        if (!_power.IsPowered(ent))
        {
            _popup.PopupClient("The centrifuge has no power.", args.User, args.User);
            args.Handled = true;
            return;
        }

        if (_itemSlots.GetItemOrNull(ent, slotId) != null)
        {
            var msg = slotId == ent.Comp.DiscSlotId
                ? "A disc is already loaded. Eject it first via the interface."
                : "A seed packet is already loaded. Eject it first.";
            _popup.PopupClient(msg, args.User, args.User);
            args.Handled = true;
            return;
        }

        if (_net.IsServer && GetServerInsertError(args.Used) is { } error)
        {
            _popup.PopupCursor(error, args.User);
            args.Handled = true;
        }
    }

    private void OnInsertAttempt(Entity<LysisCentrifugeComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_power.IsPowered(ent) ||
            _net.IsServer && GetServerInsertError(args.Item) != null)
        {
            args.Cancelled = true;
        }
    }

    private void OnSlotChanged(Entity<LysisCentrifugeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        SlotChanged(ent, args.Container.ID);
    }

    private void OnSlotChanged(Entity<LysisCentrifugeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        SlotChanged(ent, args.Container.ID);
    }

    private void OnGetVerbs(Entity<LysisCentrifugeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || ent.Comp.GenomeName == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Clear buffer",
            Act = () => TryClearBuffer(ent, user),
        });
    }

    private void OnProcessSeed(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeProcessSeedBuiMsg args)
    {
        if (_net.IsClient)
            return;

        var comp = ent.Comp;
        if (!_power.IsPowered(ent))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.Actor);
            return;
        }

        if (_itemSlots.GetItemOrNull(ent, comp.SeedSlotId) is not { } seed)
        {
            _popup.PopupCursor("No seed packet is loaded.", args.Actor);
            return;
        }

        if (comp.GenomeName != null)
        {
            _popup.PopupCursor("A genome is already loaded. Clear the buffer first.", args.Actor);
            return;
        }

        comp.LoadedSnapshot = _plantSeed.CloneSnapshot(_plantSeed.GetOrCreateSeedSnapshot(seed));
        comp.GenomeName = Name(seed);
        comp.Degradation = 0;
        Dirty(ent);

        QueueDel(seed);
        _audio.PlayPvs(comp.ScanSound, ent);
    }

    private void OnEjectSeed(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeEjectSeedBuiMsg args)
    {
        EjectSlot(ent, ent.Comp.SeedSlotId, args.Actor);
    }

    private void OnExtractGene(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeExtractGeneBuiMsg args)
    {
        if (_net.IsClient)
            return;

        var comp = ent.Comp;
        if (!_power.IsPowered(ent))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.Actor);
            return;
        }

        if (comp.LoadedSnapshot is not { } snapshot)
        {
            _popup.PopupCursor("No genome is loaded. Insert a seed packet first.", args.Actor);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent, comp.DiscSlotId, out var discSlot) ||
            !TryComp(discSlot.Item, out FloraDataDiscComponent? disc))
        {
            _popup.PopupCursor("No disc loaded. Insert a flora data disc first.", args.Actor);
            return;
        }

        var gene = PlantGene.FromSnapshot(snapshot, args.GeneType);
        gene.DisplayLabel = comp.ObfuscationCodes.GetValueOrDefault(args.GeneType);
        disc.Genes.Add(gene);
        disc.GeneSource ??= comp.GenomeName;

        comp.Degradation += _random.Next(comp.DegradationMin, comp.DegradationMax + 1);
        if (comp.Degradation >= comp.MaxDegradation)
        {
            _audio.PlayPvs(comp.FailSound, ent);
            _popup.PopupCursor("The genetic buffer degraded beyond use and was wiped.", args.Actor, PopupType.MediumCaution);
            WipeBuffer(ent);
        }
        else
        {
            _audio.PlayPvs(comp.ExtractSound, ent);
        }

        Dirty(ent);
        _itemSlots.TryEjectToHands(ent, discSlot, args.Actor);
    }

    private void OnClearBuffer(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeClearBufferBuiMsg args)
    {
        TryClearBuffer(ent, args.Actor);
    }

    private void OnEjectDisc(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeEjectDiscBuiMsg args)
    {
        EjectSlot(ent, ent.Comp.DiscSlotId, args.Actor);
    }

    private string? GetSlotFor(Entity<LysisCentrifugeComponent> ent, EntityUid item)
    {
        if (HasComp<FloraDataDiscComponent>(item))
            return ent.Comp.DiscSlotId;

        if (HasComp<RMCPlantSeedComponent>(item))
            return ent.Comp.SeedSlotId;

        return null;
    }

    private string? GetServerInsertError(EntityUid item)
    {
        if (TryComp(item, out FloraDataDiscComponent? disc))
        {
            return disc.Genes.Count > 0
                ? "That disc already has gene data stored. Use a blank disc."
                : null;
        }

        if (!HasComp<RMCPlantSeedComponent>(item))
            return null;

        var mutation = _plantSeed.GetOrCreateSeedSnapshot(item).OfType<RMCPlantMutationComponent>().FirstOrDefault();
        if (mutation == null)
            return "This seed packet contains no genetic data.";

        if (mutation.MutationTier == RMCPlantMutationTier.Immutable)
            return "This seed is not compatible with our genetics technology.";

        return null;
    }

    private void SlotChanged(Entity<LysisCentrifugeComponent> ent, string containerId)
    {
        if (containerId != ent.Comp.SeedSlotId && containerId != ent.Comp.DiscSlotId)
            return;

        if (_net.IsServer && containerId == ent.Comp.SeedSlotId)
        {
            ent.Comp.SeedStats = _itemSlots.GetItemOrNull(ent, ent.Comp.SeedSlotId) is { } seed
                ? _plantSeed.GetSeedStats(seed)
                : null;
        }

        Dirty(ent);
    }

    private void EjectSlot(Entity<LysisCentrifugeComponent> ent, string slotId, EntityUid user)
    {
        if (!_itemSlots.TryGetSlot(ent, slotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, user, true);
    }

    private void TryClearBuffer(Entity<LysisCentrifugeComponent> ent, EntityUid user)
    {
        if (ent.Comp.GenomeName == null)
            return;

        if (!_power.IsPowered(ent))
        {
            _popup.PopupClient("The centrifuge has no power.", user, user);
            return;
        }

        WipeBuffer(ent);
        Dirty(ent);
        _popup.PopupClient("Genetic buffer cleared.", ent, user);
    }

    private static void WipeBuffer(Entity<LysisCentrifugeComponent> ent)
    {
        ent.Comp.LoadedSnapshot = null;
        ent.Comp.GenomeName = null;
        ent.Comp.Degradation = 0;
    }
}
