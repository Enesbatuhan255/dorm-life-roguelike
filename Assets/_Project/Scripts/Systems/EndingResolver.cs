using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    public readonly struct EndingResolution
    {
        public EndingResolution(
            EndingId endingId,
            string epilogTitle,
            string epilogBody,
            DebtBand debtBand,
            EmploymentState employmentState)
        {
            EndingId = endingId;
            EpilogTitle = epilogTitle ?? string.Empty;
            EpilogBody = epilogBody ?? string.Empty;
            DebtBand = debtBand;
            EmploymentState = employmentState;
        }

        public EndingId EndingId { get; }

        public string EpilogTitle { get; }

        public string EpilogBody { get; }

        public DebtBand DebtBand { get; }

        public EmploymentState EmploymentState { get; }
    }

    public static class EndingResolver
    {
        private const string DefaultFallbackTitle = "Hayat Devam Ediyor";
        private const string DefaultFallbackBody = "Sartlar agir. Plan bozuldu ama hikaye bitmedi.";
        private static readonly HashSet<EndingId> MissingEntryWarnings = new HashSet<EndingId>();

        public static EndingResolution Resolve(
            bool isEarlyFailure,
            bool isAcademicPass,
            float mental,
            float energy,
            float money,
            GameOutcomeConfig config,
            EndingDatabase endingDatabase)
        {
            var debtBand = ResolveDebtBand(money, config);
            var endingId = ResolveEndingId(isEarlyFailure, isAcademicPass, mental, energy, money, config);
            var employmentState = ResolveEmploymentState(endingId);

            if (endingDatabase != null && endingDatabase.TryGetEntry(endingId, out var entry))
            {
                return new EndingResolution(
                    endingId,
                    entry.EpilogTitle,
                    entry.EpilogBody,
                    debtBand,
                    employmentState);
            }

            if (!MissingEntryWarnings.Contains(endingId))
            {
                MissingEntryWarnings.Add(endingId);
                Debug.LogWarning($"[EndingResolver] Missing EndingDatabase entry for {endingId}. Using fallback text.");
            }

            var fallbackTitle = endingDatabase != null ? endingDatabase.FallbackTitle : DefaultFallbackTitle;
            var fallbackBody = endingDatabase != null ? endingDatabase.FallbackBody : DefaultFallbackBody;

            return new EndingResolution(
                endingId,
                fallbackTitle,
                fallbackBody,
                debtBand,
                employmentState);
        }

        private static EndingId ResolveEndingId(
            bool isEarlyFailure,
            bool isAcademicPass,
            float mental,
            float energy,
            float money,
            GameOutcomeConfig config)
        {
            if (isEarlyFailure)
            {
                if (money < config.SevereDebtThreshold)
                {
                    return EndingId.ExpelledDebtSpiral;
                }

                if (mental < config.LowMentalThreshold)
                {
                    return EndingId.ExpelledBurnout;
                }

                return EndingId.FailedExtendedYear;
            }

            if (isAcademicPass)
            {
                if (money < config.DebtThreshold)
                {
                    return EndingId.GraduatedUnemployedDebt;
                }

                if (money < config.LightDebtThreshold)
                {
                    return EndingId.GraduatedMinWageDebt;
                }

                if (mental < config.FragileMentalThreshold)
                {
                    return EndingId.GraduatedPrecariousStable;
                }

                if (energy < config.LowEnergyThreshold)
                {
                    return EndingId.GraduatedPrecariousStable;
                }

                return EndingId.GraduatedResilient;
            }

            if (money < config.DebtThreshold)
            {
                return EndingId.FailedDebtTrap;
            }

            return EndingId.FailedExtendedYear;
        }

        private static EmploymentState ResolveEmploymentState(EndingId endingId)
        {
            return endingId switch
            {
                EndingId.ExpelledDebtSpiral => EmploymentState.Unemployed,
                EndingId.ExpelledBurnout => EmploymentState.Unemployed,
                EndingId.GraduatedUnemployedDebt => EmploymentState.Unemployed,
                EndingId.GraduatedMinWageDebt => EmploymentState.MinimumWage,
                EndingId.GraduatedPrecariousStable => EmploymentState.Precarious,
                EndingId.GraduatedResilient => EmploymentState.Stable,
                EndingId.FailedDebtTrap => EmploymentState.Precarious,
                EndingId.FailedExtendedYear => EmploymentState.Precarious,
                _ => EmploymentState.Unknown
            };
        }

        private static DebtBand ResolveDebtBand(float money, GameOutcomeConfig config)
        {
            if (money < config.SevereDebtThreshold)
            {
                return DebtBand.SevereDebt;
            }

            if (money < config.DebtThreshold)
            {
                return DebtBand.HeavyDebt;
            }

            if (money < config.LightDebtThreshold)
            {
                return DebtBand.LightDebt;
            }

            return DebtBand.None;
        }
    }
}
