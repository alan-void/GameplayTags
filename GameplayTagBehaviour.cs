using UnityEngine;

namespace GameplayTags
{
    /// <summary>
    /// Manager what tags this game objects holding.
    /// </summary>
    public class GameplayTagComponent : MonoBehaviour
    {
        [field: SerializeField] 
        public GameplayTagSet TagSet { get; private set; } = new();
        public delegate void OnTagSetAlteredCallback(
            GameplayTagComponent component, GameplayTagSO tag, bool added);
        public event OnTagSetAlteredCallback OnTagSetAltered;
        
        public bool AddTag(GameplayTagSO newTag)
        {
            if(TagSet.AddTag(newTag))
            {
                OnTagSetAltered?.Invoke(this, newTag, true);
                return true;
            }

            return false;
        }

        public bool RemoveTag(GameplayTagSO newTag)
        {
            if (TagSet.RemoveTag(newTag))
            {
                OnTagSetAltered?.Invoke(this, newTag, false);
                return true;
            }
            return false;
        }
        public void AddTags(params GameplayTagSO[] tags)
        {
            foreach (var gameplayTag in tags)
            {
                AddTag(gameplayTag);
            }
        }
        public void AddTags(GameplayTagSet tags)
        {
            foreach (var gameplayTag in tags)
            {
                AddTag(gameplayTag);
            }
        }
        public void RemoveTags(params GameplayTagSO[] tags)
        {
            foreach (var gameplayTag in tags)
            {
                RemoveTag(gameplayTag);
            }
        }
        public void RemoveTags(GameplayTagSet tags)
        {
            foreach (var gameplayTag in tags)
            {
                RemoveTag(gameplayTag);
            }
        }
    }
}