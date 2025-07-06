using UnityEditor;
using UnityEngine;

public class AssetEditorUtils
{
    public static void RenameAsset(ScriptableObject so, string newName)
    {
        string path = AssetDatabase.GetAssetPath(so);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Object is not an asset or not saved yet.");
            return;
        }

        string error = AssetDatabase.RenameAsset(path, newName);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"Rename failed: {error}");
        }
        else
        {
            Debug.Log($"Renamed asset to '{newName}'");
        }
    }
}
