﻿using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem
{
    private void InitWounding()
    {


    }

    /// <summary>
    /// Create a new wound on a woundable from the specified wound prototype
    /// </summary>
    /// <param name="woundableEnt">Target Woundable entity</param>
    /// <param name="woundPrototype">Prototype Id of the wound being spawned</param>
    /// <param name="woundable">Woundable Component</param>
    /// <returns>A woundable entity if successful, null if not</returns>
    public Entity<WoundComponent>? CreateWound(EntityUid  woundableEnt,EntProtoId woundPrototype,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableEnt, ref woundable)
            || EntityManager.TrySpawnInContainer(woundPrototype, woundableEnt,
                WoundableComponent.WoundableContainerId, out var woundEntId)
            || !TryComp(woundEntId, out WoundComponent? wound)
            )
            return null;
        wound.Body = woundable.Body;
        wound.RootWoundable = woundable.RootWoundable;
        SubtractWoundableValues(woundableEnt, woundable, wound.IntegrityDamage, wound.IntegrityDebuff,
            wound.HealthDamage, wound.HealthDebuff);
        return new Entity<WoundComponent>(woundEntId.Value, wound);
    }

    /// <summary>
    /// Remove a wound from its parent woundable and optionally destroy it
    /// </summary>
    /// <param name="woundEnt">Target Wound Entity</param>
    /// <param name="wound">Wound Component</param>
    /// <param name="destroy">Should we destroy the removed wound</param>
    /// <returns>True if succcessful, false if not</returns>
    public bool TryRemoveWound(EntityUid woundEnt, out Entity<WoundComponent>? removedWound, WoundComponent? wound = null,
        bool destroy = true)
    {
        removedWound = null;
        if (!Resolve(woundEnt, ref wound)
            ||! _containerSystem.TryGetContainingContainer(woundEnt, out var woundCont)
            ||! TryComp(woundCont.Owner,out WoundableComponent? woundable)
            ||! _containerSystem.RemoveEntity(woundCont.Owner, woundEnt)
           )
            return false;
        removedWound = new Entity<WoundComponent>(woundEnt, wound);
        SubtractWoundableValues(woundCont.Owner, woundable,-wound.IntegrityDamage, -wound.IntegrityDebuff, -wound.HealthDamage,
            -wound.HealthDebuff);
        Dirty(woundCont.Owner, woundable);
        if (destroy)
        {
            removedWound = null;
            EntityManager.DeleteEntity(woundEnt);
        }
        return true;
    }


    /// <summary>
    /// Tries to get the appropriate wound for the specified damage type and damage amount
    /// </summary>
    /// <param name="woundableEnt">Woundable Entity</param>
    /// <param name="damageType">Damage type to check</param>
    /// <param name="damage">Damage being applied</param>
    /// <param name="woundProtoId">Found WoundProtoId</param>
    /// <param name="woundable">Woundable comp</param>
    /// <returns>True if a woundProto is found, false if not</returns>
    public bool TryGetWoundProtoFromDamage(EntityUid woundableEnt,ProtoId<DamageTypePrototype> damageType, FixedPoint2 damage,
        [NotNullWhen(true)] out EntProtoId? woundProtoId,
        WoundableComponent? woundable = null)
    {
        woundProtoId = null;
        if (!Resolve(woundableEnt, ref woundable)
            || !woundable.Config.TryGetValue(damageType, out var metadata)
            )
            return false;
        var percentageOfMax = damage * metadata.Scaling * 100 / (metadata.PoolDamageMax*100);
        var woundPool = _prototypeManager.Index(metadata.WoundPool);
        foreach (var (percentage, lastWoundProtoId) in woundPool.Wounds)
        {
            if (percentage > percentageOfMax)
                break;
            woundProtoId = lastWoundProtoId;
        }
        return woundProtoId != null;
    }


    /// <summary>
    /// This exists in case you want to force set woundable damage values for some reason!
    /// Since it can cause wound damage to desync from the woundable.
    /// WARNING: Only use this if you know you are doing! This will definitely break shit if used improperly!
    /// </summary>
    /// <param name="woundableEnt">Target Woundable Entity</param>
    /// <param name="woundable">Woundable Component</param>
    /// <param name="integrity">How much integrity damage to set the woundable to</param>
    /// <param name="integrityCap">How much integrity cap to set the woundable to</param>
    /// <param name="health">How much health to set the woundable to</param>
    /// <param name="healthCap">How much health cap to set the woundable to</param>
    private void SetWoundableValues(EntityUid woundableEnt, WoundableComponent woundable,
        FixedPoint2 integrity, FixedPoint2 integrityCap, FixedPoint2 health, FixedPoint2 healthCap)
    {
        woundable.IntegrityCap = FixedPoint2.Clamp(integrityCap, 0, woundable.MaxIntegrity);
        woundable.HealthCap = FixedPoint2.Clamp(healthCap, 0, woundable.MaxHealth);

        woundable.Integrity = FixedPoint2.Clamp(integrity, 0, woundable.IntegrityCap);
        woundable.Health = FixedPoint2.Clamp(health, 0, woundable.HealthCap);
        Dirty(woundableEnt, woundable);
    }

    /// <summary>
    /// Subtract values from a woundable. Use this when you want to update any of the core woundable damage values
    /// This is automatically called when a wound is added to a woundable!
    /// Only use this if you want to directly cause or heal damage on a woundable!
    /// Be careful of causing desyncs with wounds damage!
    /// </summary>
    /// <param name="woundableEnt">target woundable entity</param>
    /// <param name="woundable"></param>
    /// <param name="integrity"></param>
    /// <param name="integrityCap"></param>
    /// <param name="health"></param>
    /// <param name="healthCap"></param>
    private void SubtractWoundableValues(EntityUid woundableEnt, WoundableComponent woundable,
        FixedPoint2 integrity, FixedPoint2 integrityCap, FixedPoint2 health, FixedPoint2 healthCap)
    {
        woundable.IntegrityCap = FixedPoint2.Clamp(woundable.IntegrityCap-integrityCap, 0, woundable.MaxIntegrity);
        woundable.HealthCap = FixedPoint2.Clamp(woundable.HealthCap-healthCap, 0, woundable.MaxHealth);

        woundable.Integrity = FixedPoint2.Clamp(woundable.Integrity-integrity, 0, woundable.IntegrityCap);
        woundable.Health -= FixedPoint2.Clamp(woundable.Health-health, 0, woundable.HealthCap);
        Dirty(woundableEnt, woundable);
    }


}
