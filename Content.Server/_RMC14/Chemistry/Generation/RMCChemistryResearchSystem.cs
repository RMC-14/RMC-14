using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Events;
using Content.Server.Radio.EntitySystems;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Intel;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Server.GameStates;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCChemistryResearchSystem : SharedRMCChemistryResearchSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly RMCChemicalGeneratorSystem _generator = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    // Its medhud wearers in CM but I'm lazy & this makes more sense :godo:
    private static readonly ProtoId<RadioChannelPrototype> MedicalAdvisoryChannel = "MarineMedical";

    private static readonly EntProtoId<RMCChemistryResearchComponent> ResearchProto = "RMCChemistryResearch";
    private static readonly EntProtoId ContractNotesProto = "RMCChemContractNotes";

    private const string ManualScansCategory = "Manual Scans";

    private readonly Dictionary<int, ContractStats> _contractStats = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<IntelChemSynthesisPaperComponent, MapInitEvent>(OnSynthesisPaperMapInit);
        SubscribeLocalEvent<RMCResearchDocumentViewComponent, BoundUIClosedEvent>(OnDocumentViewClosed);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        EnsureResearch();
    }

    private void OnSynthesisPaperMapInit(Entity<IntelChemSynthesisPaperComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out PaperComponent? paper))
            return;

        var reagentId = _generator.GenerateReagent(_random.Next(1, 3));
        if (!_rmcReagent.TryIndex(reagentId, out var reagent) ||
            !_generator.TryGetRecipeReactants(reagentId.Id, out var ingredients))
        {
            return;
        }

        _paper.SetContent((ent.Owner, paper), FormatSynthesisNotes(reagentId, ingredients, _generator.GetProperties(reagent), contract: false));
    }

    public override void Update(float frameTime)
    {
        OpenPendingDocumentViews();

        if (!TryGetResearch(out var research))
            return;

        if (_timing.CurTime < research.Value.Comp.NextReroll)
            return;

        RerollContracts(research.Value);
    }

    public Entity<RMCChemistryResearchComponent> EnsureResearch()
    {
        if (TryGetResearch(out var research))
            return research.Value;

        var id = Spawn(ResearchProto);
        var comp = EnsureComp<RMCChemistryResearchComponent>(id);
        var ent = (Entity<RMCChemistryResearchComponent>) (id, comp);
        _pvsOverride.AddGlobalOverride(id);
        RerollContracts(ent);
        SeedStaticPendingChemicals();

        return ent;
    }

    private void SeedStaticPendingChemicals()
    {
        foreach (var reagent in _prototype.EnumeratePrototypes<ReagentPrototype>())
        {
            if (reagent.ChemClass < ChemClass.Special)
                continue;

            if (_generator.IsGenerated(reagent.ID))
                continue;

            _intel.AddPendingChemical(reagent.ID, reagent.LocalizedName);
        }
    }

    public void TryAwardIdentificationCredits(string reagentId, int credits, FixedPoint2 points)
    {
        var research = EnsureResearch();
        if (!research.Comp.IdentifiedIds.Add(reagentId))
            return;

        research.Comp.Credits += credits;
        Dirty(research);

        _intel.AddAnalyzedChemical(points);
        _intel.RemovePendingChemical(reagentId);
        RefreshArchivedReports();
    }

    public bool TryDiscoverDnaDisintegrating(string reagentName)
    {
        var research = EnsureResearch();
        if (research.Comp.DnaDisintegratingDiscovered || !research.Comp.ReachedXAccess)
            return false;

        research.Comp.DnaDisintegratingDiscovered = true;
        research.Comp.Credits += research.Comp.DnaDisintegratingCredits;
        Dirty(research);

        _intel.AddPoints(research.Comp.DnaDisintegratingPoints);
        _chat.SendAdminAlert($"The research department has discovered DNA_Disintegrating in {reagentName} adding {research.Comp.DnaDisintegratingPoints} bonus tech points.");

        // TODO RMC14: calls the weya Goons (Chemical Investigation Squad) ERT here
        return true;
    }

    public bool TrySpendCredits(int cost)
    {
        var research = EnsureResearch();
        if (cost > research.Comp.Credits)
            return false;

        research.Comp.Credits -= cost;
        Dirty(research);
        return true;
    }

    public void AddCredits(int amount)
    {
        var research = EnsureResearch();
        research.Comp.Credits += amount;
        Dirty(research);
    }

    public int SetCredits(int amount)
    {
        var research = EnsureResearch();
        research.Comp.Credits = Math.Max(amount, 0);
        Dirty(research);
        return research.Comp.Credits;
    }

    public int SetClearance(int level)
    {
        var research = EnsureResearch();
        research.Comp.ClearanceLevel = Math.Clamp(level, 1, research.Comp.MaxClearanceLevel);
        Dirty(research);
        RefreshArchivedReports();
        return research.Comp.ClearanceLevel;
    }

    public bool TryRaiseClearance(EntityCoordinates? spawnAt = null)
    {
        var research = EnsureResearch();
        if (research.Comp.ClearanceLevel >= research.Comp.MaxClearanceLevel)
            return false;

        var cost = GetClearanceUpgradeCost(research.Comp);
        if (!TrySpendCredits(cost))
            return false;

        research.Comp.ClearanceLevel++;
        Dirty(research);
        RefreshArchivedReports();

        if (spawnAt != null)
        {
            _popup.PopupCoordinates($"Clearance access increased to level {research.Comp.ClearanceLevel} for {cost} credits.", spawnAt.Value, PopupType.Medium);

            var reagentId = _generator.GenerateReagent(research.Comp.ClearanceLevel, ChemClass.Rare);
            if (_rmcReagent.TryIndex(reagentId, out var reagent))
                _generator.PrintReport(spawnAt.Value, reagentId, reagent, category: "Clearance Grants");
        }

        return true;
    }

    public bool HasXAccess()
    {
        return TryGetResearch(out var research) && research.Value.Comp.ReachedXAccess;
    }

    public bool TryRequestXAccess()
    {
        var research = EnsureResearch();
        if (research.Comp.ReachedXAccess)
            return false;

        if (research.Comp.ClearanceLevel < research.Comp.MaxClearanceLevel)
            return false;

        if (!TrySpendCredits(research.Comp.XAccessCost))
            return false;

        research.Comp.ReachedXAccess = true;
        Dirty(research);
        RefreshArchivedReports();
        return true;
    }

    public bool IsLocked(string reagentId)
    {
        return TryGetResearch(out var research) && research.Value.Comp.LockedIds.Contains(reagentId);
    }

    public bool IsIdentified(string reagentId)
    {
        return TryGetResearch(out var research) && research.Value.Comp.IdentifiedIds.Contains(reagentId);
    }

    public void LockLineage(string reagentId)
    {
        var research = EnsureResearch();

        if (!_generator.TryGetOriginalId(reagentId, out var parentId))
        {
            research.Comp.LockedIds.Add(reagentId);
        }
        else
        {
            research.Comp.LockedIds.Add(parentId);
            foreach (var sibling in _generator.GetVariantsOf(parentId))
                research.Comp.LockedIds.Add(sibling);
        }

        Dirty(research);
    }

    public void ForceRerollContracts()
    {
        RerollContracts(EnsureResearch());
    }

    public void RerollContracts(Entity<RMCChemistryResearchComponent> research)
    {
        research.Comp.Contracts.Clear();
        _contractStats.Clear();

        for (var i = 0; i < research.Comp.ContractSlotCount; i++)
        {
            try
            {
                var tier = _random.Next(1, 4);
                var stats = _generator.RollStats(tier);
                _contractStats[i] = stats;
                research.Comp.Contracts.Add(new ContractSlot
                {
                    Name = stats.Name,
                    Tier = tier,
                    PropertyHintId = stats.PropertyHintId,
                    IngredientHintId = stats.IngredientHintId,
                    Taken = false,
                });
            }
            catch (Exception e)
            {
                Log.Error($"RerollContracts: slot {i} failed to roll:\n{e}");
            }
        }

        research.Comp.PickedThisCycle = false;
        research.Comp.NextReroll = _timing.CurTime + research.Comp.ContractRerollNotPicked;
        Dirty(research);

        var computers = EntityQueryEnumerator<RMCResearchComputerComponent>();
        while (computers.MoveNext(out var computerId, out _))
        {
            _popup.PopupEntity("Chemical contracts have been updated!", computerId, PopupType.Medium);
            _audio.PlayPvs(research.Comp.NotifySound, computerId);
        }
    }

    public bool TryTakeContract(int slotIndex, EntityCoordinates spawnAt)
    {
        var research = EnsureResearch();
        if (research.Comp.PickedThisCycle)
            return false;

        if (slotIndex < 0 || slotIndex >= research.Comp.Contracts.Count)
            return false;

        var slot = research.Comp.Contracts[slotIndex];
        if (slot.Taken)
            return false;

        ProtoId<ReagentPrototype> reagentId;
        List<(string Id, int Amount, bool Catalyst)> ingredients;
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties;

        if (slot.ExistingReagentId is { } existingId)
        {
            if (!_rmcReagent.TryIndex(existingId, out var reagent) ||
                !_generator.TryGetRecipeReactants(existingId, out ingredients))
            {
                return false;
            }

            reagentId = existingId;
            properties = _generator.GetProperties(reagent);
        }
        else if (_contractStats.TryGetValue(slotIndex, out var stats))
        {
            (reagentId, ingredients) = _generator.FinalizeContract(stats, slot.Tier);
            properties = stats.Properties;
        }
        else
        {
            return false;
        }

        slot.Taken = true;
        research.Comp.PickedThisCycle = true;
        research.Comp.NextReroll = _timing.CurTime + research.Comp.ContractRerollPicked;

        var notes = Spawn(ContractNotesProto, spawnAt);
        _paper.SetContent(notes, FormatSynthesisNotes(reagentId, ingredients, properties, contract: true));
        _audio.PlayPvs(research.Comp.FaxSound, notes);
        research.Comp.LastPickedContractReagent = reagentId.Id;

        Dirty(research);
        return true;
    }

    public bool AddContractForReagent(ProtoId<ReagentPrototype> reagentId, int tier)
    {
        tier = Math.Clamp(tier, 1, 3);
        var research = EnsureResearch();

        if (!_rmcReagent.TryIndex(reagentId, out var reagent))
            return false;

        var properties = _generator.GetProperties(reagent);
        if (!_generator.TryGetRecipeReactants(reagentId, out var ingredients))
            ingredients = new();

        var propertyHintId = properties.Count > 0 ? _random.Pick(properties).Property.ID : string.Empty;
        var ingredientHintId = ingredients.Count > 0 ? _random.Pick(ingredients).Id : string.Empty;

        var slotIndex = research.Comp.Contracts.FindIndex(c => !c.Taken);
        if (slotIndex == -1)
            slotIndex = 0;

        var slot = new ContractSlot
        {
            Name = reagent.LocalizedName,
            Tier = tier,
            PropertyHintId = propertyHintId,
            IngredientHintId = ingredientHintId,
            Taken = false,
            ExistingReagentId = reagentId,
        };

        if (slotIndex < research.Comp.Contracts.Count)
            research.Comp.Contracts[slotIndex] = slot;
        else
            research.Comp.Contracts.Add(slot);

        _contractStats.Remove(slotIndex);
        Dirty(research);

        var computers = EntityQueryEnumerator<RMCResearchComputerComponent>();
        while (computers.MoveNext(out var computerId, out _))
        {
            _popup.PopupEntity("Chemical contracts have been updated!", computerId, PopupType.Medium);
            _audio.PlayPvs(research.Comp.NotifySound, computerId);
        }

        return true;
    }

    public bool TryReprintContract(EntityCoordinates spawnAt)
    {
        var research = EnsureResearch();
        if (string.IsNullOrEmpty(research.Comp.LastPickedContractReagent))
            return false;

        var reagentId = research.Comp.LastPickedContractReagent;
        if (!_rmcReagent.TryIndex(reagentId, out var reagent) ||
            !_generator.TryGetRecipeReactants(reagentId, out var ingredients))
        {
            return false;
        }

        var properties = _generator.GetProperties(reagent);

        var notes = Spawn(ContractNotesProto, spawnAt);
        _paper.SetContent(notes, FormatSynthesisNotes(reagentId, ingredients, properties, contract: true));
        _audio.PlayPvs(research.Comp.FaxSound, notes);

        return true;
    }

    private static readonly char[] ExperimentPrefixes = ['C', 'Q', 'V', 'W', 'X', 'Y', 'Z'];
    private static readonly char[] ExperimentSuffixes = ['a', 'b', 'c'];

    private string FormatSynthesisNotes(
        ProtoId<ReagentPrototype> reagentId,
        List<(string Id, int Amount, bool Catalyst)> ingredients,
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        bool contract)
    {
        var name = GetReagentName(reagentId);
        var experimentId = $"{_random.Pick(ExperimentPrefixes)}{_random.Next(100, 1000)}{_random.Pick(ExperimentSuffixes)}";
        var title = contract ? $"Contract for {name}" : $"Synthesis of {name}";

        var sb = new StringBuilder();
        RMCChemPaperFormat.AppendHeader(sb, "Official Weston-Yamada Document", "Experiment Notes");
        sb.AppendLine();
        sb.AppendLine($"[bold][head=3][color=#4A90E2]{title}[/color][/head][/bold]");
        sb.AppendLine();

        sb.AppendLine($"During experiment {experimentId} the theorized compound identified as {name} was successfully synthesized using the following formula:");
        sb.AppendLine();

        foreach (var (id, amount, catalyst) in ingredients.Where(i => !i.Catalyst))
            sb.AppendLine($"- {amount} {GetReagentName(id)}");

        var catalysts = ingredients.Where(i => i.Catalyst).ToList();
        if (catalysts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("While using the following catalysts:");
            sb.AppendLine();
            foreach (var (id, amount, _) in catalysts)
                sb.AppendLine($"- {amount} {GetReagentName(id)}");
        }

        sb.AppendLine();
        sb.AppendLine("Testing for chemical properties is currently pending.");

        if (RMCChemPaperFormat.IsVolatile(properties))
        {
            sb.AppendLine();
            sb.AppendLine("[bold]WARNING: UNSTABLE REAGENT. MIX CAREFULLY.[/bold]");
        }

        sb.AppendLine();
        RMCChemPaperFormat.AppendFooter(sb, "Weston-Yamada");

        return RMCChemPaperFormat.Wrap(sb);
    }

    private string GetReagentName(string reagentId)
    {
        return _rmcReagent.TryIndex(reagentId, out var reagent) ? reagent.LocalizedName : reagentId;
    }

    public void SaveDocument(string category, EntityUid report, string title)
    {
        if (CopyDocument(report, null) is not { } archived)
            return;

        var research = EnsureResearch();
        if (!research.Comp.Documents.TryGetValue(category, out var list))
            research.Comp.Documents[category] = list = new List<DocumentEntry>();

        list.Add(new DocumentEntry
        {
            Id = research.Comp.NextDocumentId++,
            Report = GetNetEntity(archived),
            Title = title,
            Time = _timing.CurTime,
        });
        Dirty(research);
    }

    public bool PublishDocument(int id)
    {
        if (!TryGetDocument(id, published: false, out var category, out var entry))
            return false;

        var research = EnsureResearch();
        if (!research.Comp.Published.TryGetValue(category, out var published))
            research.Comp.Published[category] = published = new List<DocumentEntry>();

        if (published.Any(e => e.Id == id))
            return false;

        published.Add(new DocumentEntry { Id = entry.Id, Report = entry.Report, Title = entry.Title, Time = entry.Time });
        Dirty(research);
        return true;
    }

    public bool UnpublishDocument(int id)
    {
        if (!TryGetDocument(id, published: true, out var category, out var entry) &&
            !TryGetDocument(id, published: false, out category, out entry))
        {
            return false;
        }

        var research = EnsureResearch();
        if (!research.Comp.Published.TryGetValue(category, out var list))
            return false;

        var compound = GetCompoundName(entry.Title);
        var removed = list.RemoveAll(e => GetCompoundName(e.Title) == compound);
        Dirty(research);
        return removed > 0;
    }

    private static string GetCompoundName(string title)
    {
        var dashIndex = title.IndexOf(" - ", StringComparison.Ordinal);
        return dashIndex < 0 ? title : title[(dashIndex + 3)..];
    }

    public bool TryGetDocument(int id, bool published, out string category, [NotNullWhen(true)] out DocumentEntry? entry)
    {
        category = string.Empty;
        entry = null;
        if (!TryGetResearch(out var research))
            return false;

        var dict = published ? research.Value.Comp.Published : research.Value.Comp.Documents;
        foreach (var (key, list) in dict)
        {
            if (list.FirstOrDefault(e => e.Id == id) is not { } found)
                continue;

            category = key;
            entry = found;
            return true;
        }

        return false;
    }

    private bool TryGetArchivedReport(int id, [NotNullWhen(true)] out EntityUid? report)
    {
        report = null;
        if (!TryGetDocument(id, published: false, out _, out var entry) &&
            !TryGetDocument(id, published: true, out _, out entry))
        {
            return false;
        }

        if (GetEntity(entry.Report) is not { Valid: true } uid)
            return false;

        report = uid;
        return true;
    }

    private void RefreshArchivedReports()
    {
        if (!TryGetResearch(out var research))
            return;

        var renamed = new Dictionary<int, string>();
        foreach (var (category, list) in research.Value.Comp.Documents)
        {
            if (category == ManualScansCategory)
                continue;

            foreach (var entry in list)
            {
                if (GetEntity(entry.Report) is not { Valid: true } report)
                    continue;

                var oldName = Name(report);
                _generator.RefreshReport(report);

                var newName = Name(report);
                if (newName == oldName || !entry.Title.EndsWith(oldName, StringComparison.Ordinal))
                    continue;

                entry.Title = entry.Title[..^oldName.Length] + newName;
                renamed[entry.Id] = entry.Title;
            }
        }

        if (renamed.Count == 0)
            return;

        foreach (var list in research.Value.Comp.Published.Values)
        {
            foreach (var entry in list)
            {
                if (renamed.TryGetValue(entry.Id, out var title))
                    entry.Title = title;
            }
        }

        Dirty(research.Value);
    }

    public void AnnounceDocument(EntityUid computer, int id, EntityUid actor)
    {
        if (!TryGetDocument(id, published: true, out _, out var entry))
        {
            _popup.PopupEntity("The document must be published to announce it.", computer, actor);
            return;
        }

        var research = EnsureResearch();
        if (_timing.CurTime < research.Comp.NextAnnounceAt)
        {
            _popup.PopupEntity("The announcement system is still recharging.", computer, actor);
            return;
        }

        if (GetEntity(entry.Report) is not { Valid: true } report ||
            !TryComp(report, out RMCChemResearchReportComponent? reportComp) ||
            reportComp.ReagentId is not { } reagentId)
        {
            return;
        }

        var name = GetReagentName(reagentId);
        var properties = reportComp.Properties.Count > 0
            ? string.Join(", ", reportComp.Properties.Select(p => $"{p.PropertyId} {p.Level}"))
            : "none detected";

        var message = $"Chemical Advisory: {name} has been identified. Notable properties: {properties}.";
        _radio.SendRadioMessage(computer, message, MedicalAdvisoryChannel, computer);
        _popup.PopupEntity("The terminal transmits a chemical advisory to the medical channel.", computer, actor);

        research.Comp.NextAnnounceAt = _timing.CurTime + research.Comp.AnnounceCooldown;
        Dirty(research);
    }

    public bool TryPrintDocument(int id, EntityCoordinates spawnAt)
    {
        return TryGetArchivedReport(id, out var original) &&
               CopyDocument(original.Value, spawnAt) != null;
    }

    public bool TryOpenReadingCopy(int id, EntityCoordinates spawnAt, EntityUid viewer)
    {
        if (!TryGetArchivedReport(id, out var original) ||
            CopyDocument(original.Value, spawnAt) is not { } copy)
        {
            return false;
        }

        var view = EnsureComp<RMCResearchDocumentViewComponent>(copy);
        view.PendingViewer = viewer;
        view.OpenAt = _timing.CurTime + view.OpenDelay;
        return true;
    }

    private void OpenPendingDocumentViews()
    {
        var time = _timing.CurTime;
        var views = EntityQueryEnumerator<RMCResearchDocumentViewComponent>();
        while (views.MoveNext(out var uid, out var view))
        {
            if (view.PendingViewer is not { } viewer || time < view.OpenAt)
                continue;

            view.PendingViewer = null;
            if (!TerminatingOrDeleted(viewer))
                _ui.OpenUi(uid, PaperComponent.PaperUiKey.Key, viewer);

            if (!_ui.IsUiOpen(uid, PaperComponent.PaperUiKey.Key, viewer))
                QueueDel(uid);
        }
    }

    private void OnDocumentViewClosed(Entity<RMCResearchDocumentViewComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(PaperComponent.PaperUiKey.Key))
            return;

        if (_container.IsEntityInContainer(ent))
        {
            RemCompDeferred<RMCResearchDocumentViewComponent>(ent);
            return;
        }

        QueueDel(ent);
    }

    public bool TrySaveManualScan(EntityUid item)
    {
        if (!HasComp<PaperComponent>(item) || MetaData(item).EntityPrototype == null)
            return false;

        SaveDocument(ManualScansCategory, item, Name(item));
        return true;
    }

    private EntityUid? CopyDocument(EntityUid original, EntityCoordinates? spawnAt)
    {
        if (!TryComp(original, out PaperComponent? paper))
            return null;

        var protoId = HasComp<IntelChemSynthesisPaperComponent>(original)
            ? ContractNotesProto.Id
            : MetaData(original).EntityPrototype?.ID ?? ContractNotesProto.Id;

        var copy = spawnAt is { } coordinates ? Spawn(protoId, coordinates) : Spawn(protoId);
        _paper.SetContent(copy, paper.Content);
        _metaData.SetEntityName(copy, Name(original));

        RMCChemResearchReportComponent? report = null;
        TryCopyComponent(original, copy, ref report, out _);

        return copy;
    }
}
