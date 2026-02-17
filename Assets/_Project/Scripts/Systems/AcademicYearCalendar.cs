using System;

namespace DormLifeRoguelike
{
    public static class AcademicYearCalendar
    {
        public const int TotalDays = 72;
        public const int FirstSemesterLastDay = 36;
        public const int InflationShockDay = 37;

        public static bool IsSecondSemester(int day)
        {
            return NormalizeDay(day) > FirstSemesterLastDay;
        }

        public static bool IsInflationShockDay(int day)
        {
            return NormalizeDay(day) == InflationShockDay;
        }

        public static bool IsKykPayday(int day)
        {
            var normalized = NormalizeDay(day);
            return normalized == 1
                || normalized == 15
                || normalized == 29
                || normalized == 43
                || normalized == 57
                || normalized == 71;
        }

        public static bool IsInExamWindow(int day)
        {
            var normalized = NormalizeDay(day);
            return IsInRange(normalized, 16, 19)
                || IsInRange(normalized, 34, 36)
                || IsInRange(normalized, 52, 55)
                || IsInRange(normalized, 70, 72);
        }

        private static bool IsInRange(int value, int minInclusive, int maxInclusive)
        {
            return value >= minInclusive && value <= maxInclusive;
        }

        private static int NormalizeDay(int day)
        {
            return Math.Clamp(day, 1, TotalDays);
        }
    }
}
