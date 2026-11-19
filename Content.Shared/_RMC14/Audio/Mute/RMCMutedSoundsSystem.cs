using Content.Shared._RMC14.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Audio.Mute;

public sealed class RMCMutedSoundsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly INetConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private const char Separator = ',';

    private readonly Dictionary<string, EntProtoId<RMCMutableSoundComponent>> _byCollection = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        ReloadCategories();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadCategories();
    }

    private void ReloadCategories()
    {
        _byCollection.Clear();

        foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract ||
                !proto.TryGetComponent<RMCMutableSoundComponent>(out var mutable, _compFactory))
            {
                continue;
            }

            foreach (var collection in mutable.Collections)
            {
                if (!_byCollection.TryAdd(collection.Id, proto.ID))
                    Log.Error($"Sound collection {collection.Id} is in more than one mutable sound category");
            }
        }
    }

    public bool TryGetCategory(SoundSpecifier? sound, out EntProtoId<RMCMutableSoundComponent> category)
    {
        if (sound is SoundCollectionSpecifier { Collection: { } collection })
            return _byCollection.TryGetValue(collection, out category);

        category = default;
        return false;
    }

    public bool IsMuted(ICommonSession session, EntProtoId<RMCMutableSoundComponent> category)
    {
        if (_net.IsClient)
            return false;

        var muted = _config.GetClientCVar(session.Channel, RMCCVars.RMCMutedSounds);
        foreach (var id in Parse(muted))
        {
            if (id == category.Id)
                return true;
        }

        return false;
    }

    public Filter RemoveMuted(Filter filter, SoundSpecifier? sound)
    {
        if (_net.IsClient || !TryGetCategory(sound, out var category))
            return filter;

        return filter.RemoveWhere(s => IsMuted(s, category));
    }

    public void PlayPvs(SoundSpecifier? sound, EntityUid uid)
    {
        if (_net.IsClient || !TryGetCategory(sound, out var category))
        {
            _audio.PlayPvs(sound, uid);
            return;
        }

        var filter = Filter.Pvs(uid, entityManager: EntityManager).RemoveWhere(s => IsMuted(s, category));
        if (filter.Count == 0)
            return;

        _audio.PlayEntity(sound, filter, uid, true);
    }

    public void PlayPvs(SoundSpecifier? sound, EntityCoordinates coordinates)
    {
        if (_net.IsClient || !TryGetCategory(sound, out var category))
        {
            _audio.PlayPvs(sound, coordinates);
            return;
        }

        var filter = Filter.Pvs(coordinates, entityMan: EntityManager).RemoveWhere(s => IsMuted(s, category));
        if (filter.Count == 0)
            return;

        _audio.PlayStatic(sound, filter, coordinates, true);
    }

    public static IEnumerable<string> Parse(string muted)
    {
        return muted.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static string Serialize(IEnumerable<string> muted)
    {
        return string.Join(Separator, muted);
    }
}
