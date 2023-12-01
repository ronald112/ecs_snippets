/// <summary>
/// Система инициализирует события интерфейса связывая их с ЕЦС миром
/// </summary>
public class UI: EventsSystem<TickComponent>.IAnyComponentChangedListener
{
    private readonly ILogger _logger;
    
    [Inject] private MainUI _view;
    [Inject] private EcsWorld _world;
    [Inject] private UIViewManager _uiViewManager;
    [Inject] private ClientPlayerService _clientPlayerService;
    [Inject] private CheatingService _cheatingService;
    [Inject] private IClientInputService _clientInputService;
    [Inject] private TeamService _teamService;
    [Inject] private GConfigService _itemsService;
    [Inject] private CharacterSelectionService _characterSelectionService;
    [Inject] private TalentService _talentService;
    [Inject] private ClientClanService _clientClanService;
    [Inject] private ClientInventoryService _clientInventoryService;
    [InjectOptional] private ClientDummyServices _clientDummyServices;
    

    private int _cachedSkillId = -1;

    [Inject]
    private void Injected()
    {
        _view.Init(_cheatingService);

        _view.OnLateUpdate += () =>
        {
            if (_world.HasUnique<ViewTickSystem.LastViewTickComponent>())
            {
                var ftick = _world.GetUnique<ViewTickSystem.LastViewTickComponent>().FTick;
                _view.Text.text = $"{ftick}";
            }

            _uiViewManager.LateUpdate();
        };

        SetupNewCheats();
        SetupServicePanel();

        _view.TeamControlUI.SelectAllButton.onClick.AddListener(() =>
        {
            if (!_clientPlayerService.TryGetControlledEntity(out int controlledEntity))
                return;

            if (!_teamService.IsMasterEntity(controlledEntity))
                return;

            _characterSelectionService.SelectAllTeamMembers();
        });

        _view.TeamControlUI.DeselectAllButton.onClick.AddListener(() =>
        {
            if (!_clientPlayerService.TryGetControlledEntity(out int controlledEntity))
                return;

            if (!_teamService.IsMasterEntity(controlledEntity))
                return;

            _characterSelectionService.DeselectAllTeamMembers();
        });
    }

    private void SetupNewCheats()
    {
        _view.SetupButton("Toggle clan Visibility", async () =>
        {
            if (!_clientPlayerService.TryGetPlayerEntity(out int playerEntity))
                return;

            _clientInputService.Input(new InputUseClanVisibilityComponent()
            {
                IsOn = !playerEntity.EntityHas<UseClanVisibilityTagComponent>(_world)
            });
        });
        
        _view.SetupButton("InputDestroyEntity", async () =>
        {
            _clientInputService.Input(new InputDestroyEntityComponent
            {
                Entity = await _cheatingService.GetUnitEntityPackedAsync()
            });
        }).WithAsyncEntity();
        
        _view.SetupButton("Toggle unit immortality", async () =>
        {
            var targetUnitPacked = await _cheatingService.GetUnitEntityAsync("select target entity");
            
            _clientInputService.Input(new InputImmortalModComponent()
            {
                Entity = targetUnitPacked.EntityPack(_world),
                IsImmortal = !targetUnitPacked.EntityHas<ImmortalTagComponent>(_world)
            });
        }).WithAsyncEntity();
        
        _view.SetupButton("Attack entity", async () =>
        {
            var targetUnitPacked = await _cheatingService.GetUnitEntityPackedAsync("select target entity");
            
            _clientInputService.Input(new InputAttackEntityComponent()
            {
                Target = targetUnitPacked
            });
        }).WithAsyncEntity();
        
        _view.SetupButton("Auto attack On", async () =>
        {
            _clientInputService.Input(new InputFriendlyModComponent()
            {
                IsFriendly = false
            });
        });
        
        _view.SetupButton("Auto attack Off", async () =>
        {
            _clientInputService.Input(new InputFriendlyModComponent()
            {
                IsFriendly = true
            });
        });
        
        _view.SetupButton("Friendly state on stop", async () =>
        {
            if (!_clientPlayerService.TryGetControlledEntity(out int controlledEntity))
                return;

            controlledEntity.EntityGetOrCreateRef<SetFriendlyModOnStopComponent>(_world).IsFriendly = true;
        });
        
        _view.SetupButton("Battle state on stop", async () =>
        {
            if (!_clientPlayerService.TryGetControlledEntity(out int controlledEntity))
                return;

            controlledEntity.EntityGetOrCreateRef<SetFriendlyModOnStopComponent>(_world).IsFriendly = false;
        });

        _view.SetupButton("Add Resources", async () =>
        {
            await _clientInventoryService.AddItem(GResourceItem.WoodTier1, 10);
            await _clientInventoryService.AddItem(GResourceItem.StoneTier1, 10);
            await _clientInventoryService.AddItem(GResourceItem.Ward, 10);
            await _clientInventoryService.AddItem(GResourceItem.Diamond, 10);
        });
        
        _view.SetupGroup("Dungeon", async () =>
        {
            foreach (var location in ClientServerConfig.Locations)
            {
                _view.SetupButton(location, async () =>
                {
                    _clientInputService.Input(new InputEnterDungeonComponent
                    {
                        LocationName = location
                    });
                });
            }
        });
// ...... Класс очень большой, более 1к строк, но в целом взаимодействие Unity события строилось таким образом //