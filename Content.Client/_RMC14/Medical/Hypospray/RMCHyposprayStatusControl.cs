using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared._RMC14.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.Hypospray;

public sealed class RMCHyposprayStatusControl : Control
{
    private readonly Entity<RMCHyposprayComponent> _parent;
    private readonly SharedSolutionContainerSystem _solutionContainers;
    private readonly RichTextLabel _label;
    private readonly SharedContainerSystem _container;

    private EntityUid? PrevVial;
    private FixedPoint2 PrevVolume;
    private FixedPoint2 PrevMaxVolume;
    private FixedPoint2 PrevTransferAmount;

    public RMCHyposprayStatusControl(Entity<RMCHyposprayComponent> parent, SharedSolutionContainerSystem solutionContainers, SharedContainerSystem containers)
    {
        _parent = parent;
        _solutionContainers = solutionContainers;
        _container = containers;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_container.TryGetContainer(_parent, _parent.Comp.SlotId, out var container))
            return;

        if(container.ContainedEntities.Count == 0)
        {
            if (PrevTransferAmount == _parent.Comp.TransferAmount && PrevVial == null)
                return;
            PrevVial = null;
            PrevTransferAmount = _parent.Comp.TransferAmount;
            _label.SetMarkup(Loc.GetString("rmc-hypospray-label-novial",
    ("transferVolume", _parent.Comp.TransferAmount)));
            return;
        }

        var vial = container.ContainedEntities[0];

        if (!_solutionContainers.TryGetSolution(vial, _parent.Comp.VialName, out _, out var solution))
            return;

        // only updates the UI if any of the details are different than they previously were
        if (PrevVolume == solution.Volume
            && PrevMaxVolume == solution.MaxVolume
            && PrevTransferAmount == _parent.Comp.TransferAmount && PrevVial == vial)
            return;

        PrevVolume = solution.Volume;
        PrevMaxVolume = solution.MaxVolume;
        PrevTransferAmount = _parent.Comp.TransferAmount;
        PrevVial = vial;

        // Update current volume

        _label.SetMarkup(Loc.GetString("rmc-hypospray-label",
            ("currentVolume", solution.Volume),
            ("totalVolume", solution.MaxVolume),
            ("transferVolume", _parent.Comp.TransferAmount)));
    }
}
