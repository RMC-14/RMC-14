using Content.Client._CM14.Xenos.UI;
using Content.Client.IoC;
using Content.Client.Resources;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Pheromones;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Client._CM14.Xenos.Pheromones;

[UsedImplicitly]
public sealed class XenoPheromonesBui : BoundUserInterface
{
    [ViewVariables]
    private XenoPheromonesWindow? _window;

    public XenoPheromonesBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        _window = new XenoPheromonesWindow();
        _window.OnClose += Close;

        if (EntMan.HasComponent<XenoComponent>(Owner))
        {
            foreach (var pheromones in Enum.GetValues<XenoPheromones>())
            {
                var name = pheromones.ToString().ToLowerInvariant();
                var control = new XenoChoiceControl();
                var controlSprite = StaticIoC.ResC.GetTexture(new ResPath($"/Textures/_CM14/Interface/xeno_pheromones.rsi/{name}.png"));
                control.Set(Loc.GetString($"cm-pheromones-{name}"), controlSprite);
                control.Button.OnPressed += _ =>
                {
                    SendMessage(new XenoPheromonesChosenBuiMessage(pheromones));
                    Close();
                };

                _window.Pheromones.AddChild(control);
            }
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
