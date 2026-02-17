using UnityEngine;

namespace DormLifeRoguelike
{
    public readonly struct MicroChallengeInput
    {
        public MicroChallengeInput(float accuracy, float speed, int hits, int mistakes)
        {
            Accuracy = Mathf.Clamp01(accuracy);
            Speed = Mathf.Clamp01(speed);
            Hits = Mathf.Max(0, hits);
            Mistakes = Mathf.Max(0, mistakes);
        }

        public float Accuracy { get; }

        public float Speed { get; }

        public int Hits { get; }

        public int Mistakes { get; }

        public static MicroChallengeInput FromQuality(float quality)
        {
            var normalized = Mathf.Clamp01(quality);
            return new MicroChallengeInput(normalized, normalized, Mathf.RoundToInt(normalized * 10f), Mathf.RoundToInt((1f - normalized) * 3f));
        }
    }
}
