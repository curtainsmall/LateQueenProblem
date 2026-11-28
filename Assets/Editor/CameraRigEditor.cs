using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>CameraRig 的检视面板：用按钮在编辑期预览三种视图。</summary>
[CustomEditor(typeof(CameraRig))]
public sealed class CameraRigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var rig = (CameraRig)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("视图预览", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Away")) Apply(rig, CameraRig.ViewMode.Away);
            if (GUILayout.Button("Focus")) Apply(rig, CameraRig.ViewMode.Focus);
            if (GUILayout.Button("Top")) Apply(rig, CameraRig.ViewMode.Top);
        }

        EditorGUILayout.HelpBox("当前模式：" + rig.Mode, MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("以当前视角覆盖预设", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存为 Away")) Capture(rig, CameraRig.ViewMode.Away);
            if (GUILayout.Button("保存为 Focus")) Capture(rig, CameraRig.ViewMode.Focus);
            if (GUILayout.Button("保存为 Top")) Capture(rig, CameraRig.ViewMode.Top);
        }
    }

    static void Capture(CameraRig rig, CameraRig.ViewMode mode)
    {
        Undo.RecordObject(rig, "Capture View Preset");
        rig.CapturePreset(mode);
        EditorUtility.SetDirty(rig);
    }

    static void Apply(CameraRig rig, CameraRig.ViewMode mode)
    {
        var camera = rig.GetComponentInChildren<Camera>();

        Undo.RecordObject(rig.transform, "View Mode");
        if (camera != null) Undo.RecordObject(camera.transform, "View Mode");

        rig.Show(mode);

        if (camera != null && !Application.isPlaying) Undo.RecordObject(camera, "View Mode");

        EditorUtility.SetDirty(rig);
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
    }
}
