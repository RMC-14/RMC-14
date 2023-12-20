using System.Numerics;
using Content.Client.Eui;
using Content.Shared._CM14.Admin;
using Content.Shared._CM14.Xenos;
using Content.Shared.Eui;
using Content.Shared.Humanoid.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.ItemList;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client._CM14.Admin;

[UsedImplicitly]
public sealed class CMAdminEui : BaseEui
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private static readonly Comparer<EntityPrototype> EntityComparer =
        Comparer<EntityPrototype>.Create(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

    private static readonly Comparer<SpeciesPrototype> SpeciesComparer =
        Comparer<SpeciesPrototype>.Create(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

    private CMAdminWindow _adminWindow = default!;
    private CMCreateHiveWindow? _createHiveWindow;

    public override void Opened()
    {
        _adminWindow = new CMAdminWindow();

        _adminWindow.XenoTab.HiveList.OnItemSelected += OnHiveSelected;
        _adminWindow.XenoTab.CreateHiveButton.OnPressed += OnCreateHivePressed;

        var humanoidRow = new CMTransformRow();
        humanoidRow.Label.Text = Loc.GetString("cm-ui-humanoid");
        _adminWindow.TransformTab.Container.AddChild(humanoidRow);

        var allSpecies = new SortedSet<SpeciesPrototype>(SpeciesComparer);
        foreach (var entity in _prototypes.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (!entity.RoundStart)
                continue;

            allSpecies.Add(entity);
        }

        foreach (var species in allSpecies)
        {
            var button = new CMTransformButton { Type = TransformType.Humanoid };
            button.TransformName.Text = Loc.GetString(species.Name);
            button.OnPressed += _ => SendMessage(new CMAdminTransformHumanoidMessage(species.ID));

            humanoidRow.Container.AddChild(button);
        }

        var tiers = new SortedDictionary<int, SortedSet<EntityPrototype>>();
        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.Abstract || !entity.TryGetComponent(out XenoComponent? xeno))
                continue;

            if (!tiers.TryGetValue(xeno.Tier, out var xenos))
            {
                xenos = new SortedSet<EntityPrototype>(EntityComparer);
                tiers.Add(xeno.Tier, xenos);
            }

            xenos.Add(entity);
        }

        foreach (var (tier, xenos) in tiers)
        {
            var row = new CMTransformRow();
            row.Label.Text = Loc.GetString("cm-ui-tier", ("tier", tier));
            foreach (var xeno in xenos)
            {
                var button = new CMTransformButton { Type = TransformType.Xeno };
                button.TransformName.Text = xeno.Name;
                row.Container.AddChild(button);

                button.OnPressed += _ => SendMessage(new CMAdminTransformXenoMessage(xeno.ID));
            }

            _adminWindow.TransformTab.Container.AddChild(row);
        }

        _adminWindow.OpenCentered();
    }

    private void OnHiveSelected(ItemListSelectedEventArgs args)
    {
        var item = args.ItemList[args.ItemIndex];
        var msg = new CMAdminChangeHiveMessage((Hive) item.Metadata!);
        SendMessage(msg);
    }

    private void OnCreateHivePressed(ButtonEventArgs args)
    {
        if (_createHiveWindow != null)
        {
            _createHiveWindow.RecenterWindow(new Vector2(0.5f, 0.5f));
            return;
        }

        _createHiveWindow = new CMCreateHiveWindow();
        _createHiveWindow.OnClose += OnCreateHiveClosed;
        _createHiveWindow.HiveName.OnTextEntered += OnCreateHiveEntered;

        _createHiveWindow.OpenCentered();
    }

    private void OnCreateHiveClosed()
    {
        _createHiveWindow?.Dispose();
        _createHiveWindow = null;
    }

    private void OnCreateHiveEntered(LineEditEventArgs args)
    {
        var msg = new CMAdminCreateHiveMessage(args.Text);
        SendMessage(msg);
        _createHiveWindow?.Dispose();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not CMAdminEuiState s)
            return;

        foreach (var hive in s.Hives)
        {
            var list = _adminWindow.XenoTab.HiveList;
            list.Add(new Item(list)
            {
                Text = hive.Name,
                Metadata = hive
            });
        }
    }

    public override void Closed()
    {
        _adminWindow.Dispose();
    }
}
