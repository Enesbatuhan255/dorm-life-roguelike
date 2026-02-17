using System;

namespace DormLifeRoguelike
{
    public sealed class TimeManager : ITimeManager
    {
        public TimeManager()
        {
            Day = 1;
            Hour = 8;
            WeekIndex = GetWeekIndex(Day);
            MonthIndex = GetMonthIndex(Day);
        }

        public event Action<TimeChangedEventArgs> OnTimeAdvanced;

        public event Action<int> OnDayChanged;

        public event Action<int> OnWeekChanged;

        public event Action<int> OnMonthChanged;

        public int Day { get; private set; }

        public int Hour { get; private set; }

        public int WeekIndex { get; private set; }

        public int MonthIndex { get; private set; }

        public int TotalDaysInAcademicYear => AcademicYearCalendar.TotalDays;

        public bool IsInExamWindow(int day)
        {
            return AcademicYearCalendar.IsInExamWindow(day);
        }

        public bool IsKykPayday(int day)
        {
            return AcademicYearCalendar.IsKykPayday(day);
        }

        public bool IsInflationShockDay(int day)
        {
            return AcademicYearCalendar.IsInflationShockDay(day);
        }

        public bool IsSecondSemester(int day)
        {
            return AcademicYearCalendar.IsSecondSemester(day);
        }

        public void AdvanceTime(int hours)
        {
            if (hours <= 0)
            {
                return;
            }

            var oldDay = Day;
            var oldHour = Hour;

            Hour += hours;

            while (Hour >= 24)
            {
                Hour -= 24;
                Day++;
                OnDayChanged?.Invoke(Day);

                var nextWeek = GetWeekIndex(Day);
                if (nextWeek != WeekIndex)
                {
                    WeekIndex = nextWeek;
                    OnWeekChanged?.Invoke(WeekIndex);
                }

                var nextMonth = GetMonthIndex(Day);
                if (nextMonth != MonthIndex)
                {
                    MonthIndex = nextMonth;
                    OnMonthChanged?.Invoke(MonthIndex);
                }
            }

            OnTimeAdvanced?.Invoke(new TimeChangedEventArgs(oldDay, oldHour, Day, Hour, hours));
        }

        public void SetAbsoluteTimeForLoad(int day, int hour)
        {
            Day = Math.Max(day, 1);
            Hour = Math.Clamp(hour, 0, 23);
            WeekIndex = GetWeekIndex(Day);
            MonthIndex = GetMonthIndex(Day);
            OnTimeAdvanced?.Invoke(new TimeChangedEventArgs(Day, Hour, Day, Hour, 0));
        }

        private static int GetWeekIndex(int day)
        {
            return ((Math.Max(day, 1) - 1) / 7) + 1;
        }

        private static int GetMonthIndex(int day)
        {
            return ((Math.Max(day, 1) - 1) / 30) + 1;
        }
    }
}
