using System.Linq;
using Content.Shared._RMC14.Marines.Access;
using Content.Shared._RMC14.UserInterface;
using Content.Shared.Access.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Access;

public sealed class IdModificationConsoleBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private readonly ContainerSystem _container;
    public IdModificationConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
    }

    private IdModificationConsoleWindow? _window;

    private readonly HashSet<IdModificationConsoleAccessButton> _accessButtons = [];
    private readonly HashSet<IdModificationConsoleAccessGroupButton> _accessGroupButtons = [];
    private string _currentAccessGroup = "";

    private readonly HashSet<IdModificationConsoleAccessGroupButton> _jobGroupButtons = new();
    private readonly HashSet<IdModificationConsoleAccessButton> _jobButtons = new();
    private string _currentJobGroup = "";

    private readonly HashSet<IdModificationConsoleTabButton> tabs = new();
    private string _currenttab = "";

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out IdModificationConsoleComponent? console))
            return;
        TryContainerEntity(Owner, console.TargetIdSlot, out var target);
        TryContainerEntity(Owner, console.PrivilegedIdSlot, out var privileged);
        EntMan.TryGetComponent(target, out MetaDataComponent? metaData);
        EntMan.TryGetComponent(target, out IdCardComponent? targetCardComponent);
        EntMan.TryGetComponent(target, out AccessComponent? targetCardAccessComponent);

        if (console.Authenticated)
            _window.SignInButton.Text = "Log Out";
        else if (privileged != null)
            _window.SignInButton.Text = "Eject Card";
        else
            _window.SignInButton.Text = "Sign In";

        if (target != null)
        {
            var entityName = metaData?.EntityName ?? "Unknown Name";
            var fullName = targetCardComponent?.FullName ?? "Unknown Name";

            _window.SignInTargetButton.Text = $"Eject Card: {entityName}";
            _window.SignInTargetAccount.Text = $"{fullName}'s Account Number:"; //todo RMC14 Account Numbers
            _window.SignInTargetName.Text = fullName;
        }
        else
        {
            _window.SignInTargetButton.Text = "Insert Id To Modify";
            _window.SignInTargetAccount.Text = "No Card Inserted";
            _window.SignInTargetName.Text = string.Empty;
        }

        foreach (var tab in tabs)
        {
            if (tab.TabButton.Text != _currenttab)
                tab.TabButton.Disabled = false;
        }

        if (console.Authenticated && targetCardComponent != null && targetCardAccessComponent != null)
        {
            _window.TabsContainer.Visible = true;

            // Set all containers to false first, then enable the active one
            _window.AccessContainer.Visible = false;
            _window.JobContainer.Visible = false;
            // _window.RanksContainer.Visible = false;
            _window.SquadContainer.Visible = false;

            switch (_currenttab)
            {
                case "Access":
                    _window.AccessContainer.Visible = true;
                    AccessGroupRefresh(console, targetCardAccessComponent);
                    AccessButtonRefresh(targetCardAccessComponent);
                    RefreshIFFButton(console);
                    break;
                case "Jobs":
                    _window.JobContainer.Visible = true;
                    jobGroupButtonRefresh();
                    break;
                case "Ranks":
                    // _window.RanksContainer.Visible = true;
                    break;
                case "Squads":
                    _window.SquadContainer.Visible = true;
                    DisplaySquads(console);
                    break;
            }
        }
        else
        {
            _window.TabsContainer.Visible = false;
            _window.AccessContainer.Visible = false;
            _window.JobContainer.Visible = false;
            // _window.RanksContainer.Visible = false;
            _window.SquadContainer.Visible = false;
        }
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<IdModificationConsoleWindow>();

        if (!EntMan.TryGetComponent(Owner, out IdModificationConsoleComponent? console))
            return;
        TryContainerEntity(Owner, console.TargetIdSlot, out var target);
        EntMan.TryGetComponent(target, out MetaDataComponent? metaData);
        EntMan.TryGetComponent(target, out IdCardComponent? targetCardComponent);
        EntMan.TryGetComponent(target, out AccessComponent? targetCardAccessComponent);

        _window.SignInButton.OnPressed += _ =>
        {
            SendPredictedMessage(new IdModificationConsoleSignInBuiMsg());
        };

        _window.SignInTargetButton.OnPressed += _ =>
        {
            SendPredictedMessage(new IdModificationConsoleSignInTargetBuiMsg());
        };

        var tab1 = new IdModificationConsoleTabButton();
        tab1.TabButton.Text = "Access";
        tab1.TabButton.Disabled = true;
        tab1.TabButton.OnPressed += _ =>
        {
            _window.AccessGroups.RemoveAllChildren();
            _currentAccessGroup = "";
            _window.Accesses.RemoveAllChildren();
            _currenttab = "Access";
            tab1.TabButton.Disabled = true;

            foreach (var button in _accessGroupButtons)
            {
                _window.AccessGroups.AddChild(button);
            }

            _window.GrantAllButton.Visible = false;
            _window.RevokeAllButton.Visible = false;
            Refresh();
        };
        tabs.Add(tab1);
        _window.Tabs.AddChild(tab1);

        _window.GrantAllButton.OnPressed += _ =>
        {
            SendPredictedMessage(new IdModificationConsoleMultipleAccessChangeBuiMsg("GrantAll", _currentAccessGroup));
            foreach (var button in _accessButtons)
            {
                button.AccessButton.ModulateSelfOverride = Color.Green;
            }
        };

        _window.RevokeAllButton.OnPressed += _ =>
        {
            SendPredictedMessage(new IdModificationConsoleMultipleAccessChangeBuiMsg("RevokeAll", _currentAccessGroup));
            foreach (var button in _accessButtons)
            {
                button.AccessButton.ModulateSelfOverride = null;
            }
        };

        _window.GrantAllGroupButton.OnPressed += _ =>
        {
            SendPredictedMessage(
                new IdModificationConsoleMultipleAccessChangeBuiMsg("GrantAllGroup", _currentAccessGroup));
            foreach (var button in _accessButtons)
            {
                button.AccessButton.ModulateSelfOverride = Color.Green;
            }
        };

        _window.RevokeAllGroupButton.OnPressed += _ =>
        {
            SendPredictedMessage(
                new IdModificationConsoleMultipleAccessChangeBuiMsg("RevokeAllGroup", _currentAccessGroup));
            foreach (var button in _accessButtons)
            {
                button.AccessButton.ModulateSelfOverride = null;
            }
        };

        _window.IFF.OnPressed += _ =>
        {
            SendPredictedMessage(new IdModificationConsoleIFFChangeBuiMsg(console.HasIFF));
            Refresh();
        };

        DisplayAccessGroups(console);

        // Jobs
        var tab2 = new IdModificationConsoleTabButton();
        tab2.TabButton.Text = "Jobs";
        tab2.TabButton.OnPressed += _ =>
        {
            _currenttab = "Jobs";
            _currentJobGroup = "";
            tab2.TabButton.Disabled = true;
            Refresh();
        };
        tabs.Add(tab2);
        _window.Tabs.AddChild(tab2);

        _window.Terminate.OnPressed += _ =>
        {
            _window.Terminate.Visible = false;
            _window.TerminateConfirm.Visible = true;
        };

        _window.TerminateConfirm.OnPressed += _ =>
        {
            _window.TerminateConfirm.Text = "ID Terminated";
            _window.TerminateConfirm.Disabled = true;
            SendPredictedMessage(new IdModificationConsoleTerminateConfirmBuiMsg());
        };

        DisplayJobGroups(console);

        // Todo RMC14 add rank demotion and promotion.
        // var tab3 = new IdModificationConsoleTabButton();
        // tab3.TabButton.Text = "Ranks";
        // tab3.TabButton.OnPressed += _ =>
        // {
        //     currenttab = "Ranks";
        //     tab3.TabButton.Disabled = true;
        //     Refresh();
        // };
        // tabs.Add(tab3);
        // _window.Tabs.AddChild(tab3);

        // Squads
        var tab4 = new IdModificationConsoleTabButton();
        tab4.TabButton.Text = "Squads";
        tab4.TabButton.OnPressed += _ =>
        {
            _currenttab = "Squads";
            tab4.TabButton.Disabled = true;
            Refresh();
        };
        tabs.Add(tab4);
        _window.Tabs.AddChild(tab4);

        _window.SquadClear.OnPressed += _ =>
        {
            SendPredictedMessage(new IdModificationConsoleAssignSquadMsg(null));
            Refresh();
        };

        Refresh();
    }

    private void RefreshIFFButton(IdModificationConsoleComponent console)
    {
        if (_window is not { IsOpen: true })
            return;

        if (console.HasIFF)
        {
            _window.IFF.ModulateSelfOverride = Color.Maroon;
            _window.IFF.Text = "Revoke IFF";
            return;
        }

        _window.IFF.ModulateSelfOverride = Color.Green;
        _window.IFF.Text = "Grant IFF";
    }

    private void DisplayAccessGroups(IdModificationConsoleComponent console)
    {
        if (_window is not { IsOpen: true })
            return;

        _window.AccessGroups.RemoveAllChildren();

        var listAccessGroup = console.AccessGroups.ToList();
        listAccessGroup.Sort();
        var listAccess = console.AccessList.ToList();
        listAccess.Sort();

        foreach (var accessGroup in listAccessGroup)
        {
            if (!_prototype.TryIndex(accessGroup, out var accessGroupPrototype))
                continue;
            var button = new IdModificationConsoleAccessGroupButton();
            button.Tag = accessGroupPrototype.AccessGroup;
            button.AccessButton.HorizontalExpand = true;
            button.AccessButton.SetHeight = 30;
            button.AccessButton.OnPressed += _ =>
            {
                _currentAccessGroup = button.Tag;
                button.AccessButton.ModulateSelfOverride = Color.Green;
                _window.GrantAllButton.Visible = true;
                _window.RevokeAllButton.Visible = true;

                _accessButtons.Clear();
                _window.Accesses.RemoveAllChildren();
                foreach (var access in listAccess)
                {
                    if (!_prototype.TryIndex(access, out var accessPrototype))
                        continue;
                    if (accessPrototype.AccessGroup != accessGroupPrototype.AccessGroup)
                        continue;
                    var accessButton = new IdModificationConsoleAccessButton();
                    if (accessPrototype.Name != null)
                    {
                        accessButton.AccessButton.Text = Loc.GetString(accessPrototype.Name);
                        accessButton.Tag = accessPrototype.Name;
                    }

                    accessButton.AccessButton.HorizontalExpand = true;
                    accessButton.AccessButton.SetHeight = 30;
                    accessButton.AccessButton.OnPressed += _ =>
                    {
                        ToggleAccessButtonColor(accessButton);
                        SendPredictedMessage(new IdModificationConsoleAccessChangeBuiMsg(access,
                            accessButton.AccessButton.ModulateSelfOverride != null));
                        Refresh();
                    };

                    _accessButtons.Add(accessButton);
                    _window.Accesses.AddChild(accessButton);
                    Refresh();
                }
            };
            _accessGroupButtons.Add(button);
        }
    }

    private void DisplayJobGroups(IdModificationConsoleComponent console)
    {
        if (_window is not { IsOpen: true })
            return;

        _window.AccessGroups.RemoveAllChildren();

        var listJobGroup = console.JobGroups.ToList();
        listJobGroup.Sort();
        var listJob = console.JobList.ToList();
        listJob.Sort();

        foreach (var jobGroup in listJobGroup)
        {
            if (!_prototype.TryIndex(jobGroup, out var jobGroupPrototype))
                continue;
            var button = new IdModificationConsoleAccessGroupButton();
            button.Tag = jobGroupPrototype.AccessGroup;
            button.AccessButton.Text = jobGroupPrototype.AccessGroup;
            button.AccessButton.HorizontalExpand = true;
            button.AccessButton.SetHeight = 30;
            button.AccessButton.OnPressed += _ =>
            {
                _currentJobGroup = button.Tag;
                button.AccessButton.ModulateSelfOverride = Color.Green;

                _jobButtons.Clear();
                _window.Jobs.RemoveAllChildren();
                foreach (var job in listJob)
                {
                    if (!_prototype.TryIndex(job, out var jobPrototype))
                        continue;
                    if (jobPrototype.AccessGroup != jobGroupPrototype.AccessGroup)
                        continue;
                    var jobButton = new IdModificationConsoleAccessButton();
                    if (jobPrototype.Name != null)
                    {
                        jobButton.AccessButton.Text = Loc.GetString(jobPrototype.Name);
                        jobButton.Tag = jobPrototype.Name;
                    }

                    jobButton.AccessButton.HorizontalExpand = true;
                    jobButton.AccessButton.SetHeight = 30;
                    jobButton.AccessButton.OnPressed += _ =>
                    {
                        foreach (var jobButtonsToClear in _jobButtons)
                        {
                            jobButtonsToClear.AccessButton.Disabled = true;
                        }

                        jobButton.AccessButton.ModulateSelfOverride = Color.Green;
                        SendPredictedMessage(new IdModificationConsoleJobChangeBuiMsg(jobPrototype));
                        Refresh();
                    };

                    _jobButtons.Add(jobButton);
                    _window.Jobs.AddChild(jobButton);
                    Refresh();
                }
            };
            _jobGroupButtons.Add(button);
            _window.JobGroups.AddChild(button);
        }
    }

    public void DisplaySquads(IdModificationConsoleComponent console)
    {
        if (_window is not { IsOpen: true })
            return;

        _window.Squads.RemoveAllChildren();

        if (console.Squads is not { } squads)
            return;

        foreach (var squad in squads)
        {
            var button = new IdModificationConsoleAccessButton();
            button.AccessButton.Text = squad.Name;
            button.AccessButton.Modulate = squad.Color;
            button.AccessButton.OnPressed += _ =>
            {
                SendPredictedMessage(new IdModificationConsoleAssignSquadMsg(squad.Id));
                Refresh();
            };
            _window.Squads.AddChild(button);
        }
    }

    private void jobGroupButtonRefresh()
    {
        foreach (var button in _jobGroupButtons)
        {
            if (button.Tag != _currentJobGroup)
                button.AccessButton.ModulateSelfOverride = null;
        }
    }

    private void AccessButtonRefresh(AccessComponent access)
    {
        foreach (var button in _accessButtons)
        {
            if (button.AccessButton.Text == null)
                return;

            foreach (var tag in access.Tags)
            {
                if (!_prototype.TryIndex(tag, out var accessPrototype) || accessPrototype.Name == null)
                    continue;

                if (button.Tag != accessPrototype.Name)
                    continue;

                button.AccessButton.ModulateSelfOverride = Color.Green;
                break;
            }
        }
    }

    private void ToggleAccessButtonColor(IdModificationConsoleAccessButton accessButton)
    {
        accessButton.AccessButton.ModulateSelfOverride =
            accessButton.AccessButton.ModulateSelfOverride == null
                ? Color.Green
                : null;
    }

    private void AccessGroupRefresh(IdModificationConsoleComponent console, AccessComponent? targetCardAccessComponent)
    {
        foreach (var obj in _accessGroupButtons)
        {
            if (targetCardAccessComponent != null)
            {
                var text = obj.Tag;
                var counter = 0;
                var counterCompete = 0;
                foreach (var accessLevel in console.AccessList)
                {
                    if (!_prototype.TryIndex(accessLevel, out var accessPrototype))
                        continue;
                    if (accessPrototype.AccessGroup != obj.Tag)
                        continue;
                    counter++;
                    if (targetCardAccessComponent.Tags.Contains(accessLevel))
                        counterCompete++;
                }

                if (counterCompete >= counter)
                    obj.AccessButton.Text = $"[ ◆ ] {text}";
                else if (counterCompete > 0)
                    obj.AccessButton.Text = $"[ ◈ ] {text}";
                else
                    obj.AccessButton.Text = $"[ ◇ ] {text}";
            }
            else
                obj.AccessButton.Text = obj.Tag;

            if (obj.Tag == _currentAccessGroup)
                continue;
            obj.AccessButton.ModulateSelfOverride = null;
            obj.AccessButton.Disabled = false;
        }
    }

    private bool TryContainerEntity(EntityUid ent, string containerType, out EntityUid? contained)
    {
        var container = _container.EnsureContainer<ContainerSlot>(ent, containerType);
        contained = container.ContainedEntity;
        return contained != null;
    }
}
