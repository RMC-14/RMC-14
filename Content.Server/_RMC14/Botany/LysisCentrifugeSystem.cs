using System.Linq;
using Content.Shared._RMC14.Botany;
using Content.Server.Power.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

public sealed class LysisCentrifugeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LysisCentrifugeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LysisCentrifugeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LysisCentrifugeComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<LysisCentrifugeComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);

        Subs.BuiEvents<LysisCentrifugeComponent>(LysisCentrifugeUiKey.Key, subs =>
        {
            subs.Event<LysisCentrifugeProcessSeedBuiMsg>(OnProcessSeed);
            subs.Event<LysisCentrifugeEjectSeedBuiMsg>(OnEjectSeed);
            subs.Event<LysisCentrifugeExtractGeneBuiMsg>(OnExtractGene);
            subs.Event<LysisCentrifugeClearBufferBuiMsg>(OnClearBuffer);
            subs.Event<LysisCentrifugeEjectDiscBuiMsg>(OnEjectDisc);
        });
    }

    private void OnInit(Entity<LysisCentrifugeComponent> ent, ref ComponentInit args)
    {
        EnsureComp<LysisCentrifugeBufferComponent>(ent);
        ent.Comp.DiscSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.DiscSlotId);
        ent.Comp.SeedSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.SeedSlotId);

        foreach (var type in Enum.GetValues<PlantGeneType>())
        {
            ent.Comp.ObfuscationCodes[type] = $"{_random.Next(0, 256):X2} {_random.Next(0, 256):X2}";
        }
    }

    private void OnInteractUsing(Entity<LysisCentrifugeComponent> ent, ref InteractUsingEvent args)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.User);
            return;
        }

        if (TryComp(args.Used, out FloraDataDiscComponent? insertDisc))
        {
            if (ent.Comp.DiscSlot.ContainedEntity != null)
            {
                _popup.PopupCursor("A disc is already loaded. Eject it first via the interface.", args.User);
                return;
            }

            if (insertDisc.Genes.Count > 0)
            {
                _popup.PopupCursor("That disc already has gene data stored. Use a blank disc.", args.User);
                return;
            }

            if (!_container.Insert(args.Used, ent.Comp.DiscSlot))
                return;

            UpdateState(ent);
            args.Handled = true;
            return;
        }

        if (TryComp(args.Used, out RMCPlantSeedComponent? _))
        {
            var snapshot = _plantSeed.GetOrCreateSeedSnapshot(args.Used);
            var mutation = snapshot.OfType<RMCPlantMutationComponent>().FirstOrDefault();

            if (mutation == null)
            {
                _popup.PopupCursor("This seed packet contains no genetic data.", args.User);
                return;
            }

            if (mutation.MutationTier == RMCPlantMutationTier.Immutable)
            {
                _popup.PopupCursor("This seed is not compatible with our genetics technology.", args.User);
                return;
            }

            if (ent.Comp.SeedSlot.ContainedEntity != null)
            {
                _popup.PopupCursor("A seed packet is already loaded. Eject it first.", args.User);
                return;
            }

            if (!_container.Insert(args.Used, ent.Comp.SeedSlot))
                return;

            UpdateState(ent);
            args.Handled = true;
        }
    }

    private void OnBeforeUIOpen(Entity<LysisCentrifugeComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateState(ent);
    }

    private void OnProcessSeed(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeProcessSeedBuiMsg args)
    {
        var comp = ent.Comp;

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.Actor);
            return;
        }

        if (comp.SeedSlot.ContainedEntity is not { } seedEnt)
        {
            _popup.PopupCursor("No seed packet is loaded.", args.Actor);
            return;
        }

        if (comp.GenomeName != null)
        {
            _popup.PopupCursor("A genome is already loaded. Clear the buffer first.", args.Actor);
            return;
        }

        if (!TryComp(seedEnt, out RMCPlantSeedComponent? _))
        {
            _popup.PopupCursor("This seed packet contains no genetic data.", args.Actor);
            _container.Remove(seedEnt, comp.SeedSlot);
            UpdateState(ent);
            return;
        }

        var snapshot = _plantSeed.GetOrCreateSeedSnapshot(seedEnt);

        var buffer = EnsureComp<LysisCentrifugeBufferComponent>(ent);
        buffer.LoadedSnapshot = snapshot;
        comp.GenomeName = MetaData(seedEnt).EntityName;
        comp.Degradation = 0;

        _container.Remove(seedEnt, comp.SeedSlot);
        QueueDel(seedEnt);

        _audio.PlayPvs(comp.ScanSound, ent);
        UpdateState(ent);
    }

    private void OnEjectSeed(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeEjectSeedBuiMsg args)
    {
        if (ent.Comp.SeedSlot.ContainedEntity is not { } seedEnt)
            return;

        _container.Remove(seedEnt, ent.Comp.SeedSlot);
        _hands.TryPickupAnyHand(args.Actor, seedEnt);
        UpdateState(ent);
    }

    private void OnExtractGene(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeExtractGeneBuiMsg args)
    {
        var comp = ent.Comp;

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.Actor);
            return;
        }

        if (!TryComp(ent, out LysisCentrifugeBufferComponent? buffer) || buffer.LoadedSnapshot == null)
        {
            _popup.PopupCursor("No genome is loaded. Insert a seed packet first.", args.Actor);
            return;
        }

        if (comp.Degradation >= comp.MaxDegradation)
        {
            _popup.PopupCursor("Buffer fully degraded. Genome wiped.", args.Actor);
            WipeBuffer(comp, buffer);
            UpdateState(ent);
            return;
        }

        var discEnt = comp.DiscSlot.ContainedEntity;
        if (discEnt == null || !TryComp(discEnt, out FloraDataDiscComponent? disc))
        {
            _popup.PopupCursor("No disc loaded. Insert a flora data disc first.", args.Actor);
            return;
        }

        var geneType = args.GeneType;
        if (disc.Genes.Any(g => g.Type == geneType))
        {
            _popup.PopupCursor("This sequence is already stored on the disc.", args.Actor);
            return;
        }

        var gene = PlantGene.FromSnapshot(buffer.LoadedSnapshot, geneType);
        gene.DisplayLabel = comp.ObfuscationCodes.GetValueOrDefault(geneType);
        disc.Genes.Add(gene);
        disc.GeneSource ??= comp.GenomeName;

        comp.Degradation += _random.Next(comp.DegradationMin, comp.DegradationMax + 1);

        if (comp.Degradation >= comp.MaxDegradation)
        {
            _audio.PlayPvs(comp.FailSound, ent);
            WipeBuffer(comp, buffer);
            _container.Remove(discEnt.Value, comp.DiscSlot);
            _hands.TryPickupAnyHand(args.Actor, discEnt.Value);
            UpdateState(ent);
            return;
        }

        _audio.PlayPvs(comp.ExtractSound, ent);

        _container.Remove(discEnt.Value, comp.DiscSlot);
        _hands.TryPickupAnyHand(args.Actor, discEnt.Value);

        UpdateState(ent);
    }

    private void OnClearBuffer(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeClearBufferBuiMsg args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        if (!TryComp(ent, out LysisCentrifugeBufferComponent? buffer))
            return;

        WipeBuffer(ent.Comp, buffer);
        UpdateState(ent);
    }

    private void OnEjectDisc(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeEjectDiscBuiMsg args)
    {
        if (ent.Comp.DiscSlot.ContainedEntity is not { } discEnt)
            return;

        _container.Remove(discEnt, ent.Comp.DiscSlot);
        _hands.TryPickupAnyHand(args.Actor, discEnt);
        UpdateState(ent);
    }

    private void OnGetVerbs(Entity<LysisCentrifugeComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || ent.Comp.GenomeName == null)
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = "Clear buffer",
            Act = () =>
            {
                if (!TryComp(ent, out LysisCentrifugeBufferComponent? buffer))
                    return;
                WipeBuffer(ent.Comp, buffer);
                _popup.PopupEntity("Genetic buffer cleared.", ent, user);
                UpdateState(ent);
            },
        });
    }

    private void UpdateState(Entity<LysisCentrifugeComponent> ent)
    {
        var comp = ent.Comp;
        var discEnt = comp.DiscSlot.ContainedEntity;
        var seedEnt = comp.SeedSlot.ContainedEntity;

        FloraDataDiscComponent? disc = null;
        if (discEnt != null)
            TryComp(discEnt, out disc);

        var onDiscTypes = disc != null
            ? new HashSet<PlantGeneType>(disc.Genes.Select(g => g.Type))
            : new HashSet<PlantGeneType>();

        comp.GeneSlots = Enum.GetValues<PlantGeneType>()
            .Select(t => new LysisCentrifugeGeneSlot(
                t,
                comp.ObfuscationCodes.GetValueOrDefault(t, "??"),
                onDiscTypes.Contains(t)))
            .ToList();
        comp.HasDisc = disc != null;
        comp.DiscFull = disc != null && onDiscTypes.Count >= Enum.GetValues<PlantGeneType>().Length;
        comp.HasSeed = seedEnt != null;
        comp.SeedPacketName = seedEnt != null ? Name(seedEnt.Value) : null;
        comp.SeedEntityNet = seedEnt != null ? GetNetEntity(seedEnt.Value) : null;

        comp.SeedEndurance = 0f;
        comp.SeedLifespan = 0f;
        comp.SeedMaturation = 0f;
        comp.SeedProduction = 0f;
        comp.SeedYield = 0;
        comp.SeedHarvestRepeat = HarvestType.NoRepeat;
        comp.SeedSeedless = false;
        comp.SeedPotency = 0f;
        comp.SeedViable = true;

        if (seedEnt != null)
        {
            var stats = _plantSeed.GetSeedStats(seedEnt.Value);
            comp.SeedEndurance = stats.Endurance;
            comp.SeedLifespan = stats.Lifespan;
            comp.SeedMaturation = stats.Maturation;
            comp.SeedProduction = stats.Production;
            comp.SeedYield = stats.Yield;
            comp.SeedHarvestRepeat = stats.HarvestRepeat;
            comp.SeedSeedless = stats.Seedless;
            comp.SeedPotency = stats.Potency;
            comp.SeedViable = stats.Viable;
        }

        Dirty(ent, comp);
    }

    private static void WipeBuffer(LysisCentrifugeComponent comp, LysisCentrifugeBufferComponent buffer)
    {
        buffer.LoadedSnapshot = null;
        comp.GenomeName = null;
        comp.Degradation = 0;
    }
}
