using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>工作区的保存、加载与关卡资产导出。</summary>
public static class DesignerIO
{
    public const string DefaultWorkspacePath = "DesignerData/workspace.json";
    public const string DefaultExportPath = "Assets/Levels/LevelSet.asset";

    public static string ProjectRoot => Directory.GetCurrentDirectory();

    public static string Resolve(string path)
    {
        if (string.IsNullOrEmpty(path)) return Path.Combine(ProjectRoot, DefaultWorkspacePath);
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(ProjectRoot, path);
    }

    public static void Save(DesignerWorkspace workspace, string path)
    {
        string full = Resolve(path);
        string directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        File.WriteAllText(full, JsonUtility.ToJson(workspace, true));
    }

    public static DesignerWorkspace Load(string path, out string error)
    {
        error = string.Empty;
        string full = Resolve(path);

        if (!File.Exists(full))
        {
            error = "文件不存在：" + full;
            return null;
        }

        try
        {
            DesignerWorkspace workspace = JsonUtility.FromJson<DesignerWorkspace>(File.ReadAllText(full));
            if (workspace == null)
            {
                error = "解析失败";
                return null;
            }

            workspace.situations ??= new List<DesignerSituation>();
            workspace.levelOrder ??= new List<string>();

            for (int i = 0; i < workspace.situations.Count; i++) workspace.situations[i]?.EnsureShape();

            return workspace;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return null;
        }
    }

    /// <summary>把关卡表导出成 Assets 下的 LevelSet 资产（覆盖写入）。</summary>
    public static bool ExportAssets(DesignerWorkspace workspace, string assetPath, out string error)
    {
        error = string.Empty;

        if (workspace == null)
        {
            error = "工作区为空";
            return false;
        }

        if (string.IsNullOrEmpty(assetPath)) assetPath = DefaultExportPath;
        if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            error = "导出路径必须以 Assets/ 开头";
            return false;
        }

        EnsureFolder(assetPath);

        LevelSet levelSet = AssetDatabase.LoadAssetAtPath<LevelSet>(assetPath);

        if (levelSet == null)
        {
            levelSet = ScriptableObject.CreateInstance<LevelSet>();
            AssetDatabase.CreateAsset(levelSet, assetPath);
        }

        levelSet.SetLevels(BuildLevels(workspace));

        EditorUtility.SetDirty(levelSet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    static void EnsureFolder(string assetPath)
    {
        string directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(directory) || AssetDatabase.IsValidFolder(directory)) return;

        string[] parts = directory.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    public static List<LevelEntry> BuildLevels(DesignerWorkspace workspace)
    {
        var levels = new List<LevelEntry>();

        for (int i = 0; i < workspace.levelOrder.Count; i++)
        {
            DesignerSituation situation = workspace.FindSituation(workspace.levelOrder[i]);
            if (situation == null) continue;

            situation.EnsureShape();
            situation.RebuildEdges();

            var entry = new LevelEntry
            {
                id = situation.id,
                rows = situation.rows,
                cols = situation.cols,
                flipAllowed = situation.rules != null && situation.rules.flipAllowed,
                requiredTopologies = situation.rules == null ? 1 : Mathf.Max(1, situation.rules.requiredTopologies)
            };

            entry.rowDirections.AddRange(situation.rowDirections);
            entry.colDirections.AddRange(situation.colDirections);
            entry.rowLocked.AddRange(situation.rowLocked);
            entry.colLocked.AddRange(situation.colLocked);

            for (int c = 0; c < situation.cards.Count; c++)
            {
                DesignerCard card = situation.cards[c];

                var levelCard = new LevelCard
                {
                    id = card.id,
                    text = card.text,
                    row = card.row,
                    col = card.col,
                    isFixed = card.isFixed,
                    follow = new List<string>(situation.FollowOf(card.id)),
                    lead = new List<string>(situation.LeadOf(card.id))
                };

                entry.cards.Add(levelCard);
            }

            levels.Add(entry);
        }

        return levels;
    }
}
