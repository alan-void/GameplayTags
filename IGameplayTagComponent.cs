namespace GameplayTags
{
/// <summary>
/// The tag surface an entity offers to systems that grant and revoke tags on it.
/// </summary>
/// <remarks>
/// <see cref="SimTagComponent"/> is the implementation, but it lives with the game's sim
/// entities and so is out of reach of assemblies below it. The ability system only ever reads
/// the tag set and moves counts on it, which is all of this.
/// </remarks>
public interface IGameplayTagComponent
{
    GameplayTagSet TagSet { get; }

    void UpdateTagCount(GameplayTagSet gameplayTagSet, int countDelta);

    void RemoveTags(GameplayTagSet tags);
}
}
