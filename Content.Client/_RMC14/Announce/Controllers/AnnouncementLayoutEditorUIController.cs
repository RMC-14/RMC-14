using System.Numerics;
using Content.Client.Gameplay;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._RMC14.Announce;

public sealed class AnnouncementLayoutEditorUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public event Action<Vector2>? PreviewPositionChanged;
    public event Action<float>? PreviewScaleChanged;

    private AnnouncementDisplayData? _currentPreview;
    private AnnouncementLayoutEditorOverlayWidget? _overlay;
    public Vector2? CurrentPosition => _overlay?.CurrentPosition;
    public float CurrentScale => _overlay?.CurrentScale ?? 1f;

    public bool ShowPreview(AnnouncementDisplayData announcement)
    {
        _currentPreview = announcement;

        var screen = UIManager.ActiveScreen;
        if (screen == null)
            return false;

        var overlay = GetOrCreateOverlay();
        overlay.ShowPreview(screen, announcement);
        return true;
    }

    public void HidePreview()
    {
        _currentPreview = null;
        _overlay?.HidePreview();
    }

    public void OnStateEntered(GameplayState state)
    {
        if (_currentPreview != null)
            ShowPreview(_currentPreview);
    }

    public void OnStateExited(GameplayState state)
    {
        HidePreview();
    }

    private AnnouncementLayoutEditorOverlayWidget GetOrCreateOverlay()
    {
        if (_overlay == null)
        {
            _overlay = new AnnouncementLayoutEditorOverlayWidget();
            _overlay.PreviewPositionChanged += OnPreviewPositionChanged;
            _overlay.PreviewScaleChanged += OnPreviewScaleChanged;
        }

        if (_overlay.Parent != UIManager.RootControl)
        {
            _overlay.Orphan();
            UIManager.RootControl.AddChild(_overlay);
            _overlay.SetPositionInParent(UIManager.WindowRoot.GetPositionInParent());
        }

        return _overlay;
    }

    private void OnPreviewPositionChanged(Vector2 position)
    {
        PreviewPositionChanged?.Invoke(position);
    }

    private void OnPreviewScaleChanged(float scale)
    {
        PreviewScaleChanged?.Invoke(scale);
    }
}
