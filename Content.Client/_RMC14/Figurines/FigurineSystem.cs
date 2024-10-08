using System.IO;
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Shared._RMC14.Figurines;
using Content.Shared.Administration;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.Figurines;

public sealed class FigurineSystem : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly ContentSpriteControl _control = new();

    public override void Initialize()
    {
#if !FULL_RELEASE
        SubscribeNetworkEvent<FigurineRequestEvent>(OnFigurineRequest);

        _ui.RootControl.AddChild(_control);
#endif
    }

    public override void Shutdown()
    {
#if !FULL_RELEASE
        _ui.RootControl.RemoveChild(_control);
#endif
    }

    private void OnFigurineRequest(FigurineRequestEvent ev)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!_adminManager.HasFlag(AdminFlags.Host))
            return;

        if (_player.LocalEntity is not { } ent)
            return;

        if (!TryComp(ent, out SpriteComponent? spriteComp))
            return;

        spriteComp.Scale = Vector2.One;

        // Don't want to wait for engine pr
        var size = Vector2i.Zero;

        foreach (var layer in spriteComp.AllLayers)
        {
            if (!layer.Visible)
                continue;

            size = Vector2i.ComponentMax(size, layer.PixelSize);
        }

        // Stop asserts
        if (size.Equals(Vector2i.Zero))
            return;

        var texture = _clyde.CreateRenderTarget(new Vector2i(size.X, size.Y), new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "export");

        _control.QueuedTextures.Enqueue((texture, Direction.South, ent));
    }

    private sealed class ContentSpriteControl : Control
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        internal readonly Queue<(IRenderTexture Texture, Direction Direction, EntityUid Entity)> QueuedTextures = new();

        public ContentSpriteControl()
        {
            IoCManager.InjectDependencies(this);
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            while (QueuedTextures.TryDequeue(out var queued))
            {
                try
                {
                    var result = queued;
                    handle.RenderInRenderTarget(queued.Texture,
                        () =>
                        {
                            handle.DrawEntity(result.Entity,
                                result.Texture.Size / 2,
                                Vector2.One,
                                Angle.Zero,
                                overrideDirection: result.Direction);
                        },
                        null);

                    queued.Texture.CopyPixelsToMemory<Rgba32>(image =>
                    {
                        var stream = new MemoryStream();
                        image.SaveAsPng(stream);
                        var ev = new FigurineImageEvent(stream.ToArray());
                        _entManager.EntityNetManager.SendSystemNetworkMessage(ev);
                    });
                }
                catch (Exception exc)
                {
                    queued.Texture.Dispose();

                    if (!string.IsNullOrEmpty(exc.StackTrace))
                        Logger.Fatal(exc.StackTrace);
                }
            }
        }
    }
}
