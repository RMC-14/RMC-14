﻿using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry;

[Serializable, NetSerializable]
public sealed partial class SentryDeployDoAfterEvent : SimpleDoAfterEvent;
