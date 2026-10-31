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

public sealed class GeneEditorSystem : EntitySystem
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
        SubscribeLocalEvent<GeneEditorComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<GeneEditorComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<GeneEditorComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<GeneEditorComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<GeneEditorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        Subs.BuiEvents<GeneEditorComponent>(GeneEditorUiKey.Key, subs =>
        {
            subs.Event<GeneEditorApplyBuiMsg>(OnApplyGenes);
            subs.Event<GeneEditorEjectBuiMsg>(OnEjectAll);
        });
    }

    private void OnInteractUsing(Entity<GeneEditorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        string slotId;
        if (HasComp<FloraDataDiscComponent>(args.Used))
            slotId = ent.Comp.DiscSlotId;
        else if (HasComp<RMCPlantSeedComponent>(args.Used))
            slotId = ent.Comp.SeedSlotId;
        else
            return;

        if (!_power.IsPowered(ent))
        {
            _popup.PopupClient("The delivery system has no power.", args.User, args.User);
            args.Handled = true;
            return;
        }

        if (_itemSlots.GetItemOrNull(ent, slotId) != null)
        {
            var msg = slotId == ent.Comp.DiscSlotId
                ? "A disc is already loaded. Eject it first."
                : "A seed packet is already loaded. Eject it first.";
            _popup.PopupClient(msg, args.User, args.User);
            args.Handled = true;
            return;
        }

        if (_net.IsClient)
        {
            args.Handled = true;
            return;
        }

        if (GetServerInsertError(args.Used) is { } error)
        {
            _popup.PopupCursor(error, args.User);
            args.Handled = true;
        }
    }

    private void OnInsertAttempt(Entity<GeneEditorComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_power.IsPowered(ent) ||
            _net.IsServer && GetServerInsertError(args.Item) != null)
        {
            args.Cancelled = true;
        }
    }

    private void OnSlotChanged(Entity<GeneEditorComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        SlotChanged(ent, args.Container.ID);
    }

    private void OnSlotChanged(Entity<GeneEditorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        SlotChanged(ent, args.Container.ID);
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
        var hasDisc = _itemSlots.GetItemOrNull(ent, ent.Comp.DiscSlotId) != null;
        var hasSeed = _itemSlots.GetItemOrNull(ent, ent.Comp.SeedSlotId) != null;

        if (hasDisc && hasSeed)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = "Apply genes",
                Priority = 2,
                Act = () => ApplyGenes(ent, user),
            });
        }

        if (hasDisc || hasSeed)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = "Eject contents",
                Priority = 1,
                Act = () => EjectAll(ent, user),
            });
        }
    }

    private string? GetServerInsertError(EntityUid item)
    {
        if (TryComp(item, out FloraDataDiscComponent? disc))
        {
            return disc.Genes.Count == 0
                ? "That disc does not have any gene data loaded."
                : null;
        }

        if (!HasComp<RMCPlantSeedComponent>(item))
            return null;

        return _plantSeed.GetOrCreateSeedSnapshot(item).OfType<RMCPlantMutationComponent>().FirstOrDefault() is
            { MutationTier: RMCPlantMutationTier.Immutable }
            ? "This seed is not compatible with our genetics technology."
            : null;
    }

    private void SlotChanged(Entity<GeneEditorComponent> ent, string containerId)
    {
        var comp = ent.Comp;
        if (containerId != comp.DiscSlotId && containerId != comp.SeedSlotId)
            return;

        if (_net.IsServer)
        {
            comp.DiscSource = null;
            comp.DiscGeneLabels = new List<string>();
            if (TryComp(_itemSlots.GetItemOrNull(ent, comp.DiscSlotId), out FloraDataDiscComponent? disc))
            {
                comp.DiscSource = disc.GeneSource;
                comp.DiscGeneLabels = disc.Genes.Select(g => g.GetLabel()).ToList();
            }

            comp.SeedEditCount = 0;
            comp.SeedStats = null;
            if (_itemSlots.GetItemOrNull(ent, comp.SeedSlotId) is { } seed)
            {
                comp.SeedEditCount = CompOrNull<RMCPlantSeedMutationComponent>(seed)?.GeneEditCount ?? 0;
                comp.SeedStats = _plantSeed.GetSeedStats(seed);
            }
        }

        Dirty(ent);
    }

    private void ApplyGenes(Entity<GeneEditorComponent> ent, EntityUid user)
    {
        if (_net.IsClient)
            return;

        var comp = ent.Comp;
        if (!_power.IsPowered(ent))
        {
            _popup.PopupCursor("The delivery system has no power.", user);
            return;
        }

        if (_itemSlots.GetItemOrNull(ent, comp.DiscSlotId) is not { } discEnt ||
            _itemSlots.GetItemOrNull(ent, comp.SeedSlotId) is not { } seed)
        {
            return;
        }

        if (!TryComp(discEnt, out FloraDataDiscComponent? disc) || disc.Genes.Count == 0)
        {
            _popup.PopupCursor("The loaded disc has no genes to apply.", user);
            return;
        }

        var snapshot = _plantSeed.GetOrCreateSeedSnapshot(seed);
        if (snapshot.OfType<RMCPlantMutationComponent>().FirstOrDefault() is not { } mutation)
        {
            _popup.PopupCursor("This seed packet contains no genetic data.", user);
            return;
        }

        var seedMutation = Comp<RMCPlantSeedMutationComponent>(seed);
        if (seedMutation.GeneEditCount >= comp.MaxEditCount)
        {
            _popup.PopupCursor("This seed cannot accept any more gene edits.", user);
            return;
        }

        if (!mutation.Modified)
        {
            mutation.Modified = true;
            _plantSeed.UpdatePacketName(seed);
        }

        var failChance = Math.Clamp(seedMutation.GeneEditCount / (float) comp.MaxEditCount, 0f, 1f);
        if (_random.Prob(failChance))
        {
            seedMutation.GeneEditCount = comp.MaxEditCount + 1;
            _audio.PlayPvs(comp.FailSound, ent);
            _popup.PopupCursor("[color=red]Gene delivery failed! The seed can no longer be edited.[/color]", user);
        }
        else
        {
            foreach (var gene in disc.Genes)
            {
                gene.ApplyTo(snapshot);
                seedMutation.GeneEditCount += _random.Next(comp.EditCountAddMin, comp.EditCountAddMax + 1);
            }

            _audio.PlayPvs(comp.ApplySound, ent);
        }

        SlotChanged(ent, comp.SeedSlotId);
    }

    private void EjectAll(Entity<GeneEditorComponent> ent, EntityUid user)
    {
        var ejected = false;
        foreach (var slotId in new[] { ent.Comp.SeedSlotId, ent.Comp.DiscSlotId })
        {
            if (_itemSlots.TryGetSlot(ent, slotId, out var slot) && slot.HasItem)
                ejected |= _itemSlots.TryEjectToHands(ent, slot, user, true);
        }

        if (ejected)
            _audio.PlayPredicted(ent.Comp.EjectSound, ent, user);
    }
}
