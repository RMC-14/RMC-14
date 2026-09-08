using Content.Client.Message;
using Content.Shared._RMC14.Dropship.Fabricator;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Content.Shared._RMC14.Dropship.Fabricator.DropshipFabricatorPrintableComponent;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Dropship.Fabricator;

[UsedImplicitly]
public sealed class DropshipFabricatorBui : BoundUserInterface
{
    private const int QueueRowHeight = 28;

    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    [ViewVariables]
    private DropshipFabricatorWindow? _window;

    private readonly DropshipFabricatorSystem _system;

    public DropshipFabricatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _system = EntMan.System<DropshipFabricatorSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<DropshipFabricatorWindow>();
        _window.EquipmentLabel.SetMarkupPermissive(Loc.GetString("rmc-dropship-fabricator-equipment"));
        _window.AmmoLabel.SetMarkupPermissive(Loc.GetString("rmc-dropship-fabricator-ammo"));

        Refresh();

        foreach (var id in _system.Printables)
        {
            if (!_prototypes.TryIndex(id, out var printableProto) ||
                !id.TryGet(out var printable, _prototypes, _compFactory))
            {
                continue;
            }

            var label = new RichTextLabel
            {
                Margin = new Thickness(4, 2),
                HorizontalExpand = false
            };
            label.SetMarkupPermissive(printableProto.Name);

            var button = new Button
            {
                Text = Loc.GetString("rmc-dropship-fabricator-fabricate", ("cost", printable.Cost)),
                StyleClasses = { "OpenBoth" },
                MinWidth = 120
            };
            button.OnPressed += _ => SendPredictedMessage(new DropshipFabricatorPrintMsg(id));

            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(0, 4),
                Children =
                {
                    label,
                    new Control { HorizontalExpand = true },
                    button
                },
                HorizontalExpand = true
            };

            if (printable.Category == CategoryType.Equipment)
                _window.EquipmentContainer.AddChild(container);
            else
                _window.AmmoContainer.AddChild(container);
        }
    }

    public void Refresh()
    {
        if (_window is not { Disposed: false })
            return;

        if (!EntMan.TryGetComponent(Owner, out DropshipFabricatorComponent? fabricator))
            return;

        _window.PointsLabel.Text = Loc.GetString("rmc-dropship-fabricator-points",
            ("points", fabricator.Points));

        if (fabricator.Printing is { } printing)
        {
            _window.CurrentLabel.SetMarkupPermissive(Loc.GetString("rmc-dropship-fabricator-current",
                ("item", GetPrintableName(printing))));
        }
        else
        {
            _window.CurrentLabel.SetMarkupPermissive(Loc.GetString("rmc-dropship-fabricator-idle"));
        }

        _window.QueueLabel.SetMarkupPermissive(Loc.GetString("rmc-dropship-fabricator-queue",
            ("count", fabricator.Queue.Count),
            ("max", fabricator.MaxQueue)));

        _window.QueueContainer.DisposeAllChildren();
        if (fabricator.Queue.Count == 0)
        {
            var empty = new Label
            {
                Text = Loc.GetString("rmc-dropship-fabricator-queue-empty"),
                Margin = new Thickness(4, 2),
                HorizontalExpand = true,
                SetHeight = QueueRowHeight,
            };
            _window.QueueContainer.AddChild(empty);
            return;
        }

        for (var i = 0; i < fabricator.Queue.Count; i++)
        {
            var entry = fabricator.Queue[i];
            var index = i;

            var label = new Label
            {
                Text = Loc.GetString("rmc-dropship-fabricator-queue-entry",
                    ("position", i + 1),
                    ("item", GetPrintableName(entry.Id)),
                    ("cost", entry.Cost)),
                Margin = new Thickness(4, 2),
                HorizontalExpand = false,
                VerticalAlignment = Control.VAlignment.Center,
            };

            var cancel = new Button
            {
                Text = Loc.GetString("rmc-dropship-fabricator-cancel"),
                StyleClasses = { "OpenBoth" },
                MinWidth = 90,
                SetHeight = 24,
            };
            cancel.OnPressed += _ => SendPredictedMessage(new DropshipFabricatorCancelQueueMsg(index));

            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Margin = new Thickness(0, 2),
                Children =
                {
                    label,
                    new Control { HorizontalExpand = true },
                    cancel
                },
                HorizontalExpand = true,
                SetHeight = QueueRowHeight,
            };
            _window.QueueContainer.AddChild(container);
        }
    }

    private string GetPrintableName(EntProtoId<DropshipFabricatorPrintableComponent> id)
    {
        return _prototypes.TryIndex(id, out var proto)
            ? proto.Name
            : id.ToString();
    }
}
