#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class AssetSizeExplorer : EditorWindow
{
    private Vector2 _scroll;
    private int _maxResults = 20;
    private (string path, long size)[] _results;

    [MenuItem("Tools/Asset Size Explorer")]
    private static void ShowWindow()
    {
        GetWindow<AssetSizeExplorer>("Asset Size Explorer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Find Largest Assets", EditorStyles.boldLabel);

        // Let the user choose how many top files to show
        _maxResults = EditorGUILayout.IntField("Max Results", _maxResults);

        if (GUILayout.Button("Scan Assets"))
        {
            ScanAssets();
        }

        if (_results != null && _results.Length > 0)
        {
            GUILayout.Space(8);
            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (var (path, size) in _results)
            {
                // Format size in MB with two decimals
                string sizeMB = (size / (1024f * 1024f)).ToString("F2") + " MB";
                EditorGUILayout.LabelField(path, sizeMB);
            }

            GUILayout.EndScrollView();
        }
    }

    private void ScanAssets()
    {
        // Get all asset paths under "Assets/"
        var paths = AssetDatabase
            .GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/"))
            .ToArray();

        // Map to (path,size), sort descending, take top N
        _results = paths
            .Select(p =>
            {
                var full = Path.GetFullPath(p);
                var info = new FileInfo(full);
                return (path: p, size: info.Exists ? info.Length : 0L);
            })
            .OrderByDescending(t => t.size)
            .Take(_maxResults)
            .ToArray();
    }
}

#endif
