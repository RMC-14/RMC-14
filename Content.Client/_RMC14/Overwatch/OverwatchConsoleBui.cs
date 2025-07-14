using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Message;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Maths;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.SupplyDrop;
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
public sealed class OverwatchConsoleBui : RMCPopOutBui<OverwatchConsoleWindow>
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private const string GreenColor = "#229132";
    private const string RedColor = "#A42625";
    private const string YellowColor = "#CED22B";

    protected override OverwatchConsoleWindow? Window { get; set; }

    private readonly OverwatchConsoleSystem _overwatchConsole;
    private readonly SquadSystem _squad;

    private readonly Dictionary<NetEntity, OverwatchSquadView> _squadViews = new();
    private readonly Dictionary<NetEntity, PanelContainer> _squads = new();
    private readonly Dictionary<NetEntity, Dictionary<NetEntity, OverwatchRow>> _rows = new();

    public OverwatchConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _overwatchConsole = EntMan.System<OverwatchConsoleSystem>();
        _squad = EntMan.System<SquadSystem>();
    }

    protected override void Open()
    {
        base.Open();
        if (Window != null)
            return;

        Window = this.CreatePopOutableWindow<OverwatchConsoleWindow>();
        Window.OverwatchHeader.SetMarkupPermissive("[color=#88C7FA]OVERWATCH DISABLED - SELECT SQUAD[/color]");

        if (State is OverwatchConsoleBuiState s)
            RefreshState(s);

        UpdateView();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is OverwatchConsoleBuiState s)
            RefreshState(s);
    }

    private void RefreshState(OverwatchConsoleBuiState s)
    {
        if (Window == null ||
            !EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? console))
        {
            return;
        }

        var squads = s.Squads.ToList();
        squads.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        foreach (var (id, panel) in _squads)
        {
            if (squads.All(oldSquad => oldSquad.Id != id))
                panel.Orphan();
        }

        foreach (var squad in squads)
        {
            if (_squads.ContainsKey(squad.Id))
                continue;

            var squadButton = new Button
            {
                Text = squad.Name.ToUpper(),
                ModulateSelfOverride = squad.Color,
                StyleClasses = { "OpenBoth" },
            };

            squadButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleSelectSquadBuiMsg(squad.Id));

            var panel = CreatePanel();
            panel.AddChild(squadButton);
            Window.SquadsContainer.AddChild(panel);

            _squads[squad.Id] = panel;
        }

        var roleSorting = new Dictionary<ProtoId<JobPrototype>, int>();
        var activeSquad = GetActiveSquad();
        var margin = new Thickness(2);
        foreach (var squad in s.Squads)
        {
            if (!s.Marines.TryGetValue(squad.Id, out var marines))
                continue;

            marines.Sort((a, b) =>
            {
                int Sorting(OverwatchMarine marine)
                {
                    if (squad.Leader == marine.Id)
                        return 1000;

                    if (marine.Role is not { } role)
                        return 0;

                    if (roleSorting.TryGetValue(role, out var sort))
                        return sort;

                    if (!_prototypes.TryIndex(role, out var roleProto) ||
                        roleProto.OverwatchSortPriority is not { } prio)
                    {
                        return 0;
                    }

                    roleSorting[role] = prio;
                    return prio;
                }

                return Sorting(a).CompareTo(Sorting(b));
            });

            if (_squadViews.TryGetValue(squad.Id, out var monitor))
            {
                monitor.RolesContainer.DisposeAllChildren();
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
                monitor.TransferMarineButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleTransferMarineBuiMsg());

                if (EntMan.TryGetComponent(Owner, out SupplyDropComputerComponent? supplyDrop))
                {
                    monitor.Longitude.Value = supplyDrop.Coordinates.X;
                    monitor.Latitude.Value = supplyDrop.Coordinates.Y;
                }

                monitor.Longitude.OnValueChanged +=
                    args => SendPredictedMessage(new OverwatchConsoleSupplyDropLongitudeBuiMsg((int) args.Value));
                monitor.Latitude.OnValueChanged +=
                    args => SendPredictedMessage(new OverwatchConsoleSupplyDropLatitudeBuiMsg((int) args.Value));
                monitor.LaunchButton.OnPressed +=
                    _ => SendPredictedMessage(new OverwatchConsoleSupplyDropLaunchBuiMsg());
                monitor.SaveButton.OnPressed +=
                    _ =>
                    {
                        var longitude = (int)monitor.Longitude.Value;
                        var latitude = (int)monitor.Latitude.Value;
                        var msg = new OverwatchConsoleSupplyDropSaveBuiMsg(longitude, latitude);
                        SendPredictedMessage(msg);
                    };

                monitor.OrbitalLongitude.Value = console.OrbitalCoordinates.X;
                monitor.OrbitalLatitude.Value = console.OrbitalCoordinates.Y;
                monitor.OrbitalLongitude.OnValueChanged +=
                    args => SendPredictedMessage(new OverwatchConsoleOrbitalLongitudeBuiMsg((int) args.Value));
                monitor.OrbitalLatitude.OnValueChanged +=
                    args => SendPredictedMessage(new OverwatchConsoleOrbitalLatitudeBuiMsg((int) args.Value));
                monitor.OrbitalFireButton.OnPressed +=
                    _ => SendPredictedMessage(new OverwatchConsoleOrbitalLaunchBuiMsg());
                monitor.OrbitalSaveButton.OnPressed +=
                    _ =>
                    {
                        var longitude = (int)monitor.Longitude.Value;
                        var latitude = (int)monitor.Latitude.Value;
                        var msg = new OverwatchConsoleOrbitalSaveBuiMsg(longitude, latitude);
                        SendPredictedMessage(msg);
                    };

                monitor.MessageSquadButton.OnPressed += _ =>
                {
                    var window = new OverwatchTextInputWindow();

                    void SendSquadMessage()
                    {
                        SendPredictedMessage(new OverwatchConsoleSendMessageBuiMsg(window.MessageBox.Text));
                        window.Close();
                    }

                    window.MessageBox.OnTextEntered += _ => SendSquadMessage();
                    window.OkButton.OnPressed += _ => SendSquadMessage();
                    window.CancelButton.OnPressed += _ => window.Close();
                    window.OpenCentered();
                };

                var canSupplyDrop = EntMan.HasComponent<SupplyDropComputerComponent>(Owner) && squad.CanSupplyDrop;
                TabContainer.SetTabVisible(monitor.SupplyDrop, canSupplyDrop);

                if (EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? overwatch))
                {
                    TabContainer.SetTabVisible(monitor.OrbitalBombardment, overwatch.CanOrbitalBombardment);
                    monitor.MessageSquadButton.Visible = overwatch.CanMessageSquad;
                }
                else
                {
                    TabContainer.SetTabVisible(monitor.OrbitalBombardment, false);
                    monitor.MessageSquadButton.Visible = false;
                }

                _squadViews[squad.Id] = monitor;
                Window.SquadViewContainer.AddChild(monitor);
            }

            monitor.OverwatchLabel.Text = $"{squad.Name} Overwatch | Dashboard";

            monitor.OnStop += () => SendPredictedMessage(new OverwatchConsoleStopOverwatchBuiMsg());

            var allAlive = 0;
            var roles = new Dictionary<ProtoId<JobPrototype>, (HashSet<OverwatchMarine> Deployed, HashSet<OverwatchMarine> Alive, HashSet<OverwatchMarine> All)>();
            foreach (var role in _squad.SquadRolePrototypes)
            {
                roles[role.ID] = (new HashSet<OverwatchMarine>(), new HashSet<OverwatchMarine>(), new HashSet<OverwatchMarine>());
            }

            var marineIds = marines.Select(e => e.Id).ToHashSet();
            var squadRows = _rows.GetOrNew(squad.Id);
            foreach (var (id, row) in squadRows.ToArray())
            {
                if (marineIds.Contains(id))
                    continue;

                row.Name.Panel.Orphan();
                row.Role.Panel.Orphan();
                row.State.Panel.Orphan();
                row.Location.Panel.Orphan();
                row.Distance.Panel.Orphan();
                row.Buttons.Container.Orphan();

                _rows.Remove(id);
            }

            foreach (var marine in marines)
            {
                var roleName = "None";
                string? rankName = null;
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
                    roles[marine.Role.Value] = role;
                }

                if (marine.Rank != null)
                {
                    if (_prototypes.TryIndex(marine.Rank, out var rank))
                        rankName = rank.Prefix;
                }

                var name = rankName != null ? $"{rankName} {marine.Name}" : marine.Name;
                if (!squadRows.TryGetValue(marine.Id, out var row))
                {
                    var watchButton = new Button
                    {
                        StyleClasses = { "OpenBoth" },
                        Margin = new Thickness(2, 0),
                    };

                    watchButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleWatchBuiMsg(marine.Id));

                    var watchLabel = new RichTextLabel();
                    watchButton.AddChild(watchLabel);

                    var namePanel = CreatePanel(50);
                    watchButton.Margin = margin;
                    namePanel.AddChild(watchButton);
                    monitor.Names.AddChild(namePanel);

                    var rolePanel = CreatePanel(50);
                    var roleLabel = new Label
                    {
                        Text = roleName,
                        Margin = margin,
                    };
                    rolePanel.AddChild(roleLabel);
                    monitor.Roles.AddChild(rolePanel);

                    var state = new RichTextLabel { Margin = margin };
                    var statePanel = CreatePanel(50);
                    statePanel.AddChild(state);
                    monitor.States.AddChild(statePanel);

                    var location = CreatePanel(50);
                    var locationLabel = new RichTextLabel()
                    {
                        Margin = margin,
                        MaxWidth = 250,
                    };
                    location.AddChild(locationLabel);
                    monitor.Locations.AddChild(location);

                    var distancePanel = CreatePanel(50);
                    var distanceLabel = new Label { Margin = margin };
                    distancePanel.AddChild(distanceLabel);
                    monitor.Distances.AddChild(distancePanel);

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

                    var promoteButton = new Button
                    {
                        MaxWidth = 25,
                        MaxHeight = 25,
                        VerticalAlignment = VAlignment.Top,
                        StyleClasses = { "OpenBoth" },
                        Text = "^",
                        ModulateSelfOverride = Color.FromHex(GreenColor),
                        ToolTip = "Promote marine to Squad Leader",
                    };

                    hideButton.OnPressed += _ =>
                    {
                        var hidden = !_overwatchConsole.IsHidden((Owner, console), marine.Id);
                        SendPredictedMessage(new OverwatchConsoleHideBuiMsg(marine.Id, hidden));
                    };

                    promoteButton.OnPressed += _ =>
                        SendPredictedMessage(new OverwatchConsolePromoteLeaderBuiMsg(marine.Id, squad.LeaderIcon));

                    var hide = CreatePanel(50);
                    hideButton.Margin = margin;
                    hide.AddChild(hideButton);
                    var buttonsContainer = new BoxContainer { Orientation = LayoutOrientation.Horizontal };
                    buttonsContainer.AddChild(hide);

                    var promote = CreatePanel(50);
                    promoteButton.Margin = margin;
                    promote.AddChild(promoteButton);
                    buttonsContainer.AddChild(promote);

                    monitor.Buttons.AddChild(buttonsContainer);

                    row = new OverwatchRow(
                        marine.Role,
                        (namePanel, watchButton, watchLabel),
                        (rolePanel, roleLabel),
                        (statePanel, state),
                        (location, locationLabel),
                        (distancePanel, distanceLabel),
                        (buttonsContainer, hideButton, promoteButton)
                    );
                    squadRows[marine.Id] = row;

                    if (marine.Role != null && squadRows.TryFirstOrNull(r => r.Key != marine.Id && r.Value.RoleId == marine.Role, out var first))
                    {
                        var position = first.Value.Value.Name.Panel.GetPositionInParent() + 1;
                        row.Name.Panel.SetPositionInParent(position);
                        row.Role.Panel.SetPositionInParent(position);
                        row.State.Panel.SetPositionInParent(position);
                        row.Location.Panel.SetPositionInParent(position);
                        row.Distance.Panel.SetPositionInParent(position);
                        row.Buttons.Container.SetPositionInParent(position);
                    }
                }

                if (marine.Camera == default)
                {
                    row.Name.Label.SetMarkupPermissive($"[color={YellowColor}]{name} (NO CAMERA)[/color]");
                    row.Name.Button.Text = null;
                    row.Name.Button.Disabled = true;
                }
                else
                {
                    row.Name.Label.Text = null;
                    row.Name.Button.Text = name;
                    row.Name.Button.Disabled = false;
                }

                row.Role.Label.Text = roleName;

                var (mobState, color) = marine.State switch
                {
                    MobState.Critical => ("Unconscious", YellowColor),
                    MobState.Dead => ("Dead", RedColor),
                    _ => ("Conscious", GreenColor),
                };

                if (marine.SSD && marine.State != MobState.Dead)
                    mobState = $"{mobState} (SSD)";

                row.State.Label.SetMarkupPermissive($"[color={color}]{mobState}[/color]");
                row.Location.Label.Text = $"[color=white]{marine.AreaName}[/color]";

                var distanceStr = "N/A";
                if (marine.LeaderDistance is { } distance &&
                    !distance.IsLengthZero())
                {
                    distanceStr = $"{marine.LeaderDistance.Value.Length():F0} ({marine.LeaderDistance.Value.GetDir().GetShorthand()})";
                }

                row.Distance.Label.Text = distanceStr;

                if (_overwatchConsole.IsHidden((Owner, console), marine.Id) &&
                    marine.Id != squad.Leader)
                {
                    row.Buttons.Hide.Text = "+";
                    row.Buttons.Hide.ModulateSelfOverride = Color.FromHex("#248E34");
                    row.Buttons.Hide.ToolTip = "Show marine";
                }
                else
                {
                    row.Buttons.Hide.Text = "-";
                    row.Buttons.Hide.ModulateSelfOverride = Color.FromHex("#BB1F1D");
                    row.Buttons.Hide.ToolTip = "Hide marine";
                }

                if (squad.Leader == marine.Id)
                {
                    row.Buttons.Hide.Visible = false;
                    row.Buttons.Promote.Visible = false;
                }
            }

            var rolesList = new List<(string Role, HashSet<OverwatchMarine> Deployed, HashSet<OverwatchMarine> Alive, HashSet<OverwatchMarine> All, bool DisplayName, int Priority)>();
            foreach (var (id, (deployed, alive, all)) in roles)
            {
                if (_prototypes.TryIndex(id, out var role) && role.OverwatchSortPriority is { } priority)
                    rolesList.Add((role.ID, deployed, alive, all, role.OverwatchShowName, priority));
            }

            rolesList.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            var i = 0;
            BoxContainer? currentRowContainer = null;

            foreach (var (roleId, deployed, alive, all, displayName, _) in rolesList)
            {
                if (!_prototypes.TryIndex(roleId, out JobPrototype? role))
                    continue;

                if (i % 2 == 0)
                {
                    currentRowContainer = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        HorizontalExpand = true,
                        SeparationOverride = 5
                    };
                    monitor.RolesContainer.AddChild(currentRowContainer);
                }

                string roleDeployed;
                string roleAlive;
                if (displayName)
                {
                    if (_squad.IsSquadLeader(roleId) &&
                        squad.Leader != null &&
                        marines.TryFirstOrNull(m => m.Id == squad.Leader.Value, out var leader))
                    {
                        roleDeployed = leader.Value.Name;
                        roleAlive = leader.Value.State == MobState.Dead
                            ? $"[bold][color={RedColor}]DEAD[/color][/bold]"
                            : $"[bold][color={GreenColor}]ALIVE[/color][/bold]";
                    }
                    else if (all.TryFirstOrNull(out var first))
                    {
                        roleDeployed = first.Value.Name;
                        roleAlive = first.Value.State == MobState.Dead
                            ? $"[bold][color={RedColor}]DEAD[/color][/bold]"
                            : $"[bold][color={GreenColor}]ALIVE[/color][/bold]";
                    }
                    else
                    {
                        roleDeployed = $"[bold][color={RedColor}]NONE[/color][/bold]";
                        roleAlive = $"[bold][color={RedColor}]N/A[/color][/bold]";
                    }
                }
                else
                {
                    roleDeployed = $"[bold]{deployed.Count} DEPLOYED[/bold]";

                    var aliveColor = alive.Count > 0 ? GreenColor : RedColor;
                    roleAlive = $"[bold][color={aliveColor}]{alive.Count} ALIVE[/color][/bold]";
                }

                var deployedLabel = new RichTextLabel();
                deployedLabel.SetMarkupPermissive(roleDeployed);

                var aliveLabel = new RichTextLabel();
                aliveLabel.SetMarkupPermissive(roleAlive);

                var roleNamePanel = CreatePanel(thickness: new Thickness(0, 0, 0, 1));
                var roleNameLabel = new RichTextLabel
                {
                    Margin = new Thickness(0, 3, 0, 3)
                };
                roleNameLabel.SetMarkupPermissive($"[bold]{role.OverwatchRoleName}[/bold]");

                roleNamePanel.AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Control { HorizontalExpand = true },
                        roleNameLabel,
                        new Control { HorizontalExpand = true },
                    },
                    Margin = margin,
                });

                var panel = CreatePanel();
                panel.HorizontalExpand = true;
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
                i++;
                currentRowContainer?.AddChild(panel);
            }

            var totalAliveColor = allAlive > 0 ? GreenColor : RedColor;
            var totalAlive = $"[bold][color={totalAliveColor}]{allAlive} ALIVE[/color][/bold]";
            var totalAliveLabel = new RichTextLabel();
            totalAliveLabel.SetMarkupPermissive(totalAlive);

            var totalPanel = CreatePanel();
            totalPanel.HorizontalExpand = true;
            var totalLivingPanel = CreatePanel(thickness: new Thickness(0, 0, 0, 1));

            var totalLivingLabel = new RichTextLabel();
            totalLivingLabel.SetMarkupPermissive("[bold]Total/Living[/bold]");

            totalLivingPanel.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    new Control { HorizontalExpand = true },
                    totalLivingLabel,
                    new Control { HorizontalExpand = true },
                },
                Margin = margin,
            });

            var totalCountLabel = new RichTextLabel();
            totalCountLabel.SetMarkupPermissive($"[bold]{marines.Count} TOTAL[/bold]");

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
                            totalCountLabel,
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
        if (Window == null ||
            !EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? console))
        {
            return;
        }

        var supplyDrop = EntMan.GetComponentOrNull<SupplyDropComputerComponent>(Owner);
        var activeSquad = GetActiveSquad();
        if (activeSquad == null)
        {
            Window.OverwatchViewContainer.Visible = true;
            Window.SquadViewContainer.Visible = false;
            Window.Wrapper.VerticalAlignment = VAlignment.Top;
        }
        else
        {
            Window.OverwatchViewContainer.Visible = false;
            Window.SquadViewContainer.Visible = true;
            Window.Wrapper.VerticalAlignment = VAlignment.Stretch;
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

            var margin = new Thickness(2);
            if (supplyDrop != null)
            {
                squad.HasCrate = supplyDrop.HasCrate;
                squad.NextLaunchAt = supplyDrop.NextLaunchAt;

                AddSaving(squad.Longitudes, squad.Latitudes, squad.Comments, squad.Saves, margin);
                AddSavedLocation(
                    console.SavedLocations,
                    margin,
                    squad.Longitudes,
                    squad.Latitudes,
                    squad.Comments,
                    squad.Saves,
                    location =>
                    {
                        squad.Longitude.Value = location.Longitude;
                        squad.Latitude.Value = location.Latitude;

                        SendPredictedMessage(new OverwatchConsoleSupplyDropLongitudeBuiMsg(location.Longitude));
                        SendPredictedMessage(new OverwatchConsoleSupplyDropLatitudeBuiMsg(location.Latitude));
                    }
                );
            }

            AddSaving(squad.OrbitalLongitudes, squad.OrbitalLatitudes, squad.OrbitalComments, squad.OrbitalSaves, margin);
            AddSavedLocation(
                console.SavedLocations,
                margin,
                squad.OrbitalLongitudes,
                squad.OrbitalLatitudes,
                squad.OrbitalComments,
                squad.OrbitalSaves,
                location =>
                {
                    squad.Longitude.Value = location.Longitude;
                    squad.Latitude.Value = location.Latitude;

                    SendPredictedMessage(new OverwatchConsoleOrbitalLongitudeBuiMsg(location.Longitude));
                    SendPredictedMessage(new OverwatchConsoleOrbitalLatitudeBuiMsg(location.Latitude));
                }
            );

            squad.HasOrbital = console.HasOrbital;
            squad.NextOrbitalAt = console.NextOrbitalLaunch;
        }
    }

    private void AddSaving(BoxContainer longitudes, BoxContainer latitudes, BoxContainer comments, BoxContainer saves, Thickness margin)
    {
        longitudes.DisposeAllChildren();

        var panel = CreatePanel(50);
        panel.AddChild(new Label
        {
            Text = "LONG.",
            Margin = margin,
        });
        longitudes.AddChild(panel);

        latitudes.DisposeAllChildren();
        panel = CreatePanel(50);
        panel.AddChild(new Label
        {
            Text = "LAT.",
            Margin = margin,
        });
        latitudes.AddChild(panel);

        comments.DisposeAllChildren();
        panel = CreatePanel(50);
        panel.AddChild(new Label
        {
            Text = "COMMENT",
            Margin = margin,
        });
        comments.AddChild(panel);

        saves.DisposeAllChildren();
        panel = CreatePanel(50);
        panel.AddChild(new Label
        {
            Text = " ",
            Margin = margin,
        });

        saves.AddChild(panel);
    }

    private void AddSavedLocation(
        OverwatchSavedLocation?[] locations,
        Thickness margin,
        BoxContainer longitudes,
        BoxContainer latitudes,
        BoxContainer comments,
        BoxContainer saves,
        Action<OverwatchSavedLocation> onSave)
    {
        for (var i = 0; i < locations.Length; i++)
        {
            if (locations[i] is not { } location)
                continue;

            var panel = CreatePanel(50);
            panel.AddChild(new Label
            {
                Text = $"{location.Longitude}",
                Margin = margin,
            });
            longitudes.AddChild(panel);

            panel = CreatePanel(50);
            panel.AddChild(new Label
            {
                Text = $"{location.Latitude}",
                Margin = margin,
            });
            latitudes.AddChild(panel);

            var comment = new LineEdit { Text = $"{location.Comment}" };
            var index = i;
            comment.OnTextEntered += args => SaveComment(index, args.Text);

            panel = CreatePanel(50);
            panel.AddChild(comment);
            comments.AddChild(panel);

            panel = CreatePanel(50);
            var saveButton = new Button
            {
                MaxWidth = 25,
                MaxHeight = 25,
                VerticalAlignment = VAlignment.Top,
                StyleClasses = { "OpenBoth" },
                Text = "<",
                ModulateSelfOverride = Color.FromHex("#D3B400"),
                ToolTip = "Save Comment",
            };
            saveButton.OnPressed += _ => onSave(location);

            panel.AddChild(saveButton);
            saves.AddChild(panel);
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

    private void SaveComment(int index, string text)
    {
        if (text.Length > 50)
            text = text[..50];

        SendPredictedMessage(new OverwatchConsoleLocationCommentBuiMsg(index, text));
    }

    public void Refresh()
    {
        if (State is OverwatchConsoleBuiState s)
            RefreshState(s);
    }

    private readonly record struct OverwatchRow(
        ProtoId<JobPrototype>? RoleId,
        (PanelContainer Panel, Button Button, RichTextLabel Label) Name,
        (PanelContainer Panel, Label Label) Role,
        (PanelContainer Panel, RichTextLabel Label) State,
        (PanelContainer Panel, RichTextLabel Label) Location,
        (PanelContainer Panel, Label Label) Distance,
        (BoxContainer Container, Button Hide, Button Promote) Buttons
    );
}
