using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._RMC14.Body;
using Content.Server.Chat.Systems;
using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared._RMC14.Medical.IV;
using Content.Shared.Body.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.IV;

public sealed class IVDripSystem : SharedIVDripSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly RMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<ProtoId<ReagentPrototype>> _reagentRemovalBuffer = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PortableDialysisComponent, PowerCellChangedEvent>(OnDialysisBatteryChargeChanged);
    }

    private bool TryGetBloodstream(
        EntityUid attachedTo,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solEnt,
        [NotNullWhen(true)] out Solution? solution,
        out Entity<SolutionComponent>? bloodstreamSolution)
    {
        solEnt = default;
        solution = default;
        bloodstreamSolution = default;
        if (!TryComp(attachedTo, out BloodstreamComponent? attachedStream) ||
            !_solutionContainer.TryGetSolution(attachedTo, attachedStream.BloodSolutionName, out solEnt, out solution))
        {
            return false;
        }

        bloodstreamSolution = attachedStream.BloodSolution;
        return true;
    }

    protected override void DoRip(DamageSpecifier? damage, EntityUid attached, EntityUid? user, ProtoId<EmotePrototype> ripEmote, bool predict)
    {
        base.DoRip(damage, attached, user, ripEmote, predict);
        _chat.TryEmoteWithoutChat(attached, ripEmote);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var ivs = EntityQueryEnumerator<IVDripComponent>();
        while (ivs.MoveNext(out var ivId, out var ivComp))
        {
            if (ivComp.AttachedTo is not { } attachedTo)
                continue;

            if (!InRange(ivId, attachedTo, ivComp.Range))
                DetachIV((ivId, ivComp), null, true, false);

            if (time < ivComp.TransferAt)
                continue;

            if (_itemSlots.GetItemOrNull(ivId, ivComp.Slot) is not { } pack)
                continue;

            if (!TryComp(pack, out BloodPackComponent? packComponent))
                continue;

            ivComp.TransferAt = time + ivComp.TransferDelay;

            if (!_solutionContainer.TryGetSolution(pack, packComponent.Solution, out var packSolEnt, out var packSol))
                continue;

            if (!TryGetBloodstream(attachedTo, out var streamSolEnt, out var streamSol, out var attachedStream))
                continue;

            if (ivComp.Injecting)
            {
                if (attachedStream is { } bloodSolutionEnt &&
                    bloodSolutionEnt.Comp.Solution.Volume < bloodSolutionEnt.Comp.Solution.MaxVolume)
                {
                    // Don't transfer non-blood reagants
                    Solution excludedSolution = packSol.SplitSolutionWithout(packSol.MaxVolume, packComponent.TransferableReagents);
                    _solutionContainer.TryTransferSolution(bloodSolutionEnt, packSol, ivComp.TransferAmount);
                    _solutionContainer.TryAddSolution(packSolEnt.Value, excludedSolution);
                    Dirty(packSolEnt.Value);
                }
            }
            else
            {
                if (packSol.Volume < packSol.MaxVolume)
                {
                    _solutionContainer.TryTransferSolution(packSolEnt.Value, streamSol, ivComp.TransferAmount);
                    Dirty(streamSolEnt.Value);
                }
            }

            Dirty(ivId, ivComp);
            UpdateIVVisuals((ivId, ivComp));
            UpdatePackVisuals((pack, packComponent));
        }

        var packs = EntityQueryEnumerator<BloodPackComponent>();
        while (packs.MoveNext(out var packId, out var packComp))
        {
            if (packComp.AttachedTo is not { } attachedTo)
                continue;

            if (!InRange(packId, attachedTo, packComp.Range))
                DetachPack((packId, packComp), null, true, false);

            if (time < packComp.TransferAt)
                continue;

            packComp.TransferAt = time + packComp.TransferDelay;

            if (!_solutionContainer.TryGetSolution(packId, packComp.Solution, out var packSolEnt, out var packSol))
                continue;

            if (!TryGetBloodstream(attachedTo, out var streamSolEnt, out var streamSol, out var attachedStream))
                continue;

            if (packComp.Injecting)
            {
                if (attachedStream is { } bloodSolutionEnt &&
                    bloodSolutionEnt.Comp.Solution.Volume < bloodSolutionEnt.Comp.Solution.MaxVolume)
                {
                    // Don't transfer non-blood reagants
                    Solution excludedSolution = packSol.SplitSolutionWithout(packSol.MaxVolume, packComp.TransferableReagents);
                    _solutionContainer.TryTransferSolution(bloodSolutionEnt, packSol, packComp.TransferAmount);
                    _solutionContainer.TryAddSolution(packSolEnt.Value, excludedSolution);
                    Dirty(packSolEnt.Value);
                }
            }
            else
            {
                if (packSol.Volume < packSol.MaxVolume)
                {
                    _solutionContainer.TryTransferSolution(packSolEnt.Value, streamSol, packComp.TransferAmount);
                    Dirty(streamSolEnt.Value);
                }
            }

            Dirty(packId, packComp);
            UpdatePackVisuals((packId, packComp));
        }

        var dialysis = EntityQueryEnumerator<PortableDialysisComponent>();
        while (dialysis.MoveNext(out var dialysisId, out var dialysisComp))
        {
            if (dialysisComp.DetachingEnd != TimeSpan.Zero && dialysisComp.DetachingEnd <= time)
            {
                dialysisComp.DetachingEnd = TimeSpan.Zero;
                Dirty(dialysisId, dialysisComp);
            }

            if (dialysisComp.AttachedTo is not { } attachedTo)
                continue;

            if (!InRange(dialysisId, attachedTo, dialysisComp.Range))
                DetachDialysis((dialysisId, dialysisComp), null, true, false);

            if (!_powerCell.HasActivatableCharge(dialysisId))
                DetachDialysis((dialysisId, dialysisComp), null, false, false);

            if (time < dialysisComp.TransferAt)
                continue;

            dialysisComp.TransferAt = time + dialysisComp.TransferDelay;

            if (!TryGetBloodstream(attachedTo, out var streamSolEnt, out _, out _))
                continue;

            if (_rmcBloodstream.TryGetChemicalSolution(attachedTo, out var chemSolEnt, out var chemSol))
            {
                _reagentRemovalBuffer.Clear();
                foreach (var reagentQuantity in chemSol.Contents)
                {
                    if (!dialysisComp.NonTransferableReagents.Contains(reagentQuantity.Reagent.Prototype))
                        _reagentRemovalBuffer.Add(reagentQuantity.Reagent.Prototype);
                }

                foreach (var reagent in _reagentRemovalBuffer)
                {
                    _solutionContainer.RemoveReagent(chemSolEnt, reagent, dialysisComp.DialysisAmount);
                }
            }

            _solutionContainer.SplitSolution(streamSolEnt.Value, dialysisComp.BloodCost);
            _powerCell.TryUseActivatableCharge(dialysisId);
            Dirty(dialysisId, dialysisComp);
        }
    }

    private void OnDialysisBatteryChargeChanged(Entity<PortableDialysisComponent> dialysis, ref PowerCellChangedEvent args)
    {
        UpdateDialysisBatteryAppearance(dialysis.Owner, GetDialysisBatteryLevel(dialysis));
    }

    private DialysisBatteryLevel GetDialysisBatteryLevel(Entity<PortableDialysisComponent> dialysis)
    {
        if (!_powerCell.TryGetBatteryFromSlot(dialysis, out _, out var battery) || battery.MaxCharge <= 0)
            return DialysisBatteryLevel.Battery0;

        var percentCharged = battery.CurrentCharge / battery.MaxCharge;
        return percentCharged switch
        {
            > 0.85f => DialysisBatteryLevel.Battery100,
            > 0.60f => DialysisBatteryLevel.Battery85,
            > 0.45f => DialysisBatteryLevel.Battery60,
            > 0.30f => DialysisBatteryLevel.Battery45,
            > 0.15f => DialysisBatteryLevel.Battery30,
            > 0.01f => DialysisBatteryLevel.Battery15,
            _ => DialysisBatteryLevel.Battery0,
        };
    }
}
