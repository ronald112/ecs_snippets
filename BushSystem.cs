/// <summary>
/// Система инициализирует группы кустов и переназначает принадлежность сущностей в клетках к группе кустов. 
/// </summary>
public class BushInitSystem : IEcsInitSystem, IEcsRunSystem
{
    private readonly NearestHexesService _nearestHexesService;
    private readonly IGridHexService _gridHexService;
    private readonly NearestHexesSettings _nearestHexesSettings;
    
    private EcsWorld _world;
    private EcsFilter _filterNewEntities;

    private readonly List<Hex> _foundHexes = new List<Hex>();
    private readonly HashSet<Hex> _bushHexes = new HashSet<Hex>();
    
    private int _bushIdCounter = 0;

    public BushInitSystem(NearestHexesService nearestHexesService, IGridHexService gridHexService)
    {
        _nearestHexesService = nearestHexesService;
        _gridHexService = gridHexService;
        _nearestHexesSettings = new NearestHexesSettings(amount: 100);
    }

    public void Init(EcsSystems systems)
    {
        _world = systems.GetWorld();

        _filterNewEntities = _world
            .Filter<InteractableIdComponent>()
            .Inc<NewTagComponent>()
            .Inc<HexComponent>()
            .End();
    }

    public void Run(EcsSystems systems)
    {
        _bushHexes.Clear();
        
        foreach (int entity in _filterNewEntities)
        {
            int id = entity.EntityGet<InteractableIdComponent>(_world).Value;

            if (id != InteractableId.Bush)
                continue;

            Hex hex = entity.EntityGet<HexComponent>(_world).Hex;
            
            if (_bushHexes.Contains(hex))
                continue;

            _nearestHexesService.QueryOnlySameHexes(hex, _nearestHexesSettings, _foundHexes, IsHexBushInternal);
            
            if (_foundHexes.Count <= 0)
                continue;
            
            foreach (Hex bushHex in _foundHexes)
            {
                _bushHexes.Add(bushHex);
                
                if (!_gridHexService.TryQueryFirstEntity(bushHex, IsEntityBush, out int bushEntity))
                    continue;
                
                bushEntity.EntityGetOrCreateRef<BushGroupIdComponent>(_world).Value = _bushIdCounter;
            }

            _bushIdCounter++;
        }
    }
    
    bool IsEntityBush(int checkEntity)
    {
        return checkEntity.EntityTryGet<InteractableIdComponent>(_world, out var idComponent) &&
               idComponent.Value == InteractableId.Bush;
    }

    bool IsHexBushInternal(Hex from, Hex to)
    {
        return _gridHexService.TryQueryFirstEntity(to, IsEntityBush, out _);
    }
}