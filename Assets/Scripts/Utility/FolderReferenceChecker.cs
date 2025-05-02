using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

// Place this script in an Editor folder (e.g. Assets/Editor/FolderReferenceChecker.cs)
public class FolderReferenceChecker : EditorWindow
{
    private DefaultAsset folderAsset;
    private string folderPath;
    private List<string> referencingAssets = new List<string>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Folder Reference Checker")]
    public static void ShowWindow()
    {
        GetWindow<FolderReferenceChecker>("Folder Reference Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Check Folder References", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Select the folder to check
        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folderAsset, typeof(DefaultAsset), false);
        if (folderAsset != null)
        {
            folderPath = AssetDatabase.GetAssetPath(folderAsset);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorGUILayout.HelpBox("Please select a valid folder.", MessageType.Error);
                return;
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Check References"))
        {
            if (folderAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder to check.", "OK");
            }
            else
            {
                ScanForReferences();
            }
        }

        EditorGUILayout.Space();
        if (referencingAssets != null)
        {
            if (referencingAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("No references found. Safe to remove.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("References detected in these assets:", MessageType.Warning);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                foreach (var path in referencingAssets)
                {
                    EditorGUILayout.LabelField(path);
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void ScanForReferences()
    {
        referencingAssets.Clear();

        // Gather all files under the target folder
        var targetGUIDs = AssetDatabase.FindAssets("", new[] { folderPath });
        var targetPaths = targetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToHashSet();

        // All other assets in the project
        var allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && !p.StartsWith(folderPath + "/"))
            .ToArray();

        EditorUtility.DisplayProgressBar("Folder Reference Checker", "Scanning assets...", 0f);

        for (int i = 0; i < allAssets.Length; i++)
        {
            var asset = allAssets[i];
            EditorUtility.DisplayProgressBar("Folder Reference Checker", asset, (float)i / allAssets.Length);

            var deps = AssetDatabase.GetDependencies(asset, true);
            if (deps.Any(dep => targetPaths.Contains(dep)))
            {
                referencingAssets.Add(asset);
            }
        }

        EditorUtility.ClearProgressBar();
    }
}