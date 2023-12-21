using Content.Server.Body.Components;
using Content.Shared._CM14.Marines.Medical;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._CM14.Marines.Medical;

public sealed class IVDripSystem : SharedIVDripSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<IVDripComponent>();
        while (query.MoveNext(out var ivId, out var ivComp))
        {
            if (ivComp.AttachedTo == default)
                continue;

            if (_itemSlots.GetItemOrNull(ivId, ivComp.Slot) is not { } bag)
                continue;

            if (!TryComp(bag, out BloodPackComponent? bagComponent))
                continue;

            if (!_solutionContainer.TryGetSolution(bag, bagComponent.Solution, out var bagSolution))
                continue;

            if (!TryComp(ivComp.AttachedTo, out BloodstreamComponent? targetBloodstream))
                continue;

            if (time < ivComp.TransferAt)
                continue;

            if (targetBloodstream.BloodSolution.Volume < targetBloodstream.BloodSolution.MaxVolume)
            {
                _solutionContainer.TryTransferSolution(bag, ivComp.AttachedTo, bagSolution, targetBloodstream.BloodSolution, ivComp.TransferAmount);
            }

            ivComp.TransferAt = time + ivComp.TransferDelay;
            Dirty(ivId, ivComp);
        }
    }
}
