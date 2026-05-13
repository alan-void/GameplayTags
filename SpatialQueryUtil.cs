using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
/// <summary>
/// Utility helpers for performing spatial queries against the <see cref="TagManager"/>.
/// </summary>
public static class SpatialQueryUtil
{
    /// <summary>
    /// Attempts to find the nearest <see cref="SimObject"/> to <paramref name="source"/> within the specified
    /// <paramref name="radius"/> that has the provided <paramref name="tag"/>.
    /// </summary>
    /// <param name="source">The source object from which the search originates. Its predicted position is used for distance checks.</param>
    /// <param name="radius">Search radius in world units. Objects outside this radius are ignored.</param>
    /// <param name="tag">The gameplay tag to filter objects by.</param>
    /// <param name="result">When this method returns, contains the nearest matching <see cref="SimObject"/> if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a matching object was found inside <paramref name="radius"/>; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This method queries <see cref="TagManager.GetObjectsInRangeWithTag"/> using the source's predicted position
    /// and then selects the nearest object by comparing predicted positions using <see cref="Vector3d.Distance"/>.
    /// </remarks>
    public static bool TryFindNearestWithTag(SimObject source, double radius, GameplayTagSO tag, out SimObject result)
    {
        SimObject nearestObject = null;
        double nearestDistance = double.MaxValue;
        foreach (var simObject in TagManager.I.GetObjectsInRangeWithTag(
                     source.GE_GetPredictedPosition(),
                     radius, tag))
        {
            var distance = Vector3d.Distance(
                source.GE_GetPredictedPosition(), simObject.GE_GetPredictedPosition());
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestObject = simObject;
            }
        }

        if (nearestObject)
        {
            result = nearestObject;
            return true;
        }
        result = null;
        return false;
    }

    /// <summary>
    /// Attempts to find the nearest <see cref="SimObject"/> to <paramref name="source"/> within the specified
    /// <paramref name="radius"/> that has any of the tags in the provided <paramref name="tags"/> set.
    /// </summary>
    /// <param name="source">The source object from which the search originates. Its predicted position is used for distance checks.</param>
    /// <param name="radius">Search radius in world units. Objects outside this radius are ignored.</param>
    /// <param name="tags">The set of gameplay tags to filter objects by.</param>
    /// <param name="result">When this method returns, contains the nearest matching <see cref="SimObject"/> if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a matching object was found inside <paramref name="radius"/>; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This method iterates over each tag in the <paramref name="tags"/> set and queries
    /// <see cref="TagManager.GetObjectsInRangeWithTag"/> using the source's predicted position for each tag.
    /// It then selects the nearest object by comparing predicted positions using <see cref="Vector3d.Distance"/>.
    /// </remarks>
    public static bool TryFindNearestWithTagAny(SimObject source, double radius, GameplayTagSet tags, out SimObject result)
    {
        // Handle null/empty tag set
        if (tags == null || tags.Count == 0)
        {
            result = null;
            return false;
        }

        SimObject nearestObject = null;
        double nearestDistance = double.MaxValue;

        // Iterate each tag in the provided set and query objects for that tag
        foreach (var tag in tags)
        {
            if (!tag) continue;

            foreach (var simObject in TagManager.I.GetObjectsInRangeWithTag(
                         source.GE_GetPredictedPosition(),
                         radius, tag))
            {
                var distance = Vector3d.Distance(
                    source.GE_GetPredictedPosition(), simObject.GE_GetPredictedPosition());
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestObject = simObject;
                }
            }
        }

        if (nearestObject)
        {
            result = nearestObject;
            return true;
        }

        result = null;
        return false;
    }

    public static IEnumerable<SimObject> GeAllObjectsInRange(Vector3d position, double radius)
    {
        return TagManager.I.GetAllObjectsInRange(position, radius);
    }
}
}