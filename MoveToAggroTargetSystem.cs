using Game.ClientServer.Ecs.Components;
using Game.ClientServer.Services;
using Game.Server.Ecs.Components;
using Game.Server.Services;
using Zenject;

namespace Game.Server.Ecs.Systems;

/// <summary>
/// Система устанавливает желаемый для перемещения хекс сущности
/// </summary>
public class MoveToAggroTargetSystem : IEcsInitSystem, IEcsRunSystem
{
    [Inject] private HierarchyService _hierarchyService;
    [Inject] private AggroService _aggroService;

    private EcsWorld _world;
    private EcsFilter _filter;

    public void Init(EcsSystems systems)
    {
        _world = systems.GetWorld();

        _filter = _world
            .Filter<AggroTargetEntityComponent>()
            .Inc<MovableTagComponent>()
            .Inc<HexComponent>()
            .Exc<MoveToHexProgressComponent>()
            .Exc<InBattleTagComponent>()
            .End();
    }

    public void Run(EcsSystems systems)
    {
        foreach (var entity in _filter)
        {
            if (!entity.EntityTryGetUnpack<AggroTargetEntityComponent>(_world, out int targetEntity))
                continue;

            targetEntity = _hierarchyService.GetParentOrSelfEntity(targetEntity);

            if (!targetEntity.EntityTryGet(_world, out HexComponent targetHexComponent))
                continue;
            
            if (!_aggroService.CheckEntityCanBeAggroTargetForCurrentEntity(entity, targetEntity))
                continue;

            entity.EntityReplaceRef<WantMoveToTargetHexComponent>(_world).Hex = targetHexComponent.Hex;
        }
    }
}