using System.Linq;
using Content.Shared.Clothing.Components;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Clothing;

/// <summary>
/// Assigns a loadout to an entity based on the startingGear prototype
/// </summary>
public sealed class LoadoutSystem : EntitySystem
{
    // Shared so we can predict it for placement manager.

    [Dependency] private readonly SharedStationSpawningSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadoutComponent, MapInitEvent>(OnMapInit);
    }

    /// <summary>
    /// Tries to get the name of a loadout.
    /// </summary>
    public string GetName(LoadoutPrototype loadout)
    {
        if (!_protoMan.TryIndex(loadout.Equipment, out var gear))
            return Loc.GetString("loadout-unknown");

        var count = gear.Equipment.Count + gear.Inhand.Count;

        if (count == 1)
        {
            if (gear.Equipment.Count == 1 && _protoMan.TryIndex<EntityPrototype>(gear.Equipment.Values.First(), out var proto))
            {
                return proto.Name;
            }

            if (gear.Inhand.Count == 1 && _protoMan.TryIndex<EntityPrototype>(gear.Inhand[0], out proto))
            {
                return proto.Name;
            }
        }

        return Loc.GetString($"loadout-{loadout.ID}");
    }

    private void OnMapInit(EntityUid uid, LoadoutComponent component, MapInitEvent args)
    {
        if (component.Prototypes == null)
            return;

        var proto = _protoMan.Index<StartingGearPrototype>(_random.Pick(component.Prototypes));
        _station.EquipStartingGear(uid, proto);
    }
}
