namespace DormLifeRoguelike
{
    public interface ISnapshotMigrator
    {
        GameSnapshot Migrate(GameSnapshot source);
    }
}
