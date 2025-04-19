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
        _window = new DropshipFabricatorWindow();
        _window.OnClose += Close;

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

            var label = new RichTextLabel();
            label.SetMessage(printableProto.Name);

            var button = new Button
            {
                Text = Loc.GetString("rmc-dropship-fabricator-fabricate", ("cost", printable.Cost)),
                StyleClasses = { "OpenBoth" },
            };
            button.OnPressed += _ => SendPredictedMessage(new DropshipFabricatorPrintMsg(id));

            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    label,
                    new Control { HorizontalExpand = true },
                    button,
                },
                HorizontalExpand = true,
            };

            if (printable.Category == CategoryType.Equipment)
                _window.EquipmentContainer.AddChild(container);
            else
                _window.AmmoContainer.AddChild(container);
        }

        _window.OpenCentered();
    }

    public void Refresh()
    {
        if (_window is not { Disposed: false })
            return;

        if (EntMan.TryGetComponent(Owner, out DropshipFabricatorComponent? fabricator))
            _window.PointsLabel.Text = Loc.GetString("rmc-dropship-fabricator-points", ("points", fabricator.Points));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
