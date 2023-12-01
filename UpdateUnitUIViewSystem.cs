/// <summary>
/// Система реагирует на события ецс мира и меняет интерфейс юнити соответственно
/// </summary>
public class UpdateUnitUIStatusViewSystem : IEcsInitSystem, IEcsRunSystem
{
    [Inject] private DebugVisualService _debugVisualService;
    [Inject] private EntityReferencesService _entityReferencesService;
    [Inject] private readonly GConfigService _configService;
    private EcsWorld _world;

    private EcsPool<UnitBattleDefenceTagComponent> _poolBattleDefence;

    private EcsPool<DeadTagComponent> _poolUnitDeathTag;
    
    private EcsFilter _filterUnits;
    private EcsFilter _filterSkillPrototypes;
    private EcsFilter _filterUpdateNormalHp;
    private EcsFilter _filterUpdateUIPositions;
    private EcsFilter _filterBuildingProgress;
    private EcsFilter _filterBuildingProgressRemoved;
    private EcsFilter _filterUpdateShadowHp;

    public void Init(EcsSystems systems)
    {
        _world = systems.GetWorld();
        
        _filterUpdateNormalHp = _world
            .Filter<TransformComponent>()
            .Inc<UiViewComponent>()
            .Inc<HpComponent>()
            .Inc<MaxHpComponent>()
            .End();
        
        _filterUpdateShadowHp = _world
            .Filter<TransformComponent>()
            .Inc<UiViewComponent>()
            .Inc<ShadowHpComponent>()
            .Inc<ShadowMaxHpComponent>()
            .End();
        
        _filterUnits = _world
            .Filter<UnitTagComponent>()
            .Inc<UiViewComponent>()
            .Inc<TransformComponent>()
            .End();

        _filterSkillPrototypes = _world
            .Filter<SkillPrototypeTagComponent>()
            .End();

        _filterBuildingProgress = _world
            .Filter<BuildingFoundationTagComponent>()
            .Inc<TransformComponent>()
            .Inc<UiViewComponent>()
            .End();

        _filterBuildingProgressRemoved = _world
            .FilterRemoved<BuildingFoundationTagComponent>()
            .Inc<TransformComponent>()
            .Inc<UiViewComponent>()
            .End();
        
        _poolUnitDeathTag = _world.GetPool<DeadTagComponent>();
        _poolBattleDefence = _world.GetPool<UnitBattleDefenceTagComponent>();
    }

    public void Run(EcsSystems systems)
    {
        foreach (var entity in _filterUnits)
        {
            UnitUIViews unitUIViews = entity.EntityGet<UiViewComponent>(_world).Value.GetComponent<UnitUIViews>();
            unitUIViews.skillIdInfoView.SkillInfo("");
            unitUIViews.skillIdInfoView.SkillIds.Clear();
            
            UpdateTextInfoPanel(entity, unitUIViews);
            
            UpdateFriendlyImage(entity, unitUIViews);
            UpdateEnergy(entity, unitUIViews);
            UpdateActionWindow(entity, unitUIViews);
        }

        foreach (var entity in _filterUpdateNormalHp)
        {
            UpdateShield(entity);
            UpdateHp(entity);
        }
        
        foreach (var entity in _filterUpdateShadowHp)
        {
            UpdateShield(entity);
            UpdateShadowHp(entity);
        }

        foreach (var entity in _filterSkillPrototypes)
        {
            UpdateSkillInfo(entity);
        }

        foreach (var entity in _filterBuildingProgress)
        {
            float currentProgress = entity.EntityGet<HpComponent>(_world).Value;
            float maxProgress = entity.EntityGet<MaxHpComponent>(_world).Value;
            
            var p = currentProgress / maxProgress;
            
            UnitUIViews unitUIViews = entity.EntityGet<UiViewComponent>(_world).Value.GetComponent<UnitUIViews>();
            
            unitUIViews.ProgressBarView.gameObject.SetActive(true);
            unitUIViews.ProgressBarView.SetValue(p);
            
            if ((maxProgress - currentProgress) < Mathf.Epsilon)
            {
                unitUIViews.ProgressBarView.gameObject.SetActive(false);
            }
        }
        
        foreach (var entity in _filterBuildingProgressRemoved)
        {
            UnitUIViews unitUIViews = entity.EntityGet<UiViewComponent>(_world).Value.GetComponent<UnitUIViews>();
            unitUIViews.ProgressBarView.gameObject.SetActive(false);
        }
    }

