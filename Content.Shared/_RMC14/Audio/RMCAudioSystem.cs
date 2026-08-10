using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Audio;

public sealed class RMCAudioSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RMCAudioPlayGlobalEvent>(OnPlayGlobal);
    }

    private void OnPlayGlobal(RMCAudioPlayGlobalEvent msg)
    {
        if (_net.IsServer)
            return;

        var audio = _audio.PlayGlobal(msg.Sound, Filter.Local(), true, msg.AudioParams);
        if (audio != null && !EntityManager.HasComponent(audio.Value.Entity, msg.Component))
            EntityManager.AddComponent(audio.Value.Entity, msg.Component);
    }

    public void PlayGlobal<T>(SoundSpecifier sound, AudioParams audioParams) where T : IComponent, new()
    {
        if (_compFactory.GetRegistration<T>().NetID is { } netId)
        {
            var ev = new RMCAudioPlayGlobalEvent(sound, audioParams, netId);
            RaiseNetworkEvent(ev);
        }

        if (_net.IsClient)
            return;

        var audio = _audio.PlayGlobal(sound, Filter.Empty(), true, audioParams);
        if (audio != null)
            EnsureComp<T>(audio.Value.Entity);
    }
}
