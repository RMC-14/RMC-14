using System;
using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Wounds;

[Serializable, NetSerializable]
public readonly record struct EscharEntry(BodyPartType BodyPart, BodyPartSymmetry Symmetry);
