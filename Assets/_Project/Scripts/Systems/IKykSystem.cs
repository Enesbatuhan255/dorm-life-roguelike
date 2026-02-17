using System;

namespace DormLifeRoguelike
{
    public interface IKykSystem : IService
    {
        event Action<KykStatus> OnStatusChanged;

        KykStatus Status { get; }

        bool IsCut { get; }
    }
}
