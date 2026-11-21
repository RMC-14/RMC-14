using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

/// <summary>
/// A control that is to be used in conjunction with <see cref="DraggableJobIcon"/>. These serve as drop targets for the
/// <see cref="DraggableJobIcon"/>. This class handles hover and drop behavior, as well as ensuring that job icons
/// that are added remain sorted.
/// </summary>
public sealed class DraggableJobTarget : Control
{
    /// <summary>
    /// This will be the main "layout" box of the control, which contains the job container and label header
    /// </summary>
    private readonly BoxContainer _mainBox;

    /// <summary>
    /// This panel is what becomes visible when you are dragging an icon over the target
    /// </summary>
    private readonly PanelContainer? _backgroundPanel;

    /// <summary>
    /// This is the main container that holds the job icons. This is a <see cref="GridContainer"/> unless
    /// <see cref="Priority"/> is "High", then it is a <see cref="BoxContainer"/>
    /// </summary>
    private Container? _jobIconContainer;

    /// <summary>
    /// The job priority that this drop target represents.
    /// Setting this updates the visual layout for this target immediately.
    /// </summary>
    public JobPriority Priority
    {
        get => _priority;
        set
        {
            if (_priority == value)
                return;

            _priority = value;
            RebuildLayout();
        }
    }
    private JobPriority _priority = JobPriority.Never;

    /// <summary>
    /// Fired when an icon is dropped over this target.
    /// Drop handling policy should be implemented by the parent control.
    /// </summary>
    public event Action<DraggableJobTarget, DraggableJobIcon>? JobIconDropped;

    public DraggableJobTarget()
    {
        // Add the panel used to highlight the target when hovered
        var panelStyle = new StyleBoxFlat { BackgroundColor = StyleNano.NanoGold };
        _backgroundPanel = new PanelContainer { Visible = false, PanelOverride = panelStyle };
        AddChild(_backgroundPanel);

        // Add the main content box
        _mainBox = new BoxContainer
        {
            Margin = new Thickness(10, 0),
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        AddChild(_mainBox);

        RebuildLayout();
    }

    private void RebuildLayout()
    {
        var existingIcons = _jobIconContainer?.Children.OfType<DraggableJobIcon>().ToList() ?? [];

        _mainBox.RemoveAllChildren();
        _jobIconContainer = null;

        var header = new Label
        {
            Text = Loc.GetString($"humanoid-profile-editor-job-priority-{Priority.ToString().ToLower()}-button"),
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { "LabelBig" },
            Margin = new Thickness(0, 6),
        };
        _mainBox.AddChild(header);

        if (Priority != JobPriority.High)
        {
            _jobIconContainer = new GridContainer
            {
                Columns = 5,
                HSeparationOverride = 0,
                VSeparationOverride = 0,
                HorizontalAlignment = HAlignment.Center,
            };
        }
        else
        {
            _jobIconContainer = new BoxContainer
            {
                Name = "HighBox",
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                MinWidth = 88,
            };
        }

        _mainBox.AddChild(_jobIconContainer);

        foreach (var icon in existingIcons)
        {
            AddJobIcon(icon);
        }
    }

    /// <summary>
    /// Nuke all the jobs in the job container, just like a collection clear
    /// </summary>
    public void ClearIcons()
    {
        _jobIconContainer?.DisposeAllChildren();
    }

    /// <summary>
    /// Register a <see cref="DraggableJobIcon"/>
    /// </summary>
    public void RegisterJobIcon(DraggableJobIcon icon)
    {
        icon.OnMouseMove += HandleMouseMove;
        icon.OnMouseUp += args => HandleMouseUp(args, ref icon);
    }

    /// <summary>
    /// Add a job icon to this control. The icon will be reparented if it is already parented.
    /// </summary>
    /// <param name="icon">Job icon to be added and parented.</param>
    /// <param name="insertIndex">
    /// Optional index in the target container where this icon should be inserted. If null, it is appended.
    /// </param>
    public void AddJobIcon(DraggableJobIcon icon, int? insertIndex = null)
    {
        icon.SetScale(Priority);
        icon.Orphan();
        _jobIconContainer?.AddChild(icon);
        if (insertIndex is { } index && index >= 0)
            icon.SetPositionInParent(index);
    }

    /// <summary>
    /// Check if an icon is hovering above the target on a drag end and handle it if it is.
    /// </summary>
    private void HandleMouseUp(Vector2 pos, ref DraggableJobIcon icon)
    {
        if (!icon.Dragging || !GlobalRect.Contains(pos))
            return;
        if (!icon.TryConsumeDrop())
            return;

        JobIconDropped?.Invoke(this, icon);

        if (_backgroundPanel is not null)
            _backgroundPanel.Visible = false;
    }

    /// <summary>
    /// Check if an icon is hovering above the target and handle the feedback effects
    /// </summary>
    private void HandleMouseMove(Vector2 pos)
    {
        var contained = GlobalRect.Contains(pos);
        if (_backgroundPanel is not null)
            _backgroundPanel.Visible = contained;
    }

    /// <summary>
    /// Get the jobs that are contained in this control.
    /// </summary>
    public IEnumerable<JobPrototype> GetContainedJobs()
    {
        if (_jobIconContainer is null)
            return [];

        return _jobIconContainer.Children.Cast<DraggableJobIcon>().Select(icon => icon.JobProto);
    }

    /// <summary>
    /// Get the draggable icons currently contained in this control.
    /// </summary>
    public IEnumerable<DraggableJobIcon> GetContainedIcons()
    {
        if (_jobIconContainer is null)
            return [];

        return _jobIconContainer.Children.Cast<DraggableJobIcon>();
    }

    /// <summary>
    /// Get the number of jobs contained in this control.
    /// </summary>
    public int ContainedJobCount()
    {
        return GetContainedJobs().Count();
    }

    /// <summary>
    /// Set the column count of the GridContainer in this control
    /// </summary>
    public void SetColumns(int columns)
    {
        // If child count is less than requested columns, just set that instead or else there will be
        // little separators that make the icon not centered.
        // Also GridContainer will throw if you try to set 0 columns.
        if (_jobIconContainer is GridContainer grid)
            grid.Columns = grid.ChildCount == 0 ? 1 : Math.Min(columns, grid.ChildCount);
    }
}
