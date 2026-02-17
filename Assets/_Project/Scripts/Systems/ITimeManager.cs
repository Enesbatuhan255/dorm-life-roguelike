using System;

namespace DormLifeRoguelike
{
    public interface ITimeManager : IService
    {
        event Action<TimeChangedEventArgs> OnTimeAdvanced;

        event Action<int> OnDayChanged;

        event Action<int> OnWeekChanged;

        event Action<int> OnMonthChanged;

        int Day { get; }

        int Hour { get; }

        int WeekIndex { get; }

        int MonthIndex { get; }

        int TotalDaysInAcademicYear { get; }

        bool IsInExamWindow(int day);

        bool IsKykPayday(int day);

        bool IsInflationShockDay(int day);

        bool IsSecondSemester(int day);

        void AdvanceTime(int hours);

        void SetAbsoluteTimeForLoad(int day, int hour);
    }
}
