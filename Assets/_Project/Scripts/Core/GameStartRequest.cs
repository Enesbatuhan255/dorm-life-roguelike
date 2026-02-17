namespace DormLifeRoguelike
{
    public static class GameStartRequest
    {
        public static bool LoadQuickOnBoot { get; private set; }

        public static void RequestQuickLoad()
        {
            LoadQuickOnBoot = true;
        }

        public static bool ConsumeQuickLoad()
        {
            if (!LoadQuickOnBoot)
            {
                return false;
            }

            LoadQuickOnBoot = false;
            return true;
        }

        public static void Reset()
        {
            LoadQuickOnBoot = false;
        }
    }
}
