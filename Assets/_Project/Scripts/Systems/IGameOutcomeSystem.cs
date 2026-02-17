using System;

namespace DormLifeRoguelike
{
    public interface IGameOutcomeSystem : IService
    {
        event Action<GameOutcomeResult> OnOutcomeResolved;
        event Action<GameOutcomeResult> OnGameEnded;

        bool IsResolved { get; }

        GameOutcomeResult CurrentResult { get; }
    }
}
