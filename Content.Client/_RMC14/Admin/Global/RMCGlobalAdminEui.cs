using System.Linq;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Eui;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Eui;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Admin.Global;

public sealed class RMCGlobalAdminEui : BaseEui
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private RMCGlobalAdminWindow _window = default!;

    public override void Opened()
    {
        _window = new RMCGlobalAdminWindow();

        TabContainer.SetTabTitle(_window.CVarsTab, "CVars");
        TabContainer.SetTabTitle(_window.MarinesTab, "Marines");
        TabContainer.SetTabTitle(_window.XenosTab, "Xenos");
        TabContainer.SetTabTitle(_window.TacticalMapTab, "Tactical Map");
        TabContainer.SetTabTitle(_window.FactionsTab, "Factions");

        _window.RefreshButton.OnPressed += OnRefresh;
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not RMCAdminEuiState s)
            return;

        _window.CVars.DisposeAllChildren();
        _window.Squads.DisposeAllChildren();
        _window.XenoTiers.DisposeAllChildren();
        _window.TacticalMapHistory.DisposeAllChildren();
        _window.Factions.DisposeAllChildren();

        foreach (var cVar in _config.GetRegisteredCVars())
        {
            if (!cVar.StartsWith("rmc.") || cVar.Contains("play_voicelines_"))
                continue;

            _window.CVars.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new HSeparator
                    {
                        Color = Color.FromHex("#4972A1"),
                        Margin = new Thickness(0, 10),
                    },
                    new Label { Text = cVar, MinWidth = 50 },
                    new Label { Text = _config.GetCVar(cVar).ToString() },
                },
            });
        }

        foreach (var squad in s.Squads)
        {
            var squadRow = new RMCSquadRow
            {
                HorizontalExpand = true,
                Margin = new Thickness(0, 0, 0, 10),
            };

            squadRow.AddToSquadButton.Visible = false;

            var squadName = string.Empty;
            var color = Color.White;
            if (_prototypes.TryIndex(squad.Id, out var squadPrototype))
            {
                squadName = squadPrototype.Name;

                if (squadPrototype.TryGetComponent(out SquadTeamComponent? squadComp, _compFactory))
                    color = squadComp.Color;
            }

            squadRow.CreateSquadButton(
                squad.Exists,
                () => SendMessage(new RMCAdminCreateSquadMsg(squad.Id)),
                squad.Members,
                squadName,
                color
            );

            _window.Squads.AddChild(squadRow);
        }

        _window.MarinesLabel.Text = $"Total marine players alive: {s.Marines}";

        var xenoTiers = new Dictionary<int, int>();
        foreach (var entity in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.Abstract || !entity.TryGetComponent(out XenoComponent? xeno, _compFactory))
                continue;

            xenoTiers.GetOrNew(xeno.Tier);
        }

        foreach (var xeno in s.Xenos)
        {
            if (_prototypes.TryIndex(xeno.Proto, out var xenoProto) &&
                xenoProto.TryGetComponent(out XenoComponent? xenoComp, _compFactory))
            {
                xenoTiers[xenoComp.Tier] = xenoTiers.GetOrNew(xenoComp.Tier) + 1;
            }
        }

        foreach (var (tier, amount) in xenoTiers.OrderBy(x => x.Key))
        {
            _window.XenoTiers.AddChild(new Label { Text = $"Tier {tier}: {amount} xenos" });
            _window.XenoTiers.AddChild(new HSeparator
            {
                Color = Color.FromHex("#4972A1"),
                Margin = new Thickness(0, 10),
            });
        }

        _window.XenosLabel.Text = $"Total xenonid players alive: {s.Xenos.Count}";

        foreach (var (guid, actor, round) in s.TacticalMapHistory)
        {
            var lines = new Button { Text = $"Round {round} by {actor}" };
            lines.OnPressed += _ => SendMessage(new RMCAdminRequestTacticalMapHistory(guid));
            _window.TacticalMapHistory.AddChild(lines);
        }

        _window.TacticalMap.Lines.Clear();
        if (s.TacticalMapLines == default)
        {
            _window.TacticalMapLabel.Text = "Selected: None";
        }
        else
        {
            _window.TacticalMapLabel.Text = $"Selected: Round {s.TacticalMapLines.RoundId} by {s.TacticalMapLines.Actor}";
            _window.TacticalMap.Texture = Texture.Transparent;
            _window.TacticalMap.Lines.AddRange(s.TacticalMapLines.Lines);
        }

        foreach (var (left, value) in s.Factions)
        {
            var factionBox = new BoxContainer()
            {
                Orientation = LayoutOrientation.Vertical,
            };
            factionBox.AddChild(new Label { Text = $"{left}:", MinWidth = 50 });

            foreach (var right in s.Factions.Keys)
            {
                if (left == right)
                    continue;

                var innerBox = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var group = new ButtonGroup();
                var friendly = new Button()
                {
                    Text = "+",
                    ToggleMode = true,
                    Pressed = value.Friendly.Contains(right),
                    Group = group,
                };
                friendly.OnPressed += _ =>
                {
                    SendMessage(new RMCAdminFactionMsg(RMCAdminFactionMsgType.Friendly, left, right));
                };
                innerBox.AddChild(friendly);

                var neutral = new Button()
                {
                    Text = "=",
                    ToggleMode = true,
                    Pressed = !(value.Friendly.Contains(right) || value.Hostile.Contains(right)),
                    Group = group,
                };
                neutral.OnPressed += _ =>
                {
                    SendMessage(new RMCAdminFactionMsg(RMCAdminFactionMsgType.Neutral, left, right));
                };
                innerBox.AddChild(neutral);

                var hostile = new Button()
                {
                    Text = "-",
                    ToggleMode = true,
                    Pressed = value.Hostile.Contains(right),
                    Group = group,
                };
                hostile.OnPressed += _ =>
                {
                    SendMessage(new RMCAdminFactionMsg(RMCAdminFactionMsgType.Hostile, left, right));
                };
                innerBox.AddChild(hostile);

                innerBox.AddChild(new Label { Text = $"{right}", MinWidth = 50 });
                factionBox.AddChild(innerBox);
            }

            factionBox.AddChild(new HSeparator
            {
                Color = Color.FromHex("#4972A1"),
                Margin = new Thickness(0, 10),
            });
            _window.Factions.AddChild(factionBox);
        }
    }

    private void OnRefresh(ButtonEventArgs args)
    {
        SendMessage(new RMCAdminRefresh());
    }
}
