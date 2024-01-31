﻿using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounding.Prototypes;

[Prototype]
[Serializable, NetSerializable]
public sealed class WoundPoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A list of possible wounds and the percentage of damage taken that is needed to apply them
    /// </summary>
    [DataField(required:true,customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<FixedPoint2, EntityPrototype>))]
    public SortedDictionary<FixedPoint2, string> Wounds = new(); //TODO: This may cause an explosion IDK
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class WoundingMetadata
{
    /// <summary>
    /// The uppermost damage for the wound pool
    /// </summary>
    [DataField(required:true)]
    public FixedPoint2 PoolDamageMax = 200;

    /// <summary>
    /// How much to scale incoming damage by
    /// </summary>
    [DataField]
    public FixedPoint2 Scaling = 1;

    /// <summary>
    /// Prototype id for the woundpool we are using for this damage
    /// </summary>
    [DataField(required:true)]
    public ProtoId<WoundPoolPrototype> WoundPool;
}
