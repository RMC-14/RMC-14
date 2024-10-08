using System.Linq;
using Content.Client.Message;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Overwatch;

[UsedImplicitly]
public sealed class OverwatchConsoleBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private const string GreenColor = "#229132";
    private const string RedColor = "#A42625";
    private const string YellowColor = "#CED22B";

    [ViewVariables]
    private OverwatchConsoleWindow? _window;

    private readonly OverwatchConsoleSystem _overwatchConsole;
    private readonly SquadSystem _squad;

    private readonly Dictionary<NetEntity, OverwatchSquadView> _squadViews = new();

    public OverwatchConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _overwatchConsole = EntMan.System<OverwatchConsoleSystem>();
        _squad = EntMan.System<SquadSystem>();
    }

    protected override void Open()
    {
        if (_window != null)
            return;

        _window = new OverwatchConsoleWindow();
        _window.OnClose += Close;
        _window.OverwatchHeader.SetMarkupPermissive($"[color=#88C7FA]OVERWATCH DISABLED - SELECT SQUAD[/color]");

        if (State is OverwatchConsoleBuiState s)
            RefreshState(s);

        UpdateView();

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is OverwatchConsoleBuiState s)
            RefreshState(s);
    }

    private void RefreshState(OverwatchConsoleBuiState s)
    {
        if (_window == null ||
            !EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? console))
        {
            return;
        }

        _window.SquadsContainer.DisposeAllChildren();

        var squads = s.Squads.ToList();
        squads.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        foreach (var squad in squads)
        {
            var squadButton = new Button
            {
                Text = squad.Name.ToUpper(),
                ModulateSelfOverride = squad.Color,
                StyleClasses = { "OpenBoth" },
            };

            squadButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleSelectSquadBuiMsg(squad.Id));

            var panel = CreatePanel();
            panel.AddChild(squadButton);
            _window.SquadsContainer.AddChild(panel);
        }

        var activeSquad = GetActiveSquad();
        var margin = new Thickness(2);
        foreach (var squad in s.Squads)
        {
            if (!s.Marines.TryGetValue(squad.Id, out var marines))
                continue;

            if (_squadViews.TryGetValue(squad.Id, out var monitor))
            {
                monitor.Names.DisposeAllChildren();
                monitor.Roles.DisposeAllChildren();
                monitor.States.DisposeAllChildren();
                monitor.RolesContainer.DisposeAllChildren();
                monitor.Buttons.DisposeAllChildren();
            }
            else
            {
                monitor = new OverwatchSquadView();
                monitor.Visible = squad.Id == activeSquad;
                monitor.TacticalMapButton.OnPressed += _ => SendPredictedMessage(new OverwatchViewTacticalMapBuiMsg());
                monitor.OperatorButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleTakeOperatorBuiMsg());
                monitor.SearchBar.OnTextChanged += _ => monitor.UpdateResults(
                    console.Location,
                    console.ShowDead,
                    console.ShowHidden,
                    marines,
                    console
                );

                monitor.ShowLocationButton.Label.ModulateSelfOverride = Color.Black;
                monitor.ShowLocationButton.OnPressed += _ =>
                {
                    var location = console.Location == null
                        ? OverwatchLocation.Min
                        : console.Location + 1;

                    if (location > OverwatchLocation.Max)
                        location = null;

                    SendPredictedMessage(new OverwatchConsoleSetLocationBuiMsg(location));
                };

                monitor.ShowDeadButton.Label.ModulateSelfOverride = Color.Black;
                monitor.ShowDeadButton.OnPressed += _ =>
                    SendPredictedMessage(new OverwatchConsoleShowDeadBuiMsg(!console.ShowDead));

                monitor.ShowHiddenButton.Label.ModulateSelfOverride = Color.Black;
                monitor.ShowHiddenButton.OnPressed += _ =>
                    SendPredictedMessage(new OverwatchConsoleShowHiddenBuiMsg(!console.ShowHidden));

                monitor.TransferMarineButton.Label.ModulateSelfOverride = Color.Black;

                _squadViews[squad.Id] = monitor;
                _window.SquadViewContainer.AddChild(monitor);
            }

            monitor.OverwatchLabel.Text = $"{squad.Name} Overwatch | Dashboard";

            monitor.OnStop += () => SendPredictedMessage(new OverwatchConsoleStopOverwatchBuiMsg());

            var allAlive = 0;
            var roles = new Dictionary<ProtoId<JobPrototype>, (HashSet<OverwatchMarine> Deployed, HashSet<OverwatchMarine> Alive, HashSet<OverwatchMarine> All)>();
            foreach (var role in _squad.SquadRolePrototypes)
            {
                roles[role.ID] = (new HashSet<OverwatchMarine>(), new HashSet<OverwatchMarine>(), new HashSet<OverwatchMarine>());
            }

            foreach (var marine in marines)
            {
                var roleName = "None";
                if (marine.Role != null)
                {
                    if (_prototypes.TryIndex(marine.Role, out var job))
                        roleName = job.LocalizedName;

                    var role = roles.GetOrNew(marine.Role.Value, out var present);
                    if (!present)
                    {
                        role.Deployed = new HashSet<OverwatchMarine>();
                        role.Alive = new HashSet<OverwatchMarine>();
                        role.All = new HashSet<OverwatchMarine>();
                    }

                    if (marine.State == MobState.Alive)
                    {
                        role.Alive.Add(marine);
                        allAlive++;
                    }

                    if (marine.Deployed)
                        role.Deployed.Add(marine);

                    role.All.Add(marine);
                }

                var name = marine.Name;
                Control watchControl;
                if (marine.Camera == default)
                {
                    var watchLabel = new RichTextLabel();
                    watchLabel.SetMarkupPermissive($"[color={YellowColor}]{name} (NO HELMET)[/color]");
                    watchControl = watchLabel;
                }
                else
                {
                    var watchButton = new Button
                    {
                        Text = marine.Name,
                        StyleClasses = { "OpenBoth" },
                        Margin = margin,
                    };

                    watchButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleWatchBuiMsg(marine.Camera));
                    watchControl = watchButton;
                }

                var panel = CreatePanel(50);
                watchControl.Margin = margin;
                panel.AddChild(watchControl);
                monitor.Names.AddChild(panel);

                panel = CreatePanel(50);
                panel.AddChild(new Label
                {
                    Text = roleName,
                    Margin = margin,
                });
                monitor.Roles.AddChild(panel);

                var (mobState, color) = marine.State switch
                {
                    MobState.Critical => ("Unconscious", YellowColor),
                    MobState.Dead => ("Dead", RedColor),
                    _ => ("Conscious", GreenColor),
                };

                if (marine.SSD && marine.State != MobState.Dead)
                    mobState = $"{mobState} (SSD)";

                var state = new RichTextLabel { Margin = margin };
                state.SetMarkupPermissive($"[color={color}]{mobState}[/color]");
                panel = CreatePanel(50);
                panel.AddChild(state);
                monitor.States.AddChild(panel);

                var hideButton = new Button
                {
                    MaxWidth = 25,
                    MaxHeight = 25,
                    VerticalAlignment = VAlignment.Top,
                    StyleClasses = { "OpenBoth" },
                    Text = "-",
                    ModulateSelfOverride = Color.FromHex("#BB1F1D"),
                    ToolTip = "Hide marine",
                };

                if (_overwatchConsole.IsHidden((Owner, console), marine.Marine))
                {
                    hideButton.Text = "+";
                    hideButton.ModulateSelfOverride = Color.FromHex("#248E34");
                    hideButton.ToolTip = "Show marine";
                }

                hideButton.OnPressed += _ =>
                {
                    var hidden = !_overwatchConsole.IsHidden((Owner, console), marine.Marine);
                    SendPredictedMessage(new OverwatchConsoleHideBuiMsg(marine.Marine, hidden));
                };

                panel = CreatePanel(50);
                hideButton.Margin = margin;
                panel.AddChild(hideButton);
                monitor.Buttons.AddChild(panel);
            }

            // TODO RMC14 change squad leader
            var rolesList = new List<(string Role, HashSet<OverwatchMarine> Deployed, HashSet<OverwatchMarine> Alive, HashSet<OverwatchMarine> All, bool DisplayName, int Priority)>();
            foreach (var (id, (deployed, alive, all)) in roles)
            {
                if (_prototypes.TryIndex(id, out var role) && role.OverwatchSortPriority is { } priority)
                    rolesList.Add((role.ID, deployed, alive, all, role.OverwatchShowName, priority));
            }

            rolesList.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var (roleId, deployed, alive, all, displayName, _) in rolesList)
            {
                if (!_prototypes.TryIndex(roleId, out JobPrototype? role))
                    continue;

                string roleDeployed;
                string roleAlive;
                if (displayName)
                {
                    if (all.TryFirstOrNull(out var first))
                    {
                        roleDeployed = first.Value.Name;
                        roleAlive = first.Value.State == MobState.Dead
                            ? $"[color={RedColor}]DEAD[/color]"
                            : $"[color={GreenColor}]ALIVE[/color]";
                    }
                    else
                    {
                        roleDeployed = $"[color={RedColor}]NONE[/color]";
                        roleAlive = $"[color={RedColor}]N/A[/color]";
                    }
                }
                else
                {
                    roleDeployed = $"{deployed.Count} DEPLOYED";

                    var aliveColor = alive.Count > 0 ? GreenColor : RedColor;
                    roleAlive = $"[color={aliveColor}]{alive.Count} ALIVE[/color]";
                }

                var deployedLabel = new RichTextLabel();
                deployedLabel.SetMarkupPermissive(roleDeployed);

                var aliveLabel = new RichTextLabel();
                aliveLabel.SetMarkupPermissive(roleAlive);

                var roleNamePanel = CreatePanel(thickness: new Thickness(0, 0, 0, 1));
                roleNamePanel.AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Control { HorizontalExpand = true },
                        new Label { Text = role.OverwatchRoleName },
                        new Control { HorizontalExpand = true },
                    },
                    Margin = margin,
                });

                var panel = CreatePanel();
                panel.AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    HorizontalExpand = true,
                    Children =
                    {
                        roleNamePanel,
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Children =
                            {
                                new Control { HorizontalExpand = true },
                                deployedLabel,
                                new Control { HorizontalExpand = true },
                            },
                        },
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Children =
                            {
                                new Control { HorizontalExpand = true },
                                aliveLabel,
                                new Control { HorizontalExpand = true },
                            },
                        },
                    },
                });
                monitor.RolesContainer.AddChild(panel);
            }

            var totalAliveColor = allAlive > 0 ? GreenColor : RedColor;
            var totalAlive = $"[color={totalAliveColor}]{allAlive} ALIVE[/color]";
            var totalAliveLabel = new RichTextLabel();
            totalAliveLabel.SetMarkupPermissive(totalAlive);

            var totalPanel = CreatePanel();
            var totalLivingPanel = CreatePanel(thickness: new Thickness(0, 0, 0, 1));
            totalLivingPanel.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    new Control { HorizontalExpand = true },
                    new Label { Text = "Total/Living" },
                    new Control { HorizontalExpand = true },
                },
                Margin = margin,
            });
            totalPanel.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
                Children =
                {
                    totalLivingPanel,
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Control { HorizontalExpand = true },
                            new Label { Text = $"{marines.Count} TOTAL" },
                            new Control { HorizontalExpand = true },
                        },
                    },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            new Control { HorizontalExpand = true },
                            totalAliveLabel,
                            new Control { HorizontalExpand = true },
                        },
                    },
                },
            });

            monitor.RolesContainer.AddChild(totalPanel);
            monitor.UpdateResults(console.Location, console.ShowDead, console.ShowHidden, marines, console);
        }

        UpdateView();
    }

    private void UpdateView()
    {
        if (_window == null ||
            !EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? console))
        {
            return;
        }

        var activeSquad = GetActiveSquad();
        if (activeSquad == null)
        {
            _window.OverwatchViewContainer.Visible = true;
            _window.SquadViewContainer.Visible = false;
            _window.Wrapper.VerticalAlignment = VAlignment.Top;
        }
        else
        {
            _window.OverwatchViewContainer.Visible = false;
            _window.SquadViewContainer.Visible = true;
            _window.Wrapper.VerticalAlignment = VAlignment.Stretch;
        }

        var consoleOperator = GetOperator();
        foreach (var (id, squad) in _squadViews)
        {
            squad.Visible = id == activeSquad;
            squad.OperatorButton.Text = consoleOperator == null
                ? string.Empty
                : $"Operator - {consoleOperator}";

            squad.ShowLocationButton.Text = console.Location switch
            {
                OverwatchLocation.Planet => "Shown: planetside",
                OverwatchLocation.Ship => "Shown: shipside",
                _ => "Shown: all",
            };

            squad.ShowDeadButton.Text = console.ShowDead
                ? "Hide dead"
                : "Show dead";

            squad.ShowHiddenButton.Text = console.ShowHidden
                ? "Hide hidden"
                : "Show hidden";
        }
    }

    private PanelContainer CreatePanel(float minHeight = 0, Thickness? thickness = null)
    {
        thickness ??= new Thickness(1);
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BorderColor = Color.FromHex("#88C7FA"),
                BorderThickness = thickness.Value,
            },
        };

        if (minHeight > 0)
            panel.MinHeight = minHeight;

        return panel;
    }

    private NetEntity? GetActiveSquad()
    {
        return EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? overwatch)
            ? overwatch.Squad
            : null;
    }

    private string? GetOperator()
    {
        return EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? overwatch)
            ? overwatch.Operator
            : null;
    }

    public void Refresh()
    {
        if (State is OverwatchConsoleBuiState s)
            RefreshState(s);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