    private void UpdateHp(int entity)
    {
        UnitUIViews unitUIViews = entity.EntityGet<UiViewComponent>(_world).Value.GetComponent<UnitUIViews>();
        if (!_debugVisualService.StatusBars)
        {
            unitUIViews.HpView.gameObject.SetActive(false);
            return;
        }
        float currentHp = entity.EntityGet<HpComponent>(_world).Value;
        float maxHp = entity.EntityGet<MaxHpComponent>(_world).Value;

        var p = currentHp / maxHp;


        unitUIViews.HpView.SetShadowMod(false);
        unitUIViews.HpView.SetValue(p >= 0.01f ? p : 0);
    }

    private void UpdateShadowHp(int entity)
    {
        UnitUIViews unitUIViews = entity.EntityGet<UiViewComponent>(_world).Value.GetComponent<UnitUIViews>();
        if (!_debugVisualService.StatusBars)
        {
            unitUIViews.HpView.gameObject.SetActive(false);
            return;
        }
        float currentHp = entity.EntityGet<ShadowHpComponent>(_world).Value;
        float maxHp = entity.EntityGet<ShadowMaxHpComponent>(_world).Value;

        var p = currentHp / maxHp;


        unitUIViews.HpView.SetShadowMod(true);
        unitUIViews.HpView.SetValue(p >= 0.01f ? p : 0);
    }

    private void UpdateActionWindow(int entity, UnitUIViews unitUIViews)
    {
        if (entity.EntityTryGet<BattleAttackWindowComponent>(_world, out var battleAttackWindowComponent) &&
            _debugVisualService.WindowIndexes)
        {
            unitUIViews.ActionWindowView.gameObject.SetActive(true);
            unitUIViews.ActionWindowView.SetText(battleAttackWindowComponent.Value.ToString());
        }
        else
            unitUIViews.ActionWindowView.gameObject.SetActive(false);
    }

    private void UpdateFriendlyImage(int entity, UnitUIViews unitUIViews)
    {
        if (!_debugVisualService.StatusBars)
        {
            unitUIViews.InfoImageColor.gameObject.SetActive(false);
            return;
        }
        bool isNpc = entity.EntityHas<NpcTagComponent>(_world);
        unitUIViews.InfoImageColor.gameObject.SetActive(!isNpc);

        if (!isNpc)
        {
            unitUIViews.InfoImageColor.SetColor(_poolBattleDefence.Has(entity) ? Color.green : Color.red);
        }

        if (_poolUnitDeathTag.Has(entity))
        {
            unitUIViews.InfoImageColor.SetColor(Color.red);
        }
    }

    private void UpdateTextInfoPanel(int entity, UnitUIViews unitUIViews)
    {
        unitUIViews.InfoTextView.gameObject.SetActive(false);
        if (!_debugVisualService.InfoMassages)
            return;

        string text = "";
        
        if (entity.EntityHas<StunStatusTagComponent>(_world))
        {
            text += "Stunned\n";
        }
        if (entity.EntityHas<TauntStatusTagComponent>(_world))
        {
            text += "Taunted\n";
        }
        if (entity.EntityHas<FreezeEnergyGainStatusTagComponent>(_world))
        {
            text += "Energy Freeze\n";
        }
        if (entity.EntityHas<DeadTagComponent>(_world) &&
            entity.EntityTryGet(_world, out TimerComponent<BetaTimer> timerComponent))
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds((timerComponent.TickValue - _world.GetTick()) * _world.GetDeltaSeconds());
            text += $"Respawn in {((int)timeSpan.TotalMinutes).ToString()}:{timeSpan.Seconds:00}\n";
        }

