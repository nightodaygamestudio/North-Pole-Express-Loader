using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;

public class CopyScripts : EditorWindow
{
    // Default Path
    private string folderPath = "Assets/Scripts";

    // Toggle for subdirectories
    private bool includeSubdirectories = false;

    // FIXED FILENAME - No timestamps, always this name.
    private const string FIXED_FILENAME = "CopiedScripts.txt";

    [MenuItem("Tools/Copy Scripts Tool")]
    public static void Open()
    {
        var wnd = GetWindow<CopyScripts>("Script Copier");
        wnd.minSize = new Vector2(450, 180);
        wnd.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Instant Script Export", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // --- Path Selection ---
        GUILayout.Label("Source Folder (and Output Location):");
        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField(folderPath);
        if (GUILayout.Button("Browse...", GUILayout.Width(80)))
        {
            string abs = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(abs))
            {
                if (abs.Replace("\\", "/").StartsWith(Application.dataPath.Replace("\\", "/")))
                {
                    folderPath = "Assets" + abs.Replace("\\", "/").Substring(Application.dataPath.Replace("\\", "/").Length);
                }
                else
                {
                    ShowNotification(new GUIContent("Please select inside Assets!"));
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // --- Toggle ---
        GUILayout.Space(5);
        includeSubdirectories = EditorGUILayout.Toggle("Include Subdirectories", includeSubdirectories);

        // --- Info Box ---
        GUILayout.Space(15);
        string modeInfo = includeSubdirectories ? "Recursive (All subfolders)" : "Top-Level only";
        EditorGUILayout.HelpBox($"Target: {folderPath}/{FIXED_FILENAME}\nMode: {modeInfo}\n\nClicking below will OVERWRITE instantly.", MessageType.Info);

        GUILayout.Space(20);

        // --- The Button ---
        // Green color to signal "Action"
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button($"Update {FIXED_FILENAME}", GUILayout.Height(40)))
        {
            SaveDirectly();
        }
        GUI.backgroundColor = Color.white; // Reset color
    }

    void SaveDirectly()
    {
        // 1. Validate Path
        string absFolderPath = GetAbsolutePath(folderPath);
        if (!Directory.Exists(absFolderPath))
        {
            EditorUtility.DisplayDialog("Error", $"Folder does not exist:\n{absFolderPath}", "OK");
            return;
        }

        // 2. Find Files
        SearchOption searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.GetFiles(absFolderPath, "*.cs", searchOption);

        if (files.Length == 0)
        {
            ShowNotification(new GUIContent("No .cs scripts found!"));
            return;
        }

        // 3. Build String
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"// Auto-Generated Dump");
        sb.AppendLine($"// Source: {folderPath}");
        sb.AppendLine($"// Updated: {DateTime.Now}");
        sb.AppendLine();

        int count = 0;
        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);

            // Ignore this tool and the output file itself
            if (fileName == "CopyScripts.cs" || fileName == FIXED_FILENAME) continue;

            string content = File.ReadAllText(filePath);

            sb.AppendLine($"===== FILE: {fileName} =====");
            sb.AppendLine(content);
            sb.AppendLine($"===== END: {fileName} =====");
            sb.AppendLine();
            sb.AppendLine();

            count++;
        }

        if (count == 0)
        {
            ShowNotification(new GUIContent("No scripts to export."));
            return;
        }

        // 4. SAVE INSTANTLY (No Dialog)
        string fullSavePath = Path.Combine(absFolderPath, FIXED_FILENAME);

        try
        {
            // This overwrites existing files automatically
            File.WriteAllText(fullSavePath, sb.ToString());

            // Refresh Unity so the file appears immediately
            AssetDatabase.Refresh();

            // Highlight the file in the project view
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{folderPath}/{FIXED_FILENAME}");
            EditorGUIUtility.PingObject(obj);

            Debug.Log($"<color=green><b>EXPORT SUCCESS:</b></color> Updated {FIXED_FILENAME} with {count} scripts.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save: {e.Message}");
        }
    }

    private string GetAbsolutePath(string assetPath)
    {
        if (assetPath.StartsWith("Assets"))
        {
            assetPath = assetPath.Substring("Assets".Length);
        }
        assetPath = assetPath.TrimStart('/', '\\');
        return Path.Combine(Application.dataPath, assetPath);
    }
}