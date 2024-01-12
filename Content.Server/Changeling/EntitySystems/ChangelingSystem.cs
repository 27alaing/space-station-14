using Content.Server.Actions;
using Content.Shared.DoAfter;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Store;
using Content.Server.Traitor.Uplink;
using Content.Shared.Damage;
using Content.Server.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ChangelingComponent, ChangelingEvolutionMenuActionEvent>(OnShop);

        InitializeLingAbilities();
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        _uplink.AddUplink(uid, FixedPoint2.New(10), ChangelingShopPresetPrototype, uid, EvolutionPointsCurrencyPrototype); // not really an 'uplink', but it's there to add the evolution menu
    }

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingEvolutionMenuId = "ActionChangelingEvolutionMenu";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingRegenActionId = "ActionLingRegenerate";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingAbsorbActionId = "ActionChangelingAbsorb";

    [ValidatePrototypeId<CurrencyPrototype>]
    public const string EvolutionPointsCurrencyPrototype = "EvolutionPoints";

    [ValidatePrototypeId<StorePresetPrototype>]
    public const string ChangelingShopPresetPrototype = "StorePresetChangeling";

    public bool ChangeChemicalsAmount(EntityUid uid, float amount, ChangelingComponent? component = null, bool regenCap = true)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Chemicals += amount;

        if (regenCap)
            float.Min(component.Chemicals, component.MaxChemicals);

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        return true;
    }

    private bool TryUseAbility(EntityUid uid, ChangelingComponent component, float abilityCost, bool activated = true, float regenCost = 0f)
    {
        if (component.Chemicals <= Math.Abs(abilityCost) && activated)
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), uid, uid);
            return false;
        }

        if (activated)
        {
            ChangeChemicalsAmount(uid, abilityCost, component, false);
            component.ChemicalsPerSecond -= regenCost;
        }
        else
        {
            component.ChemicalsPerSecond += regenCost;
        }

        return true;
    }

    private void OnMapInit(EntityUid uid, ChangelingComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ShopAction, ChangelingEvolutionMenuId);
        _action.AddAction(uid, ref component.RegenAction, ChangelingRegenActionId);
        _action.AddAction(uid, ref component.AbsorbAction, ChangelingAbsorbActionId);
    }

    private void OnShop(EntityUid uid, ChangelingComponent component, ChangelingEvolutionMenuActionEvent args)
    {
        if (!TryComp(uid, out StoreComponent? store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChangelingComponent>();
        while (query.MoveNext(out var uid, out var ling))
        {
            ling.Accumulator += frameTime;

            if (ling.Accumulator <= 1)
                continue;
            ling.Accumulator -= 1;

            if (_mobState.IsDead(ling.Owner)) // if ling is dead dont regenerate chemicals
                return;

            if (ling.Chemicals < ling.MaxChemicals)
            {
                ChangeChemicalsAmount(uid, ling.ChemicalsPerSecond, ling, regenCap: true);
            }
        }
    }
}
