using System;

namespace DormLifeRoguelike
{
    public sealed class WorkLimitSystem : IWorkLimitSystem, IDisposable
    {
        private readonly ITimeManager timeManager;
        private readonly WorkLimitConfig config;
        private int currentWeekIndex;
        private bool isDisposed;

        public WorkLimitSystem(ITimeManager timeManager, WorkLimitConfig config)
        {
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            currentWeekIndex = this.timeManager.WeekIndex;
            WorkActionsUsedThisWeek = 0;
            this.timeManager.OnWeekChanged += HandleWeekChanged;
        }

        public int MaxWorkActionsPerWeek => config.MaxWorkActionsPerWeek;

        public int WorkActionsUsedThisWeek { get; private set; }

        public int RemainingWorkActionsThisWeek => Math.Max(0, MaxWorkActionsPerWeek - WorkActionsUsedThisWeek);

        public bool CanWork()
        {
            return RemainingWorkActionsThisWeek > 0;
        }

        public bool TryConsumeWorkAction()
        {
            if (!CanWork())
            {
                return false;
            }

            WorkActionsUsedThisWeek++;
            return true;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnWeekChanged -= HandleWeekChanged;
        }

        private void HandleWeekChanged(int weekIndex)
        {
            if (weekIndex == currentWeekIndex)
            {
                return;
            }

            currentWeekIndex = weekIndex;
            WorkActionsUsedThisWeek = 0;
        }
    }
}
