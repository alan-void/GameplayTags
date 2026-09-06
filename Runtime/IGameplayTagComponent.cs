namespace GameplayTags
{
/// <summary>
/// The tag surface an entity offers to systems that grant and revoke tags on it.
/// </summary>
/// <remarks>
/// The host project supplies the implementation, typically a component wrapping a
/// <see cref="GameplayTagMultiSet"/>. Consumers only read the tag set and move counts on it,
/// which is all of this.
/// </remarks>
public interface IGameplayTagComponent
{
    GameplayTagSet TagSet { get; }

    void UpdateTagCount(GameplayTagSet gameplayTagSet, int countDelta);

    void RemoveTags(GameplayTagSet tags);
}
}
