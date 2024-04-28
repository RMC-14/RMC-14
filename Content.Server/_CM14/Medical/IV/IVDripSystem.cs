using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared._CM14.Medical.IV;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Timing;

namespace Content.Server._CM14.Medical.IV;

public sealed class IVDripSystem : SharedIVDripSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<IVDripComponent>();
        while (query.MoveNext(out var ivId, out var ivComp))
        {
            if (ivComp.AttachedTo is not { } attachedTo)
                continue;

            if (!InRange((ivId, ivComp), attachedTo))
                Detach((ivId, ivComp), null, true, false);

            if (_itemSlots.GetItemOrNull(ivId, ivComp.Slot) is not { } pack)
                continue;

            if (!TryComp(pack, out BloodPackComponent? packComponent))
                continue;

            if (!_solutionContainer.TryGetSolution(pack, packComponent.Solution, out var packSolEnt, out var packSol))
                continue;

            if (!TryComp(attachedTo, out BloodstreamComponent? attachedStream))
                continue;

            if (!_solutionContainer.TryGetSolution(attachedTo, attachedStream.BloodSolutionName, out var streamSolEnt, out var streamSol))
                continue;

            if (time < ivComp.TransferAt)
                continue;

            if (ivComp.Injecting)
            {
                if (attachedStream.BloodSolution is { } bloodSolutionEnt &&
                    bloodSolutionEnt.Comp.Solution.Volume < bloodSolutionEnt.Comp.Solution.MaxVolume)
                {
                    _solutionContainer.TryTransferSolution(bloodSolutionEnt, packSol, ivComp.TransferAmount);
                }
            }
            else
            {
                if (packSol.Volume < packSol.MaxVolume)
                {
                    _solutionContainer.TryTransferSolution(packSolEnt.Value, streamSol, ivComp.TransferAmount);
                }
            }

            ivComp.TransferAt = time + ivComp.TransferDelay;
            Dirty(ivId, ivComp);
        }
    }
}