        if (text.Length > 0)
        {
            unitUIViews.InfoTextView.gameObject.SetActive(true);
            unitUIViews.InfoTextView.SetText(text, 35);
            unitUIViews.InfoTextView.SetColor(Color.white);
        }
    }
    
    private void UpdateEnergy(int entity, UnitUIViews unitUIViews)
    {
        if (!_debugVisualService.StatusBars)
        {
            unitUIViews.EnergyView.gameObject.SetActive(false);
            return;
        }
        if (!entity.EntityTryGet(_world, out EnergyComponent energyComponent) ||
            !entity.EntityTryGet(_world, out StatMaxEnergyComponent maxEnergyComponent))
        {
            return;
        }
        float currEnergy = energyComponent.Value;
        float maxEnergy = maxEnergyComponent.Value;

        var p = currEnergy / maxEnergy;
        unitUIViews.EnergyView.SetValue(p >= 0.01f ? p : 0);
    }

    private void UpdateShield(int unitEntity)
    {
        UnitUIViews unitUIViews = unitEntity.EntityGet<UiViewComponent>(_world).Value.GetComponent<UnitUIViews>();
        if (!_debugVisualService.StatusBars)
        {
            unitUIViews.ShildView.gameObject.SetActive(false);
            unitUIViews.StatusesView.gameObject.SetActive(false);
            return;
        }

        if (!unitEntity.EntityHas<ShieldStatusTagComponent>(_world))
        {
            unitUIViews.StatusesView.UpdateStunIcons(0);
            unitUIViews.ShildView.SetValue(0);
            unitUIViews.ShildView.gameObject.SetActive(false);
            return;
        }

        float shieldScale = 0;
        int shieldsCounter = 0;
        
        
        foreach (var iAmTargetForEntity in _entityReferencesService.IterateUpdatedUnpacked<TargetEntityComponent>(unitEntity))
        {
            if (!iAmTargetForEntity.EntityHas<HitTagComponent>(_world)
                && iAmTargetForEntity.EntityTryGet(_world, out AbilityShieldComponent data))
            {
                shieldScale += data.Value;
                shieldsCounter++;
            }
        }
        

        if (shieldScale <= 0)
        {
            unitUIViews.StatusesView.UpdateStunIcons(0);
            unitUIViews.ShildView.gameObject.SetActive(false);
            return;
        }
        unitUIViews.ShildView.SetValue(shieldScale);
        unitUIViews.StatusesView.UpdateStunIcons(shieldsCounter);
        unitUIViews.StatusesView.gameObject.SetActive(true);
        unitUIViews.ShildView.gameObject.SetActive(true);
    }

    private void UpdateSkillInfo(int skillPrototypeEntity)
    {
        int skillOwner = -1;
        if (!skillPrototypeEntity.EntityTryGetUnpack<OwnerEntityComponent>(_world, out skillOwner))
            return;
        
        if (!skillOwner.EntityTryGet(_world, out UiViewComponent uiViewComponent))
            return;

        UnitUIViews unitUIViews = uiViewComponent.Value.GetComponent<UnitUIViews>();

        if (!_debugVisualService.SkillNames)
        {
            unitUIViews.skillIdInfoView.gameObject.SetActive(false);
            return;
        }
        
        if (skillPrototypeEntity.EntityHas<ConfigIdComponent>(_world))
        {
            var configId = skillPrototypeEntity.EntityGet<ConfigIdComponent>(_world).Value;
            string skillInfo;
            if (_configService.TryGetConfigById(configId, out IGConfig gConfig))
                skillInfo = $"{gConfig.Id}";
            else
                skillInfo = configId.ToString();

            unitUIViews.skillIdInfoView.gameObject.SetActive(true);
            unitUIViews.skillIdInfoView.SkillInfo(skillInfo);
        }
    }
}
}