namespace DormLifeRoguelike
{
    public interface IInflationShockSystem : IService
    {
        bool IsTriggered { get; }

        float CurrentMultiplier { get; }

        float ApplyToCost(float amount);
    }
}
