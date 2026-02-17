#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class BalanceSimulationTests
    {
        private const int RunsPerProfile = 40;
        private const int MaxSimDays = 72;

        [Test]
        public void DebtEnforcement_RiskyProfile_ShowsHarsherOutcomesThanCautious()
        {
            var eventPool = LoadEventPool();
            Assert.That(eventPool.Count, Is.GreaterThan(0), "Event pool must be loaded from assets.");

            var cautious = RunBatch(ProfileKind.Cautious, eventPool, seedBase: 1000);
            var balanced = RunBatch(ProfileKind.Balanced, eventPool, seedBase: 2000);
            var risky = RunBatch(ProfileKind.Risky, eventPool, seedBase: 3000);

            var cautiousHarshRate = cautious.HarshDebtRate;
            var balancedHarshRate = balanced.HarshDebtRate;
            var riskyHarshRate = risky.HarshDebtRate;

            Debug.Log(BuildSummary(cautious, balanced, risky));
            WriteBalanceReports(cautious, balanced, risky);

            Assert.That(riskyHarshRate, Is.GreaterThan(cautiousHarshRate + 0.10f));
            Assert.That(riskyHarshRate, Is.GreaterThanOrEqualTo(balancedHarshRate));
            Assert.That(risky.DebtEnforcementCount, Is.GreaterThanOrEqualTo(cautious.DebtEnforcementCount));
            Assert.That(cautious.ResilientCount, Is.GreaterThanOrEqualTo(risky.ResilientCount));
            Assert.That(risky.RiskyGambleChoiceCount, Is.GreaterThan(cautious.RiskyGambleChoiceCount));
        }

        private static ProfileBatchResult RunBatch(ProfileKind profileKind, IReadOnlyList<EventData> eventPool, int seedBase)
        {
            var profile = SimulationProfile.For(profileKind);
            var endings = new Dictionary<EndingId, int>();
            var debtEnforcementCount = 0;
            var resilientCount = 0;
            var riskyGambleChoiceCount = 0;
            var harshDebtCount = 0;

            for (var run = 0; run < RunsPerProfile; run++)
            {
                var result = SimulateSingleRun(profile, eventPool, seedBase + run);
                if (!endings.ContainsKey(result.EndingId))
                {
                    endings[result.EndingId] = 0;
                }

                endings[result.EndingId]++;
                riskyGambleChoiceCount += result.RiskyGambleChoiceCount;

                if (result.EndingId == EndingId.DebtEnforcementPrison)
                {
                    debtEnforcementCount++;
                }

                if (result.EndingId == EndingId.GraduatedResilient)
                {
                    resilientCount++;
                }

                if (IsHarshDebtEnding(result.EndingId))
                {
                    harshDebtCount++;
                }
            }

            return new ProfileBatchResult(
                profileKind,
                RunsPerProfile,
                endings,
                debtEnforcementCount,
                resilientCount,
                riskyGambleChoiceCount,
                harshDebtCount);
        }

        private static SingleRunResult SimulateSingleRun(SimulationProfile profile, IReadOnlyList<EventData> eventPool, int seed)
        {
            UnityEngine.Random.InitState(seed);

            var time = new TimeManager();
            var stats = new StatSystem();

            var outcomeConfig = LoadConfigOrDefault<GameOutcomeConfig>("Assets/_Project/ScriptableObjects/Config/GameOutcomeConfig.asset", GameOutcomeConfig.CreateRuntimeDefault);
            var academicConfig = LoadConfigOrDefault<AcademicConfig>("Assets/_Project/ScriptableObjects/Config/AcademicConfig.asset", AcademicConfig.CreateRuntimeDefault);
            var endingDatabase = LoadConfigOrDefault<EndingDatabase>("Assets/_Project/ScriptableObjects/Config/EndingDatabase.asset", EndingDatabase.CreateRuntimeDefault);
            var inflationConfig = LoadConfigOrDefault<InflationShockConfig>("Assets/_Project/ScriptableObjects/Config/InflationShockConfig.asset", InflationShockConfig.CreateRuntimeDefault);
            var mentalConfig = LoadConfigOrDefault<MentalConfig>("Assets/_Project/ScriptableObjects/Config/MentalConfig.asset", MentalConfig.CreateRuntimeDefault);
            var workLimitConfig = LoadConfigOrDefault<WorkLimitConfig>("Assets/_Project/ScriptableObjects/Config/WorkLimitConfig.asset", WorkLimitConfig.CreateRuntimeDefault);
            var cooldownConfig = LoadConfigOrDefault<EventCooldownConfig>("Assets/_Project/ScriptableObjects/Config/EventCooldownConfig.asset", () => EventCooldownConfig.CreateRuntimeDefault(6));
            var sleepDebtConfig = SleepDebtConfig.CreateRuntimeDefault();

            var inflationSystem = new InflationShockSystem(time, inflationConfig);
            var economySystem = new EconomySystem(stats, time, inflationSystem);
            using var sleepDebtSystem = new SleepDebtSystem(time, stats, sleepDebtConfig);
            using var workLimitSystem = new WorkLimitSystem(time, workLimitConfig);
            var eventManager = new EventManager(stats, time);
            using var eventScheduler = new EventScheduler(time, eventManager, stats, eventPool, 1, cooldownConfig);
            using var outcomeSystem = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            using var actionService = new PlayerActionService(
                stats,
                time,
                sleepDebtSystem,
                economySystem,
                outcomeSystem,
                eventScheduler,
                eventManager,
                workLimitSystem,
                mentalConfig,
                inflationSystem);

            var riskyGambleChoiceCount = 0;
            eventManager.OnChoiceApplied += (eventData, choice) =>
            {
                if (eventData == null || choice == null)
                {
                    return;
                }

                if (!string.Equals(eventData.EventId, "EVT_MINOR_GAMBLE_001", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (choice.FollowUpEventIds != null && choice.FollowUpEventIds.Count > 0)
                {
                    riskyGambleChoiceCount++;
                }
            };

            while (!outcomeSystem.IsResolved && time.Day <= MaxSimDays)
            {
                ResolveAllPendingEvents(eventManager, profile, stats);
                ApplyDailyPlan(actionService, stats, profile);
                ResolveAllPendingEvents(eventManager, profile, stats);

                if (!outcomeSystem.IsResolved)
                {
                    TryEndDayAndResolve(actionService, eventManager, profile, stats);
                }
            }

            return new SingleRunResult(
                outcomeSystem.CurrentResult.EndingId,
                riskyGambleChoiceCount);
        }

        private static void ApplyDailyPlan(IPlayerActionService actionService, IStatSystem stats, SimulationProfile profile)
        {
            var money = stats.GetStat(StatType.Money);
            var energy = stats.GetStat(StatType.Energy);
            var mental = stats.GetStat(StatType.Mental);

            if (money <= profile.WorkTriggerMoney && actionService.CanWorkThisWeek)
            {
                actionService.ApplyWork(4, MicroChallengeOutcomeBand.Good);
            }
            else if (energy <= profile.SleepTriggerEnergy)
            {
                actionService.ApplySleep(4);
            }
            else
            {
                actionService.ApplyStudy(4, MicroChallengeOutcomeBand.Good);
            }

            if (mental <= profile.SocializeTriggerMental)
            {
                actionService.ApplySocialize(2);
            }
            else
            {
                actionService.ApplyWait(2);
            }

            actionService.ApplyAdmin(2, MicroChallengeOutcomeBand.Good);
        }

        private static void TryEndDayAndResolve(
            IPlayerActionService actionService,
            IEventManager eventManager,
            SimulationProfile profile,
            IStatSystem stats)
        {
            for (var i = 0; i < 3; i++)
            {
                actionService.TryEndDay(out _);
                ResolveAllPendingEvents(eventManager, profile, stats);
            }
        }

        private static void ResolveAllPendingEvents(IEventManager eventManager, SimulationProfile profile, IStatSystem stats)
        {
            for (var guard = 0; guard < 40; guard++)
            {
                var current = eventManager.CurrentEvent;
                if (current == null)
                {
                    return;
                }

                var available = eventManager.GetAvailableChoices(current);
                if (available == null || available.Count == 0)
                {
                    return;
                }

                var selectedChoice = SelectChoice(current, available, profile, stats);
                var selectedIndex = FindChoiceIndex(current, selectedChoice);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }

                eventManager.TryApplyChoice(current, selectedIndex, out _);
            }
        }

        private static EventChoice SelectChoice(
            EventData eventData,
            IReadOnlyList<EventChoice> availableChoices,
            SimulationProfile profile,
            IStatSystem stats)
        {
            if (eventData != null
                && string.Equals(eventData.EventId, "EVT_MINOR_GAMBLE_001", StringComparison.OrdinalIgnoreCase))
            {
                var riskyChoice = availableChoices.FirstOrDefault(c => c != null && c.FollowUpEventIds != null && c.FollowUpEventIds.Count > 0);
                var safeChoice = availableChoices.FirstOrDefault(c => c != null && (c.FollowUpEventIds == null || c.FollowUpEventIds.Count == 0));

                if (profile.Kind == ProfileKind.Risky)
                {
                    return riskyChoice ?? availableChoices[0];
                }

                if (profile.Kind == ProfileKind.Cautious)
                {
                    return safeChoice ?? availableChoices[0];
                }

                var money = stats.GetStat(StatType.Money);
                if (money < -500f)
                {
                    return riskyChoice ?? availableChoices[0];
                }

                return safeChoice ?? availableChoices[0];
            }

            if (profile.Kind == ProfileKind.Risky)
            {
                return availableChoices
                    .OrderBy(choice => GetMoneyDelta(choice))
                    .FirstOrDefault() ?? availableChoices[0];
            }

            if (profile.Kind == ProfileKind.Cautious)
            {
                return availableChoices
                    .OrderByDescending(choice => GetMoneyDelta(choice))
                    .FirstOrDefault() ?? availableChoices[0];
            }

            return availableChoices[0];
        }

        private static float GetMoneyDelta(EventChoice choice)
        {
            if (choice == null || choice.Effects == null)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < choice.Effects.Count; i++)
            {
                var effect = choice.Effects[i];
                if (effect == null || effect.StatType != StatType.Money)
                {
                    continue;
                }

                total += effect.Delta;
            }

            return total;
        }

        private static int FindChoiceIndex(EventData eventData, EventChoice selectedChoice)
        {
            if (eventData == null || selectedChoice == null || eventData.Choices == null)
            {
                return -1;
            }

            for (var i = 0; i < eventData.Choices.Count; i++)
            {
                if (ReferenceEquals(eventData.Choices[i], selectedChoice))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsHarshDebtEnding(EndingId endingId)
        {
            return endingId == EndingId.DebtEnforcementPrison
                || endingId == EndingId.FailedDebtTrap
                || endingId == EndingId.GraduatedUnemployedDebt
                || endingId == EndingId.ExpelledDebtSpiral;
        }

        private static List<EventData> LoadEventPool()
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/_Project/ScriptableObjects/Events" });
            return guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(AssetDatabase.LoadAssetAtPath<EventData>)
                .Where(e => e != null)
                .ToList();
        }

        private static T LoadConfigOrDefault<T>(string assetPath, Func<T> createDefault) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            return createDefault();
        }

        private static string BuildSummary(ProfileBatchResult cautious, ProfileBatchResult balanced, ProfileBatchResult risky)
        {
            return
                "[BalanceSimulation]\n" +
                cautious.ToLine() + "\n" +
                balanced.ToLine() + "\n" +
                risky.ToLine();
        }

        private static void WriteBalanceReports(ProfileBatchResult cautious, ProfileBatchResult balanced, ProfileBatchResult risky)
        {
            var reportsDir = Path.Combine("Temp", "BalanceReports");
            Directory.CreateDirectory(reportsDir);

            var runStamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var jsonLatestPath = Path.Combine(reportsDir, "latest-balance-summary.json");
            var csvLatestPath = Path.Combine(reportsDir, "latest-balance-summary.csv");
            var jsonRunPath = Path.Combine(reportsDir, $"balance-summary-{runStamp}.json");
            var csvRunPath = Path.Combine(reportsDir, $"balance-summary-{runStamp}.csv");

            var profiles = new[] { cautious, balanced, risky };
            var json = BuildJsonReport(profiles);
            var csv = BuildCsvReport(profiles);

            File.WriteAllText(jsonLatestPath, json, Encoding.UTF8);
            File.WriteAllText(csvLatestPath, csv, Encoding.UTF8);
            File.WriteAllText(jsonRunPath, json, Encoding.UTF8);
            File.WriteAllText(csvRunPath, csv, Encoding.UTF8);

            Debug.Log($"[BalanceSimulation] Reports written:\n- {jsonLatestPath}\n- {csvLatestPath}\n- {jsonRunPath}\n- {csvRunPath}");
        }

        private static string BuildJsonReport(IReadOnlyList<ProfileBatchResult> profiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"generatedAtUtc\": \"{DateTime.UtcNow:O}\",");
            sb.AppendLine($"  \"runsPerProfile\": {RunsPerProfile},");
            sb.AppendLine("  \"profiles\": [");

            for (var i = 0; i < profiles.Count; i++)
            {
                var p = profiles[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"profile\": \"{p.Kind}\",");
                sb.AppendLine($"      \"totalRuns\": {p.TotalRuns},");
                sb.AppendLine($"      \"harshDebtRate\": {p.HarshDebtRate:0.0000},");
                sb.AppendLine($"      \"debtEnforcementCount\": {p.DebtEnforcementCount},");
                sb.AppendLine($"      \"resilientCount\": {p.ResilientCount},");
                sb.AppendLine($"      \"riskyGambleChoiceCount\": {p.RiskyGambleChoiceCount},");
                sb.AppendLine("      \"endingCounts\": {");

                var ordered = p.EndingCounts.OrderBy(kv => kv.Key.ToString()).ToArray();
                for (var j = 0; j < ordered.Length; j++)
                {
                    var suffix = j == ordered.Length - 1 ? string.Empty : ",";
                    sb.AppendLine($"        \"{ordered[j].Key}\": {ordered[j].Value}{suffix}");
                }

                sb.AppendLine("      }");
                sb.Append("    }");
                sb.AppendLine(i == profiles.Count - 1 ? string.Empty : ",");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildCsvReport(IReadOnlyList<ProfileBatchResult> profiles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("profile,total_runs,harsh_debt_rate,debt_enforcement_count,resilient_count,risky_gamble_choice_count,endings");

            for (var i = 0; i < profiles.Count; i++)
            {
                var p = profiles[i];
                var endings = string.Join(
                    ";",
                    p.EndingCounts
                        .OrderByDescending(kv => kv.Value)
                        .Select(kv => $"{kv.Key}:{kv.Value}"));
                sb.AppendLine($"{p.Kind},{p.TotalRuns},{p.HarshDebtRate:0.0000},{p.DebtEnforcementCount},{p.ResilientCount},{p.RiskyGambleChoiceCount},\"{endings}\"");
            }

            return sb.ToString();
        }

        private readonly struct SingleRunResult
        {
            public SingleRunResult(EndingId endingId, int riskyGambleChoiceCount)
            {
                EndingId = endingId;
                RiskyGambleChoiceCount = riskyGambleChoiceCount;
            }

            public EndingId EndingId { get; }
            public int RiskyGambleChoiceCount { get; }
        }

        private readonly struct ProfileBatchResult
        {
            public ProfileBatchResult(
                ProfileKind kind,
                int totalRuns,
                IReadOnlyDictionary<EndingId, int> endingCounts,
                int debtEnforcementCount,
                int resilientCount,
                int riskyGambleChoiceCount,
                int harshDebtCount)
            {
                Kind = kind;
                TotalRuns = totalRuns;
                EndingCounts = endingCounts;
                DebtEnforcementCount = debtEnforcementCount;
                ResilientCount = resilientCount;
                RiskyGambleChoiceCount = riskyGambleChoiceCount;
                HarshDebtCount = harshDebtCount;
            }

            public ProfileKind Kind { get; }
            public int TotalRuns { get; }
            public IReadOnlyDictionary<EndingId, int> EndingCounts { get; }
            public int DebtEnforcementCount { get; }
            public int ResilientCount { get; }
            public int RiskyGambleChoiceCount { get; }
            public int HarshDebtCount { get; }

            public float HarshDebtRate => TotalRuns <= 0 ? 0f : (float)HarshDebtCount / TotalRuns;

            public string ToLine()
            {
                var ordered = EndingCounts
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => $"{kv.Key}:{kv.Value}")
                    .ToArray();
                var endings = string.Join(", ", ordered);
                return $"{Kind} -> HarshRate={HarshDebtRate:0.00}, Prison={DebtEnforcementCount}, Resilient={ResilientCount}, RiskyGambleChoices={RiskyGambleChoiceCount}, Endings=[{endings}]";
            }
        }

        private sealed class SimulationProfile
        {
            private SimulationProfile(
                ProfileKind kind,
                float workTriggerMoney,
                float sleepTriggerEnergy,
                float socializeTriggerMental)
            {
                Kind = kind;
                WorkTriggerMoney = workTriggerMoney;
                SleepTriggerEnergy = sleepTriggerEnergy;
                SocializeTriggerMental = socializeTriggerMental;
            }

            public ProfileKind Kind { get; }
            public float WorkTriggerMoney { get; }
            public float SleepTriggerEnergy { get; }
            public float SocializeTriggerMental { get; }

            public static SimulationProfile For(ProfileKind kind)
            {
                return kind switch
                {
                    ProfileKind.Cautious => new SimulationProfile(kind, 30f, 35f, 40f),
                    ProfileKind.Balanced => new SimulationProfile(kind, -150f, 25f, 30f),
                    ProfileKind.Risky => new SimulationProfile(kind, -99999f, 15f, 20f),
                    _ => new SimulationProfile(ProfileKind.Balanced, -150f, 25f, 30f)
                };
            }
        }

        private enum ProfileKind
        {
            Cautious,
            Balanced,
            Risky
        }
    }
}
#endif
