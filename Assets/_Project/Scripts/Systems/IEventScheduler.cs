namespace DormLifeRoguelike
{
    public interface IEventScheduler : IService
    {
        void ForceEvaluate();

        EventData PickMinorEventForDay(int day, int hour);

        EventData PickMajorEventForDay(int day, int hour);

        bool TryQueueMinorForCurrentDay();

        bool TryQueueMajorForCurrentDay();
    }
}
