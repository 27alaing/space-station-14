﻿using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounding.Events;

[ByRefEvent]
public record struct CreateWoundAttemptEvent(
    Entity<WoundableComponent> TargetWoundable,
    Entity<WoundComponent> PossibleWound,
    bool Canceled = false);

[ByRefEvent]
public record struct WoundAppliedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct WoundRemovedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct RemoveWoundAttemptEvent(
    Entity<WoundableComponent> TargetWoundable,
    Entity<WoundComponent> WoundToRemove,
    bool CancelRemove = false);


[ByRefEvent]
public record struct WoundFullyHealedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct WoundableHealAttemptEvent(Entity<WoundableComponent, HealableComponent> TargetWoundable, bool Canceled = false);

[ByRefEvent]
public record struct WoundableHealthChangedEvent(Entity<WoundableComponent> TargetWoundable, FixedPoint2 HealthDelta);

[ByRefEvent]
public record struct WoundableIntegrityChangedEvent(Entity<WoundableComponent> TargetWoundable, FixedPoint2 HealthDelta);

[ByRefEvent]
public record struct WoundHealAttemptEvent(Entity<WoundComponent, HealableComponent> TargetWound, bool Canceled = false);

[ByRefEvent]
public record struct WoundSeverityChangedEvent(Entity<WoundComponent> TargetWound, FixedPoint2 SeverityDelta);
