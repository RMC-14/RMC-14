using System.Numerics;
using Content.Client.Interaction;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Lobby.UI;

/// <summary>
/// This class defines a UI control for a draggable job icon. These elements are to be used with
/// <see cref="DraggableJobTarget"/>
/// </summary>
public sealed class DraggableJobIcon : TextureRect
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    /// <summary>
    /// The TextureScale of a job icon
    /// </summary>
    private const float DefaultScale = 2.6f;

    /// <summary>
    /// The TextureScale of a job icon if it's in the high priority bucket
    /// </summary>
    private const float DefaultHighScale = 4.6f;

    /// <summary>
    /// The job prototype represented by this icon
    /// </summary>
    public JobPrototype JobProto { get; private init; }

    /// <summary>
    /// If the icon is being dragged, this will hold a reference to the control that contained it before dragging.
    /// </summary>
    private Control? _oldParent;

    /// <summary>
    /// If the icon is being dragged, this stores the original <see cref="TextureRect.TextureScale"/> so canceled drags
    /// can restore it.
    /// </summary>
    private Vector2? _oldScale;

    /// <summary>
    /// A transient drag visualization control. The real icon remains in the layout while dragging.
    /// </summary>
    private TextureRect? _dragGhost;

    /// <summary>
    /// Original icon modulate restored when dragging ends.
    /// </summary>
    private Color _oldModulate = Color.White;

    /// <summary>
    /// Helper to check if this icon is being dragged. The icon is being dragged if and only if _oldParent isn't null.
    /// </summary>
    public bool Dragging => _oldParent is not null;

    /// <summary>
    /// Event invoked when the icon has been pressed with UIClick
    /// </summary>
    public event Action<GUIBoundKeyEventArgs>? OnMouseDown;

    /// <summary>
    /// Event invoked when the icon has been released with UIClick
    /// </summary>
    public event Action<Vector2>? OnMouseUp;

    /// <summary>
    /// Event invoked when the mouse has been moved while the icon is being dragged
    /// </summary>
    public event Action<Vector2>? OnMouseMove;

    /// <summary>
    /// Invoked when a drag is about to start, will be canceled if this returns false.
    /// </summary>
    public delegate bool CheckCanDrag();

    /// <summary>
    /// The delegate instance to call to check if dragging should be allowed
    /// </summary>
    private readonly CheckCanDrag? _canDragFunc;
    private readonly DragDropHelper<DraggableJobIcon> _dragDropHelper;
    private bool _dropConsumed;

    public DraggableJobIcon(JobPrototype jobPrototype, CheckCanDrag? checkDrag = null, TooltipSupplier? tooltipSupplier = null)
    {
        IoCManager.InjectDependencies(this);

        JobProto = jobPrototype;

        _canDragFunc = checkDrag;
        _dragDropHelper = new DragDropHelper<DraggableJobIcon>(OnBeginDrag, OnContinueDrag, OnEndDrag);

        var sprite = _entManager.System<SpriteSystem>();
        var iconProto = _prototypeManager.Index(jobPrototype.Icon);

        Texture = sprite.Frame0(iconProto.Icon);
        TextureScale = new Vector2(DefaultScale);
        VerticalAlignment = VAlignment.Center;
        HorizontalAlignment = HAlignment.Center;
        MouseFilter = MouseFilterMode.Pass;

        // Add a little sugar to suppress the tooltip while dragging the icon
        if (tooltipSupplier is not null)
            TooltipSupplier = obj => Dragging ? null : tooltipSupplier(obj);
    }

    /// <summary>
    /// Called after all <see cref="OnMouseUp"/> events are called to clean up the drag event
    /// </summary>
    private void StopDragging()
    {
        var oldParent = _oldParent;
        var oldScale = _oldScale;
        var oldModulate = _oldModulate;
        var dragGhost = _dragGhost;

        _oldParent = null;
        _oldScale = null;
        _dragGhost = null;
        _oldModulate = Color.White;

        _uiManager.DeferAction(() =>
        {
            dragGhost?.Orphan();

            Modulate = oldModulate;

            // If nothing reparented the icon from the drop handling, restore its pre-drag scale.
            if (Parent == oldParent && oldScale is not null)
                TextureScale = oldScale.Value;

        });
    }

    /// <summary>
    /// Start the process of dragging the icon
    /// </summary>
    private void StartDragging()
    {
        // Save the current parent and texture scale
        _oldParent = Parent;
        _oldScale = TextureScale;
        _oldModulate = Modulate;
        _dropConsumed = false;
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 0.35f);

        _dragGhost = new TextureRect
        {
            Texture = Texture,
            TextureScale = TextureScale,
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
        };

        // Put the drag ghost into PopupRoot outside of the current control-tree iteration.
        _uiManager.DeferAction(() =>
        {
            if (!Dragging)
                return;

            if (_dragGhost is null)
                return;

            _uiManager.PopupRoot.AddChild(_dragGhost);
            LayoutContainer.SetPosition(_dragGhost, _uiManager.MousePositionScaled.Position - Size / 2f);
        });
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _dragDropHelper.Update(args.DeltaSeconds);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnMouseDown?.Invoke(args);
        _dragDropHelper.MouseDown(this);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _dragDropHelper.EndDrag();
    }

    /// <summary>
    /// Set the TextureScale of the icon according to the job priority using constants
    /// </summary>
    public void SetScale(JobPriority priority)
    {
        SetScale(priority == JobPriority.High ? DefaultHighScale : DefaultScale);
    }

    /// <summary>
    /// Just a wrapper to set the TextureScale from a single float
    /// </summary>
    private void SetScale(float scale)
    {
        TextureScale = new Vector2(scale);
    }

    private bool OnBeginDrag()
    {
        if (_canDragFunc is not null && !_canDragFunc())
            return false;

        StartDragging();
        return true;
    }

    private bool OnContinueDrag(float frameTime)
    {
        if (!Dragging)
            return false;

        var mousePos = _uiManager.MousePositionScaled.Position;
        if (_dragGhost is not null)
            LayoutContainer.SetPosition(_dragGhost, mousePos - Size / 2f);

        OnMouseMove?.Invoke(mousePos);
        return true;
    }

    private void OnEndDrag()
    {
        if (!Dragging)
            return;

        OnMouseUp?.Invoke(_uiManager.MousePositionScaled.Position);
        StopDragging();
    }

    /// <summary>
    /// Attempt to consume this drop. Returns false if another target already handled it.
    /// </summary>
    public bool TryConsumeDrop()
    {
        if (_dropConsumed)
            return false;

        _dropConsumed = true;
        return true;
    }
}
