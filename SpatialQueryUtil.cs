using UnityEngine;

namespace GameplayTags
{
public static class SpatialQueryUtil
{
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
}
}