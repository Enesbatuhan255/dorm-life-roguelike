namespace DormLifeRoguelike
{
    public sealed class TimeChangedEventArgs
    {
        public TimeChangedEventArgs(int oldDay, int oldHour, int newDay, int newHour, int advancedHours)
        {
            OldDay = oldDay;
            OldHour = oldHour;
            NewDay = newDay;
            NewHour = newHour;
            AdvancedHours = advancedHours;
        }

        public int OldDay { get; }

        public int OldHour { get; }

        public int NewDay { get; }

        public int NewHour { get; }

        public int AdvancedHours { get; }
    }
}
