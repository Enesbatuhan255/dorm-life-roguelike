#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const float MinRiskGapVsCautious = 0.05f;
        private const float MaxBalancedHarshRate = 0.35f;
        private const int MinBalancedDistinctEndings = 2;

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

            Assert.That(riskyHarshRate, Is.GreaterThan(cautiousHarshRate + MinRiskGapVsCautious));
            Assert.That(riskyHarshRate, Is.GreaterThanOrEqualTo(balancedHarshRate));
            Assert.That(balancedHarshRate, Is.LessThanOrEqualTo(MaxBalancedHarshRate));
            Assert.That(balanced.DistinctEndingCount, Is.GreaterThanOrEqualTo(MinBalancedDistinctEndings));
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
            var totalFinalAcademic = 0f;
            var totalFinalMoney = 0f;

            for (var run = 0; run < RunsPerProfile; run++)
            {
                var result = SimulateSingleRun(profile, eventPool, seedBase + run);
                if (!endings.ContainsKey(result.EndingId))
                {
                    endings[result.EndingId] = 0;
                }

                endings[result.EndingId]++;
                riskyGambleChoiceCount += result.RiskyGambleChoiceCount;
                totalFinalAcademic += result.FinalAcademic;
                totalFinalMoney += result.FinalMoney;

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
                harshDebtCount,
                totalFinalAcademic / RunsPerProfile,
                totalFinalMoney / RunsPerProfile);
        }

        private static SingleRunResult SimulateSingleRun(SimulationProfile profile, IReadOnlyList<EventData> eventPool, int seed)
        {
            UnityEngine.Random.InitState(seed);
            var behaviorVariant = ComputeBehaviorVariant(profile.Kind, seed);

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

                if (!IsTrackedGambleEvent(eventData.EventId))
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
                ApplyDailyPlan(actionService, stats, profile, time.Day, behaviorVariant);
                ResolveAllPendingEvents(eventManager, profile, stats);

                if (!outcomeSystem.IsResolved)
                {
                    TryEndDayAndResolve(actionService, eventManager, profile, stats);
                }
            }

            return new SingleRunResult(
                outcomeSystem.CurrentResult.EndingId,
                riskyGambleChoiceCount,
                stats.GetStat(StatType.Academic),
                stats.GetStat(StatType.Money));
        }

        private static void ApplyDailyPlan(
            IPlayerActionService actionService,
            IStatSystem stats,
            SimulationProfile profile,
            int day,
            int behaviorVariant)
        {
            var money = stats.GetStat(StatType.Money);
            var energy = stats.GetStat(StatType.Energy);
            var mental = stats.GetStat(StatType.Mental);
            var primaryWorkTriggerMoney = profile.WorkTriggerMoney;
            var secondaryWorkTriggerMoney = profile.SecondaryWorkTriggerMoney;
            var socializeTriggerMental = profile.SocializeTriggerMental;
            var lateTermDebtMoney = profile.LateTermDebtMoney;
            var needsSleepRecovery = energy <= profile.SleepTriggerEnergy || mental <= profile.SleepTriggerMental;

            if (profile.Kind == ProfileKind.Balanced)
            {
                if (behaviorVariant == 0)
                {
                    // Study-first variant: accepts lower money before switching to work.
                    primaryWorkTriggerMoney -= 180f;
                    secondaryWorkTriggerMoney -= 220f;
                    socializeTriggerMental += 4f;
                    lateTermDebtMoney -= 180f;
                }
                else if (behaviorVariant == 2)
                {
                    // Cash-first variant: enters work loops earlier and tolerates less social recovery.
                    primaryWorkTriggerMoney += 220f;
                    secondaryWorkTriggerMoney += 280f;
                    socializeTriggerMental -= 4f;
                    lateTermDebtMoney += 140f;
                }
            }

            var inLateTermDebtMode = day >= profile.LateTermDebtPivotDay && money <= lateTermDebtMoney;
            var emergencyWork = money <= profile.EmergencyWorkMoney;
            var isStudyBurstDay = profile.IsStudyBurstDay(day) && !needsSleepRecovery && !emergencyWork;
            var forceStudyMode = profile.Kind == ProfileKind.Balanced && behaviorVariant == 0;
            var didSecondaryWork = false;

            if (forceStudyMode && !emergencyWork && !inLateTermDebtMode)
            {
                actionService.ApplyStudy(4, profile.StudyBand);
            }
            else if (isStudyBurstDay)
            {
                actionService.ApplyStudy(4, profile.StudyBand);
            }
            else if ((emergencyWork || inLateTermDebtMode || money <= primaryWorkTriggerMoney) && actionService.CanWorkThisWeek)
            {
                actionService.ApplyWork(4, profile.WorkBand);
            }
            else if (needsSleepRecovery)
            {
                actionService.ApplySleep(4);
            }
            else
            {
                actionService.ApplyStudy(4, profile.StudyBand);
            }

            if (forceStudyMode && !emergencyWork && !inLateTermDebtMode)
            {
                actionService.ApplyStudy(2, profile.StudyBand);
            }
            else if ((money <= secondaryWorkTriggerMoney || inLateTermDebtMode) && actionService.CanWorkThisWeek)
            {
                actionService.ApplyWork(2, profile.WorkBand);
                didSecondaryWork = true;
            }
            else if (isStudyBurstDay)
            {
                actionService.ApplyStudy(2, profile.StudyBand);
            }
            else if (mental <= socializeTriggerMental)
            {
                actionService.ApplySocialize(2);
            }
            else
            {
                actionService.ApplyWait(2);
            }

            if (forceStudyMode && !emergencyWork && !inLateTermDebtMode)
            {
                actionService.ApplyStudy(2, profile.StudyBand);
            }
            else
            {
                var adminBand = profile.GetAdminBandForDay(day, didSecondaryWork);
                actionService.ApplyAdmin(2, adminBand);
            }
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
            if (eventData == null || availableChoices == null || availableChoices.Count == 0)
            {
                return null;
            }

            if (TrySelectDebtGambleChoice(eventData, availableChoices, profile, stats, out var specializedChoice))
            {
                return specializedChoice;
            }

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
                if (money < -1200f)
                {
                    return riskyChoice ?? availableChoices[0];
                }

                return safeChoice ?? availableChoices[0];
            }

            return SelectByProfileScoring(availableChoices, profile);
        }

        private static bool TrySelectDebtGambleChoice(
            EventData eventData,
            IReadOnlyList<EventChoice> availableChoices,
            SimulationProfile profile,
            IStatSystem stats,
            out EventChoice selectedChoice)
        {
            selectedChoice = null;
            var eventId = eventData.EventId ?? string.Empty;
            var money = stats.GetStat(StatType.Money);

            if (string.Equals(eventId, "EVT_MAJOR_DEBT_001", StringComparison.OrdinalIgnoreCase))
            {
                var noFollowUpChoice = availableChoices
                    .Where(c => c != null)
                    .OrderBy(c => HasFollowUp(c) ? 1 : 0)
                    .ThenByDescending(GetMoneyDelta)
                    .ThenByDescending(GetMentalDelta)
                    .FirstOrDefault();

                var followUpChoice = availableChoices.FirstOrDefault(HasFollowUp);
                selectedChoice = profile.Kind switch
                {
                    ProfileKind.Risky => followUpChoice ?? noFollowUpChoice,
                    ProfileKind.Balanced when money < -900f => followUpChoice ?? noFollowUpChoice,
                    _ => noFollowUpChoice ?? availableChoices[0]
                };
                return true;
            }

            if (string.Equals(eventId, "EVT_MAJOR_DEBT_002", StringComparison.OrdinalIgnoreCase))
            {
                var maxMoneyChoice = availableChoices
                    .Where(c => c != null)
                    .OrderByDescending(GetMoneyDelta)
                    .ThenByDescending(GetEnergyDelta)
                    .FirstOrDefault();
                var lowPenaltyChoice = availableChoices
                    .Where(c => c != null)
                    .OrderByDescending(GetMentalDelta)
                    .ThenByDescending(GetMoneyDelta)
                    .FirstOrDefault();

                selectedChoice = profile.Kind switch
                {
                    ProfileKind.Risky => maxMoneyChoice ?? availableChoices[0],
                    ProfileKind.Balanced when money < -850f => maxMoneyChoice ?? availableChoices[0],
                    _ => lowPenaltyChoice ?? availableChoices[0]
                };
                return true;
            }

            if (string.Equals(eventId, "EVT_MAJOR_GAMBLE_002", StringComparison.OrdinalIgnoreCase))
            {
                var chaseChoice = availableChoices.FirstOrDefault(c => HasFollowUpTarget(c, "EVT_MAJOR_GAMBLE_003"));
                var safeChoice = availableChoices
                    .Where(c => c != null && !HasFollowUp(c))
                    .OrderByDescending(GetMentalDelta)
                    .ThenByDescending(GetMoneyDelta)
                    .FirstOrDefault();

                selectedChoice = profile.Kind switch
                {
                    ProfileKind.Risky => chaseChoice ?? safeChoice ?? availableChoices[0],
                    ProfileKind.Balanced when money < -950f => chaseChoice ?? safeChoice ?? availableChoices[0],
                    _ => safeChoice ?? availableChoices[0]
                };
                return true;
            }

            if (string.Equals(eventId, "EVT_MAJOR_GAMBLE_003", StringComparison.OrdinalIgnoreCase))
            {
                var aggressiveChoice = availableChoices
                    .Where(c => c != null)
                    .OrderByDescending(GetMoneyDelta)
                    .ThenByDescending(GetEnergyDelta)
                    .FirstOrDefault();
                var lowPenaltyChoice = availableChoices
                    .Where(c => c != null)
                    .OrderByDescending(GetMentalDelta)
                    .ThenByDescending(GetMoneyDelta)
                    .FirstOrDefault();

                selectedChoice = profile.Kind switch
                {
                    ProfileKind.Risky => aggressiveChoice ?? availableChoices[0],
                    ProfileKind.Balanced when money < -800f => aggressiveChoice ?? availableChoices[0],
                    _ => lowPenaltyChoice ?? availableChoices[0]
                };
                return true;
            }

            return false;
        }

        private static EventChoice SelectByProfileScoring(IReadOnlyList<EventChoice> availableChoices, SimulationProfile profile)
        {
            if (profile.Kind == ProfileKind.Risky)
            {
                return availableChoices
                    .Where(c => c != null)
                    .OrderBy(GetMoneyDelta)
                    .FirstOrDefault() ?? availableChoices[0];
            }

            if (profile.Kind == ProfileKind.Cautious)
            {
                return availableChoices
                    .Where(c => c != null)
                    .OrderByDescending(GetMoneyDelta)
                    .FirstOrDefault() ?? availableChoices[0];
            }

            return availableChoices
                .Where(c => c != null)
                .Select(c => new
                {
                    Choice = c,
                    Score = ScoreChoice(c, profile)
                })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Choice)
                .FirstOrDefault() ?? availableChoices[0];
        }

        private static float ScoreChoice(EventChoice choice, SimulationProfile profile)
        {
            var money = GetMoneyDelta(choice);
            var mental = GetMentalDelta(choice);
            var energy = GetEnergyDelta(choice);
            var academic = GetAcademicDelta(choice);
            var followUpPenalty = HasFollowUp(choice) ? 0.5f : 0f;
            var flagRiskPenalty = GetFlagRiskPenalty(choice);

            return profile.Kind switch
            {
                ProfileKind.Cautious => (money * 0.55f) + (mental * 1.10f) + (energy * 0.85f) + (academic * 0.75f) - followUpPenalty - (flagRiskPenalty * 1.2f),
                ProfileKind.Risky => (money * 1.20f) + (mental * 0.30f) + (energy * 0.20f) + (academic * 0.20f) - (flagRiskPenalty * 0.25f),
                _ => (money * 0.85f) + (mental * 0.70f) + (energy * 0.45f) + (academic * 0.40f) - (followUpPenalty * 0.5f) - (flagRiskPenalty * 0.8f)
            };
        }

        private static float GetMoneyDelta(EventChoice choice)
        {
            return GetStatDelta(choice, StatType.Money);
        }

        private static float GetMentalDelta(EventChoice choice)
        {
            return GetStatDelta(choice, StatType.Mental);
        }

        private static float GetEnergyDelta(EventChoice choice)
        {
            return GetStatDelta(choice, StatType.Energy);
        }

        private static float GetAcademicDelta(EventChoice choice)
        {
            return GetStatDelta(choice, StatType.Academic);
        }

        private static float GetStatDelta(EventChoice choice, StatType statType)
        {
            if (choice == null || choice.Effects == null)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < choice.Effects.Count; i++)
            {
                var effect = choice.Effects[i];
                if (effect == null || effect.StatType != statType)
                {
                    continue;
                }

                total += effect.Delta;
            }

            return total;
        }

        private static bool HasFollowUp(EventChoice choice)
        {
            return choice != null
                && choice.FollowUpEventIds != null
                && choice.FollowUpEventIds.Count > 0;
        }

        private static bool HasFollowUpTarget(EventChoice choice, string eventId)
        {
            if (!HasFollowUp(choice) || string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            for (var i = 0; i < choice.FollowUpEventIds.Count; i++)
            {
                if (string.Equals(choice.FollowUpEventIds[i], eventId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetFlagRiskPenalty(EventChoice choice)
        {
            if (choice == null || choice.Flags == null || choice.Flags.Count == 0)
            {
                return 0f;
            }

            var penalty = 0f;
            for (var i = 0; i < choice.Flags.Count; i++)
            {
                var flag = choice.Flags[i];
                if (flag == null || flag.Mode != EventFlagChangeMode.AddNumeric)
                {
                    continue;
                }

                var key = flag.Key ?? string.Empty;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (string.Equals(key, "debt_pressure", StringComparison.OrdinalIgnoreCase))
                {
                    penalty += Math.Max(0f, flag.NumericValue) * 1.2f;
                }
                else if (string.Equals(key, "kyk_risk_days", StringComparison.OrdinalIgnoreCase))
                {
                    penalty += Math.Max(0f, flag.NumericValue) * 1.6f;
                }
                else if (string.Equals(key, "work_strain", StringComparison.OrdinalIgnoreCase))
                {
                    penalty += Math.Max(0f, flag.NumericValue) * 1.0f;
                }
                else if (string.Equals(key, "burnout", StringComparison.OrdinalIgnoreCase))
                {
                    penalty += Math.Max(0f, flag.NumericValue) * 1.4f;
                }
                else if (string.Equals(key, "illegal_fine_pending", StringComparison.OrdinalIgnoreCase))
                {
                    penalty += Math.Max(0f, flag.NumericValue) * 1.8f;
                }
                else if (string.Equals(key, "discipline", StringComparison.OrdinalIgnoreCase))
                {
                    penalty -= Math.Max(0f, flag.NumericValue) * 0.8f;
                }
            }

            return Math.Max(0f, penalty);
        }

        private static bool IsTrackedGambleEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            return string.Equals(eventId, "EVT_MINOR_GAMBLE_001", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventId, "EVT_MAJOR_GAMBLE_002", StringComparison.OrdinalIgnoreCase)
                || string.Equals(eventId, "EVT_MAJOR_GAMBLE_003", StringComparison.OrdinalIgnoreCase);
        }

        private static int ComputeBehaviorVariant(ProfileKind kind, int seed)
        {
            if (kind == ProfileKind.Balanced)
            {
                return Math.Abs(seed % 3);
            }

            return kind == ProfileKind.Risky ? 2 : 0;
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
                var harshDebtRate = p.HarshDebtRate.ToString("0.0000", CultureInfo.InvariantCulture);
                sb.AppendLine("    {");
                sb.AppendLine($"      \"profile\": \"{p.Kind}\",");
                sb.AppendLine($"      \"totalRuns\": {p.TotalRuns},");
                sb.AppendLine($"      \"harshDebtRate\": {harshDebtRate},");
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
            sb.AppendLine("profile,total_runs,harsh_debt_rate,debt_enforcement_count,resilient_count,risky_gamble_choice_count,avg_final_academic,avg_final_money,endings");

            for (var i = 0; i < profiles.Count; i++)
            {
                var p = profiles[i];
                var harshDebtRate = p.HarshDebtRate.ToString("0.0000", CultureInfo.InvariantCulture);
                var endings = string.Join(
                    ";",
                    p.EndingCounts
                        .OrderByDescending(kv => kv.Value)
                        .Select(kv => $"{kv.Key}:{kv.Value}"));
                var avgAcademic = p.AverageFinalAcademic.ToString("0.0000", CultureInfo.InvariantCulture);
                var avgMoney = p.AverageFinalMoney.ToString("0.0", CultureInfo.InvariantCulture);
                sb.AppendLine($"{p.Kind},{p.TotalRuns},{harshDebtRate},{p.DebtEnforcementCount},{p.ResilientCount},{p.RiskyGambleChoiceCount},{avgAcademic},{avgMoney},\"{endings}\"");
            }

            return sb.ToString();
        }

        private readonly struct SingleRunResult
        {
            public SingleRunResult(EndingId endingId, int riskyGambleChoiceCount, float finalAcademic, float finalMoney)
            {
                EndingId = endingId;
                RiskyGambleChoiceCount = riskyGambleChoiceCount;
                FinalAcademic = finalAcademic;
                FinalMoney = finalMoney;
            }

            public EndingId EndingId { get; }
            public int RiskyGambleChoiceCount { get; }
            public float FinalAcademic { get; }
            public float FinalMoney { get; }
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
                int harshDebtCount,
                float averageFinalAcademic,
                float averageFinalMoney)
            {
                Kind = kind;
                TotalRuns = totalRuns;
                EndingCounts = endingCounts;
                DebtEnforcementCount = debtEnforcementCount;
                ResilientCount = resilientCount;
                RiskyGambleChoiceCount = riskyGambleChoiceCount;
                HarshDebtCount = harshDebtCount;
                AverageFinalAcademic = averageFinalAcademic;
                AverageFinalMoney = averageFinalMoney;
            }

            public ProfileKind Kind { get; }
            public int TotalRuns { get; }
            public IReadOnlyDictionary<EndingId, int> EndingCounts { get; }
            public int DebtEnforcementCount { get; }
            public int ResilientCount { get; }
            public int RiskyGambleChoiceCount { get; }
            public int HarshDebtCount { get; }
            public float AverageFinalAcademic { get; }
            public float AverageFinalMoney { get; }
            public int DistinctEndingCount => EndingCounts?.Count ?? 0;

            public float HarshDebtRate => TotalRuns <= 0 ? 0f : (float)HarshDebtCount / TotalRuns;

            public string ToLine()
            {
                var ordered = EndingCounts
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => $"{kv.Key}:{kv.Value}")
                    .ToArray();
                var endings = string.Join(", ", ordered);
                return $"{Kind} -> HarshRate={HarshDebtRate:0.00}, Prison={DebtEnforcementCount}, Resilient={ResilientCount}, RiskyGambleChoices={RiskyGambleChoiceCount}, AvgAcademic={AverageFinalAcademic:0.00}, AvgMoney={AverageFinalMoney:0.0}, Endings=[{endings}]";
            }
        }

        private sealed class SimulationProfile
        {
            private SimulationProfile(
                ProfileKind kind,
                float workTriggerMoney,
                float secondaryWorkTriggerMoney,
                float emergencyWorkMoney,
                float sleepTriggerEnergy,
                float sleepTriggerMental,
                float socializeTriggerMental,
                int lateTermDebtPivotDay,
                float lateTermDebtMoney,
                int studyBurstStartDay,
                int studyBurstIntervalDays,
                bool useAdminWorkTradeoff,
                MicroChallengeOutcomeBand studyBand,
                MicroChallengeOutcomeBand workBand,
                MicroChallengeOutcomeBand adminBand)
            {
                Kind = kind;
                WorkTriggerMoney = workTriggerMoney;
                SecondaryWorkTriggerMoney = secondaryWorkTriggerMoney;
                EmergencyWorkMoney = emergencyWorkMoney;
                SleepTriggerEnergy = sleepTriggerEnergy;
                SleepTriggerMental = sleepTriggerMental;
                SocializeTriggerMental = socializeTriggerMental;
                LateTermDebtPivotDay = lateTermDebtPivotDay;
                LateTermDebtMoney = lateTermDebtMoney;
                StudyBurstStartDay = studyBurstStartDay;
                StudyBurstIntervalDays = studyBurstIntervalDays;
                UseAdminWorkTradeoff = useAdminWorkTradeoff;
                StudyBand = studyBand;
                WorkBand = workBand;
                AdminBand = adminBand;
            }

            public ProfileKind Kind { get; }
            public float WorkTriggerMoney { get; }
            public float SecondaryWorkTriggerMoney { get; }
            public float EmergencyWorkMoney { get; }
            public float SleepTriggerEnergy { get; }
            public float SleepTriggerMental { get; }
            public float SocializeTriggerMental { get; }
            public int LateTermDebtPivotDay { get; }
            public float LateTermDebtMoney { get; }
            public int StudyBurstStartDay { get; }
            public int StudyBurstIntervalDays { get; }
            public bool UseAdminWorkTradeoff { get; }
            public MicroChallengeOutcomeBand StudyBand { get; }
            public MicroChallengeOutcomeBand WorkBand { get; }
            public MicroChallengeOutcomeBand AdminBand { get; }

            public bool IsStudyBurstDay(int day)
            {
                if (StudyBurstIntervalDays <= 0 || day < StudyBurstStartDay)
                {
                    return false;
                }

                return (day - StudyBurstStartDay) % StudyBurstIntervalDays == 0;
            }

            public MicroChallengeOutcomeBand GetAdminBandForDay(int day, bool didSecondaryWork)
            {
                if (!UseAdminWorkTradeoff || !didSecondaryWork)
                {
                    return AdminBand;
                }

                return day % 3 == 0
                    ? MicroChallengeOutcomeBand.Poor
                    : AdminBand;
            }

            public static SimulationProfile For(ProfileKind kind)
            {
                return kind switch
                {
                    ProfileKind.Cautious => new SimulationProfile(
                        kind,
                        workTriggerMoney: 80f,
                        secondaryWorkTriggerMoney: -200f,
                        emergencyWorkMoney: -900f,
                        sleepTriggerEnergy: 40f,
                        sleepTriggerMental: 28f,
                        socializeTriggerMental: 45f,
                        lateTermDebtPivotDay: 56,
                        lateTermDebtMoney: -1300f,
                        studyBurstStartDay: int.MaxValue,
                        studyBurstIntervalDays: 0,
                        useAdminWorkTradeoff: false,
                        studyBand: MicroChallengeOutcomeBand.Good,
                        workBand: MicroChallengeOutcomeBand.Good,
                        adminBand: MicroChallengeOutcomeBand.Good),
                    ProfileKind.Balanced => new SimulationProfile(
                        kind,
                        workTriggerMoney: -800f,
                        secondaryWorkTriggerMoney: -1200f,
                        emergencyWorkMoney: -1700f,
                        sleepTriggerEnergy: 26f,
                        sleepTriggerMental: 24f,
                        socializeTriggerMental: 30f,
                        lateTermDebtPivotDay: 56,
                        lateTermDebtMoney: -1300f,
                        studyBurstStartDay: 3,
                        studyBurstIntervalDays: 3,
                        useAdminWorkTradeoff: true,
                        studyBand: MicroChallengeOutcomeBand.Perfect,
                        workBand: MicroChallengeOutcomeBand.Good,
                        adminBand: MicroChallengeOutcomeBand.Good),
                    ProfileKind.Risky => new SimulationProfile(
                        kind,
                        workTriggerMoney: -99999f,
                        secondaryWorkTriggerMoney: -99999f,
                        emergencyWorkMoney: -99999f,
                        sleepTriggerEnergy: 15f,
                        sleepTriggerMental: 8f,
                        socializeTriggerMental: 18f,
                        lateTermDebtPivotDay: 32,
                        lateTermDebtMoney: -600f,
                        studyBurstStartDay: int.MaxValue,
                        studyBurstIntervalDays: 0,
                        useAdminWorkTradeoff: false,
                        studyBand: MicroChallengeOutcomeBand.Poor,
                        workBand: MicroChallengeOutcomeBand.Poor,
                        adminBand: MicroChallengeOutcomeBand.Poor),
                    _ => new SimulationProfile(
                        ProfileKind.Balanced,
                        workTriggerMoney: -800f,
                        secondaryWorkTriggerMoney: -1200f,
                        emergencyWorkMoney: -1700f,
                        sleepTriggerEnergy: 26f,
                        sleepTriggerMental: 24f,
                        socializeTriggerMental: 30f,
                        lateTermDebtPivotDay: 56,
                        lateTermDebtMoney: -1300f,
                        studyBurstStartDay: 3,
                        studyBurstIntervalDays: 3,
                        useAdminWorkTradeoff: true,
                        studyBand: MicroChallengeOutcomeBand.Perfect,
                        workBand: MicroChallengeOutcomeBand.Good,
                        adminBand: MicroChallengeOutcomeBand.Good)
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
