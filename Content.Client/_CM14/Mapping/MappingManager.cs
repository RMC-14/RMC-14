using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._CM14.Mapping;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client._CM14.Mapping;

public sealed class MappingManager : IPostInjectInit
{
    [Dependency] private readonly IFileDialogManager _file = default!;
    [Dependency] private readonly IClientNetManager _net = default!;

    private Stream? _saveStream;
    private MappingMapDataMessage? _mapData;

    public void PostInject()
    {
        _net.RegisterNetMessage<MappingSaveMapMessage>();
        _net.RegisterNetMessage<MappingMapDataMessage>(OnMapData);
    }

    private async void OnMapData(MappingMapDataMessage message)
    {
        if (_saveStream == null)
        {
            _mapData = message;
            return;
        }

        await _saveStream.WriteAsync(Encoding.ASCII.GetBytes(message.Yml));
        await _saveStream.DisposeAsync();

        _saveStream = null;
        _mapData = null;
    }

    public async Task SaveMap()
    {
        var request = new MappingSaveMapMessage();
        _net.ClientSendMessage(request);

        var path = await _file.SaveFile();
        if (path is not { fileStream: var stream })
            return;

        if (_mapData != null)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(_mapData.Yml));
            _mapData = null;
            await stream.DisposeAsync();
            return;
        }

        _saveStream = stream;
    }
}
