﻿using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapBlipStatus
{
    Alive = 0,
    Defibabble,
    Undefibabble,
}
