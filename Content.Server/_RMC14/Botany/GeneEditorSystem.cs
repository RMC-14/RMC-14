using System.Linq;
using Content.Shared._RMC14.Botany;
using Content.Server.Power.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Botany;

public sealed class GeneEditorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCPlantSeedSystem _plantSeed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneEditorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GeneEditorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GeneEditorComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<GeneEditorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        Subs.BuiEvents<GeneEditorComponent>(GeneEditorUiKey.Key, subs =>
        {
            subs.Event<GeneEditorApplyBuiMsg>(OnApplyGenes);
            subs.Event<GeneEditorEjectBuiMsg>(OnEjectAll);
        });
    }

    private void OnInit(Entity<GeneEditorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.DiscSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.DiscSlotId);
        ent.Comp.SeedSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.SeedSlotId);
    }

    private void OnInteractUsing(Entity<GeneEditorComponent> ent, ref InteractUsingEvent args)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The delivery system has no power.", args.User);
            return;
        }

        if (HasComp<FloraDataDiscComponent>(args.Used))
        {
            if (ent.Comp.DiscSlot.ContainedEntity != null)
            {
                _popup.PopupCursor("A disc is already loaded. Eject it first.", args.User);
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
            if (ent.Comp.DiscSlot.ContainedEntity == null)
            {
                _popup.PopupCursor("Load a flora data disc first.", args.User);
                return;
            }

            if (ent.Comp.SeedSlot.ContainedEntity != null)
            {
                _popup.PopupCursor("A seed packet is already loaded. Eject it first.", args.User);
                return;
            }

            var snapshot = _plantSeed.GetOrCreateSeedSnapshot(args.Used);
            if (snapshot.OfType<RMCPlantMutationComponent>().FirstOrDefault() is { MutationTier: RMCPlantMutationTier.Immutable })
            {
                _popup.PopupCursor("This seed is not compatible with our genetics technology.", args.User);
                return;
            }

            if (!_container.Insert(args.Used, ent.Comp.SeedSlot))
                return;

            UpdateState(ent);
            args.Handled = true;
        }
    }

    private void OnBeforeUIOpen(Entity<GeneEditorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateState(ent);
    }

    private void OnApplyGenes(Entity<GeneEditorComponent> ent, ref GeneEditorApplyBuiMsg args)
    {
        ApplyGenes(ent, args.Actor);
    }

    private void OnEjectAll(Entity<GeneEditorComponent> ent, ref GeneEditorEjectBuiMsg args)
    {
        EjectAll(ent, args.Actor);
    }

    private void OnGetAltVerbs(Entity<GeneEditorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var comp = ent.Comp;

        if (comp.DiscSlot.ContainedEntity != null && comp.SeedSlot.ContainedEntity != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = "Apply genes",
                Priority = 2,
                Act = () => ApplyGenes(ent, user),
            });
        }

        if (comp.DiscSlot.ContainedEntity != null || comp.SeedSlot.ContainedEntity != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = "Eject contents",
                Priority = 1,
                Act = () => EjectAll(ent, user),
            });
        }
    }

    private void ApplyGenes(Entity<GeneEditorComponent> ent, EntityUid user)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The delivery system has no power.", user);
            return;
        }

        var comp = ent.Comp;
        var discEnt = comp.DiscSlot.ContainedEntity;
        var seedEnt = comp.SeedSlot.ContainedEntity;

        if (discEnt == null || seedEnt == null)
            return;

        if (!TryComp(discEnt, out FloraDataDiscComponent? disc) || disc.Genes.Count == 0)
        {
            _popup.PopupCursor("The loaded disc has no genes to apply.", user);
            return;
        }

        if (!TryComp(seedEnt, out RMCPlantSeedComponent? _))
        {
            _popup.PopupCursor("This seed packet contains no genetic data.", user);
            return;
        }

        var snapshot = _plantSeed.GetOrCreateSeedSnapshot(seedEnt.Value);
        var mutation = snapshot.OfType<RMCPlantMutationComponent>().FirstOrDefault();
        if (mutation == null)
        {
            _popup.PopupCursor("This seed packet contains no genetic data.", user);
            return;
        }

        if (_random.Prob(mutation.GeneEditCount / 100f))
        {
            mutation.GeneEditCount = comp.MaxEditCount + 1;
            var growth = snapshot.OfType<RMCPlantGrowthComponent>().FirstOrDefault();
            if (growth != null)
                growth.Viable = false;
            _audio.PlayPvs(comp.FailSound, ent);
            _popup.PopupCursor("[color=red]Gene delivery failed! The seed is ruined.[/color]", user);
            EjectAll(ent, user);
            return;
        }

        foreach (var gene in disc.Genes)
        {
            gene.ApplyTo(snapshot);
            mutation.GeneEditCount += _random.Next(comp.EditCountAddMin, comp.EditCountAddMax + 1);
        }

        _audio.PlayPvs(comp.ApplySound, ent);

        _metaData.SetEntityName(seedEnt.Value, MetaData(seedEnt.Value).EntityName + "*");

        EjectAll(ent, user);
    }

    private void EjectAll(Entity<GeneEditorComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;

        if (comp.SeedSlot.ContainedEntity is { } seedEnt)
        {
            _container.Remove(seedEnt, comp.SeedSlot);
            _hands.TryPickupAnyHand(user, seedEnt);
        }

        if (comp.DiscSlot.ContainedEntity is { } discEnt)
        {
            _container.Remove(discEnt, comp.DiscSlot);
            _hands.TryPickupAnyHand(user, discEnt);
        }

        _audio.PlayPvs(comp.EjectSound, ent);
        UpdateState(ent);
    }

    private void UpdateState(Entity<GeneEditorComponent> ent)
    {
        var comp = ent.Comp;
        var discEnt = comp.DiscSlot.ContainedEntity;
        var seedEnt = comp.SeedSlot.ContainedEntity;

        comp.HasDisc = discEnt != null;
        comp.DiscSource = null;
        comp.DiscGeneLabels = new List<string>();

        if (discEnt != null && TryComp(discEnt, out FloraDataDiscComponent? disc))
        {
            comp.DiscSource = disc.GeneSource;
            comp.DiscGeneLabels = disc.Genes.Select(g => g.GetLabel()).ToList();
        }

        comp.HasSeed = seedEnt != null;
        comp.SeedEntityNet = seedEnt != null ? GetNetEntity(seedEnt.Value) : null;
        comp.SeedName = null;
        comp.SeedEditCount = 0;
        comp.SeedEndurance = 0f;
        comp.SeedLifespan = 0f;
        comp.SeedMaturation = 0f;
        comp.SeedProduction = 0f;
        comp.SeedYield = 0;
        comp.SeedHarvestRepeat = HarvestType.NoRepeat;
        comp.SeedSeedless = false;
        comp.SeedPotency = 0f;
        comp.SeedViable = true;

        if (seedEnt != null && TryComp(seedEnt, out RMCPlantSeedComponent? _))
        {
            comp.SeedName = MetaData(seedEnt.Value).EntityName;
            var snapshot = _plantSeed.GetOrCreateSeedSnapshot(seedEnt.Value);
            var mutation = snapshot.OfType<RMCPlantMutationComponent>().FirstOrDefault();
            comp.SeedEditCount = mutation?.GeneEditCount ?? 0;

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
}
