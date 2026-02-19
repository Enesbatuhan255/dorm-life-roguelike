using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace DormLifeRoguelike
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap instance;
        private readonly List<EventData> runtimeEventPool = new List<EventData>();

        [Header("Event Scheduler")]
        [SerializeField] private List<EventData> scheduledEvents = new List<EventData>();
        [SerializeField] private int schedulerCheckIntervalHours = 1;
        [SerializeField] private int schedulerCooldownHours = 6;
        [SerializeField] private EventCooldownConfig eventCooldownConfig;

        [Header("Sleep Debt")]
        [SerializeField] private SleepDebtConfig sleepDebtConfig;

        [Header("Mental")]
        [SerializeField] private MentalConfig mentalConfig;

        [Header("Academic")]
        [SerializeField] private AcademicConfig academicConfig;

        [Header("KYK")]
        [SerializeField] private KykConfig kykConfig;

        [Header("Inflation")]
        [SerializeField] private InflationShockConfig inflationShockConfig;

        [Header("Work Limit")]
        [SerializeField] private WorkLimitConfig workLimitConfig;

        [Header("Micro Challenge")]
        [SerializeField] private MicroChallengeConfig microChallengeConfig;

        [Header("Game Outcome")]
        [SerializeField] private GameOutcomeConfig gameOutcomeConfig;
        [SerializeField] private EndingDatabase endingDatabase;

        [Header("Realtime Time Progression")]
        [SerializeField] private bool enableRealtimeTimeProgression = true;
        [SerializeField] private float realtimeSecondsPerGameHour = 20f;
        [SerializeField] private int realtimeHoursPerTick = 1;
        [SerializeField] private bool pauseRealtimeWhileEventActive = true;
        [SerializeField] private bool pauseRealtimeAfterGameResolved = true;
        [SerializeField] private bool requireApplicationFocusForRealtimeProgression = false;

        [Header("UI Mode")]
        [SerializeField] private bool enableFlagDebugPanel = false;
        [SerializeField] private bool enableSaveLoadPanel = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            ServiceLocator.Clear();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            ServiceLocator.Clear();
            ValidateScheduledEventIds();

            var statSystem = new StatSystem();
            var timeManager = new TimeManager();
            var flagStateService = new FlagStateService();
            var eventManager = new EventManager(statSystem, timeManager, flagStateService);
            var flagRuleService = new FlagRuleService(flagStateService, statSystem, timeManager, eventManager);

            var runtimeInflationConfig = inflationShockConfig != null
                ? inflationShockConfig
                : InflationShockConfig.CreateRuntimeDefault();
            var inflationSystem = new InflationShockSystem(timeManager, runtimeInflationConfig);

            var economySystem = new EconomySystem(statSystem, timeManager, inflationSystem);
            var runtimeMicroChallengeConfig = microChallengeConfig != null
                ? microChallengeConfig
                : MicroChallengeConfig.CreateRuntimeDefault();
            var microChallengeSystem = new MicroChallengeService(statSystem, timeManager, economySystem, runtimeMicroChallengeConfig, () => UnityEngine.Random.value);

            var runtimeSleepDebtConfig = sleepDebtConfig != null
                ? sleepDebtConfig
                : SleepDebtConfig.CreateRuntimeDefault();
            var sleepDebtSystem = new SleepDebtSystem(timeManager, statSystem, runtimeSleepDebtConfig);

            var isUsingFallbackCooldownConfig = eventCooldownConfig == null;
            var runtimeCooldownConfig = !isUsingFallbackCooldownConfig
                ? eventCooldownConfig
                : EventCooldownConfig.CreateRuntimeDefault(schedulerCooldownHours);

            var eventScheduler = new EventScheduler(
                timeManager,
                eventManager,
                statSystem,
                BuildRuntimeEventPool(),
                schedulerCheckIntervalHours,
                runtimeCooldownConfig,
                flagStateService);

            var runtimeWorkLimitConfig = workLimitConfig != null
                ? workLimitConfig
                : WorkLimitConfig.CreateRuntimeDefault();
            var workLimitSystem = new WorkLimitSystem(timeManager, runtimeWorkLimitConfig);

            var runtimeAcademicConfig = academicConfig != null
                ? academicConfig
                : AcademicConfig.CreateRuntimeDefault();

            var isUsingFallbackOutcomeConfig = gameOutcomeConfig == null;
            var runtimeGameOutcomeConfig = !isUsingFallbackOutcomeConfig
                ? gameOutcomeConfig
                : GameOutcomeConfig.CreateRuntimeDefault();
            var isUsingFallbackEndingDatabase = endingDatabase == null;
            var runtimeEndingDatabase = !isUsingFallbackEndingDatabase
                ? endingDatabase
                : EndingDatabase.CreateRuntimeDefault();
            var gameOutcomeSystem = new GameOutcomeSystem(
                timeManager,
                statSystem,
                runtimeGameOutcomeConfig,
                runtimeAcademicConfig,
                runtimeEndingDatabase,
                flagStateService);
            var saveLoadService = new SaveLoadService(
                timeManager,
                statSystem,
                flagStateService,
                saveRootPath: null,
                migrator: null,
                eventManager: eventManager,
                eventScheduler: eventScheduler,
                gameOutcomeSystem: gameOutcomeSystem);

            var runtimeMentalConfig = mentalConfig != null
                ? mentalConfig
                : MentalConfig.CreateRuntimeDefault();

            var playerActionService = new PlayerActionService(
                statSystem,
                timeManager,
                sleepDebtSystem,
                economySystem,
                gameOutcomeSystem,
                eventScheduler,
                eventManager,
                workLimitSystem,
                runtimeMentalConfig,
                inflationSystem);
            var dayPlanningService = new DayPlanningService(playerActionService, microChallengeSystem);

            var runtimeKykConfig = kykConfig != null
                ? kykConfig
                : KykConfig.CreateRuntimeDefault();
            var kykSystem = new KykSystem(timeManager, statSystem, economySystem, runtimeKykConfig);

            if (isUsingFallbackOutcomeConfig)
            {
                Debug.LogWarning(
                    "[GameBootstrap] gameOutcomeConfig is not assigned. " +
                    "Using runtime default GameOutcomeConfig fallback. " +
                    "Assign Assets/_Project/ScriptableObjects/Config/GameOutcomeConfig.asset for canonical balancing.");
            }

            if (isUsingFallbackEndingDatabase)
            {
                Debug.LogWarning(
                    "[GameBootstrap] endingDatabase is not assigned. " +
                    "Using runtime default EndingDatabase fallback. " +
                    "Assign Assets/_Project/ScriptableObjects/Config/EndingDatabase.asset for canonical ending narratives.");
            }

            if (isUsingFallbackCooldownConfig)
            {
                Debug.LogWarning(
                    "[GameBootstrap] eventCooldownConfig is not assigned. " +
                    "Using runtime default EventCooldownConfig fallback. " +
                    "Assign Assets/_Project/ScriptableObjects/Config/EventCooldownConfig.asset for canonical balancing.");
            }

            ServiceLocator.Register<IStatSystem>(statSystem);
            ServiceLocator.Register<ITimeManager>(timeManager);
            ServiceLocator.Register<IFlagStateService>(flagStateService);
            ServiceLocator.Register<IFlagRuleService>(flagRuleService);
            ServiceLocator.Register<ISaveLoadService>(saveLoadService);
            ServiceLocator.Register<IEventManager>(eventManager);
            ServiceLocator.Register<IEconomySystem>(economySystem);
            ServiceLocator.Register<IInflationShockSystem>(inflationSystem);
            ServiceLocator.Register<IMicroChallengeService>(microChallengeSystem);
            ServiceLocator.Register<ISleepDebtSystem>(sleepDebtSystem);
            ServiceLocator.Register<IWorkLimitSystem>(workLimitSystem);
            ServiceLocator.Register<IKykSystem>(kykSystem);
            ServiceLocator.Register<IPlayerActionService>(playerActionService);
            ServiceLocator.Register<IDayPlanningService>(dayPlanningService);
            ServiceLocator.Register<IEventScheduler>(eventScheduler);
            ServiceLocator.Register<IGameOutcomeSystem>(gameOutcomeSystem);
            TryHandleStartRequest(saveLoadService);

            EnsureGameOutcomePanelPresenter();
            EnsureMicroChallengePanelPresenter();
            ConfigureOptionalPanels();
            EnsureRealtimeTimeDriver();
        }

        private static void TryHandleStartRequest(ISaveLoadService saveLoadService)
        {
            if (saveLoadService == null || !GameStartRequest.ConsumeQuickLoad())
            {
                return;
            }

            var loaded = saveLoadService.LoadQuick();
            if (!loaded)
            {
                Debug.Log("[GameBootstrap] Continue requested but quick save was not found.");
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<IEventScheduler>(out var scheduler)
                && scheduler is System.IDisposable disposableScheduler)
            {
                disposableScheduler.Dispose();
            }

            if (ServiceLocator.TryGet<IKykSystem>(out var kykSystem)
                && kykSystem is System.IDisposable disposableKykSystem)
            {
                disposableKykSystem.Dispose();
            }

            if (ServiceLocator.TryGet<IWorkLimitSystem>(out var workLimitSystem)
                && workLimitSystem is System.IDisposable disposableWorkLimitSystem)
            {
                disposableWorkLimitSystem.Dispose();
            }

            if (ServiceLocator.TryGet<ISleepDebtSystem>(out var sleepDebtSystem)
                && sleepDebtSystem is System.IDisposable disposableSleepDebtSystem)
            {
                disposableSleepDebtSystem.Dispose();
            }

            if (ServiceLocator.TryGet<IEconomySystem>(out var economySystem)
                && economySystem is System.IDisposable disposableEconomySystem)
            {
                disposableEconomySystem.Dispose();
            }

            if (ServiceLocator.TryGet<IPlayerActionService>(out var playerActionService)
                && playerActionService is System.IDisposable disposablePlayerActionService)
            {
                disposablePlayerActionService.Dispose();
            }

            if (ServiceLocator.TryGet<IInflationShockSystem>(out var inflationSystem)
                && inflationSystem is System.IDisposable disposableInflationSystem)
            {
                disposableInflationSystem.Dispose();
            }

            if (ServiceLocator.TryGet<IFlagRuleService>(out var flagRuleService)
                && flagRuleService is System.IDisposable disposableFlagRuleService)
            {
                disposableFlagRuleService.Dispose();
            }

            if (ServiceLocator.TryGet<ISaveLoadService>(out var saveLoadService)
                && saveLoadService is System.IDisposable disposableSaveLoadService)
            {
                disposableSaveLoadService.Dispose();
            }

            if (ServiceLocator.TryGet<IGameOutcomeSystem>(out var gameOutcomeSystem)
                && gameOutcomeSystem is System.IDisposable disposableGameOutcomeSystem)
            {
                disposableGameOutcomeSystem.Dispose();
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private IReadOnlyList<EventData> BuildRuntimeEventPool()
        {
            runtimeEventPool.Clear();

            for (var i = 0; i < scheduledEvents.Count; i++)
            {
                var eventData = scheduledEvents[i];
                if (eventData != null)
                {
                    runtimeEventPool.Add(eventData);
                }
            }

            if (runtimeEventPool.Count == 0 && TryGetComponent<EventDebugRunner>(out var debugRunner))
            {
                var fallback = debugRunner.ConfiguredEventData;
                if (fallback != null)
                {
                    runtimeEventPool.Add(fallback);
                }
            }

            return runtimeEventPool;
        }

        private void ValidateScheduledEventIds()
        {
            if (scheduledEvents == null || scheduledEvents.Count == 0)
            {
                return;
            }

            var byEventId = new Dictionary<string, List<EventData>>();
            for (var i = 0; i < scheduledEvents.Count; i++)
            {
                var eventData = scheduledEvents[i];
                if (eventData == null || string.IsNullOrWhiteSpace(eventData.EventId))
                {
                    continue;
                }

                var key = eventData.EventId.Trim();
                if (!byEventId.TryGetValue(key, out var list))
                {
                    list = new List<EventData>();
                    byEventId[key] = list;
                }

                list.Add(eventData);
            }

            var hasDuplicate = false;
            var report = new StringBuilder();
            foreach (var pair in byEventId)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                hasDuplicate = true;
                report.Append("- ").Append(pair.Key).Append(": ");
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    report.Append(pair.Value[i].name);
                    if (i < pair.Value.Count - 1)
                    {
                        report.Append(", ");
                    }
                }

                report.AppendLine();
            }

            if (!hasDuplicate)
            {
                return;
            }

            Debug.LogWarning("[GameBootstrap] Duplicate EventId detected in scheduledEvents:\n" + report);
        }

        private static void EnsureGameOutcomePanelPresenter()
        {
            if (FindFirstObjectByType<GameOutcomePanelPresenter>() != null)
            {
                return;
            }

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.gameObject.AddComponent<GameOutcomePanelPresenter>();
                return;
            }

            var fallbackRoot = new GameObject("GameOutcomePanelRoot");
            fallbackRoot.AddComponent<GameOutcomePanelPresenter>();
        }

        private static void EnsureMicroChallengePanelPresenter()
        {
            if (FindFirstObjectByType<MicroChallengePanelPresenter>() != null)
            {
                return;
            }

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.gameObject.AddComponent<MicroChallengePanelPresenter>();
                return;
            }

            var fallbackRoot = new GameObject("MicroChallengePanelRoot");
            fallbackRoot.AddComponent<MicroChallengePanelPresenter>();
        }

        private static void EnsureFlagDebugPanelPresenter()
        {
            if (FindFirstObjectByType<FlagDebugPanelPresenter>() != null)
            {
                return;
            }

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.gameObject.AddComponent<FlagDebugPanelPresenter>();
                return;
            }

            var fallbackRoot = new GameObject("FlagDebugPanelRoot");
            fallbackRoot.AddComponent<FlagDebugPanelPresenter>();
        }

        private static void EnsureSaveLoadPanelPresenter()
        {
            if (FindFirstObjectByType<SaveLoadPanelPresenter>() != null)
            {
                return;
            }

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.gameObject.AddComponent<SaveLoadPanelPresenter>();
                return;
            }

            var fallbackRoot = new GameObject("SaveLoadPanelRoot");
            fallbackRoot.AddComponent<SaveLoadPanelPresenter>();
        }

        private void ConfigureOptionalPanels()
        {
            if (enableFlagDebugPanel)
            {
                EnsureFlagDebugPanelPresenter();
            }
            else
            {
                SetPresenterEnabled<FlagDebugPanelPresenter>(false);
            }

            if (enableSaveLoadPanel)
            {
                EnsureSaveLoadPanelPresenter();
            }
            else
            {
                SetPresenterEnabled<SaveLoadPanelPresenter>(false);
            }
        }

        private static void SetPresenterEnabled<T>(bool isEnabled) where T : Behaviour
        {
            var presenter = FindFirstObjectByType<T>();
            if (presenter == null)
            {
                return;
            }

            presenter.enabled = isEnabled;
            presenter.gameObject.SetActive(isEnabled);
        }

        private void EnsureRealtimeTimeDriver()
        {
            var timeDriver = GetComponent<AutoTimeAdvanceDriver>();
            if (!enableRealtimeTimeProgression)
            {
                if (timeDriver != null)
                {
                    Destroy(timeDriver);
                }

                return;
            }

            if (timeDriver == null)
            {
                timeDriver = gameObject.AddComponent<AutoTimeAdvanceDriver>();
            }

            var shouldRequireFocus = requireApplicationFocusForRealtimeProgression && !Application.isEditor;
            timeDriver.Configure(
                realtimeSecondsPerGameHour,
                realtimeHoursPerTick,
                pauseRealtimeWhileEventActive,
                pauseRealtimeAfterGameResolved,
                shouldRequireFocus);
        }
    }
}
