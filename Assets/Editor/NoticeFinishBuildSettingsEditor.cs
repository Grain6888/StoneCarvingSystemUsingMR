using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoticeFinishBuildSettings))]
public class NoticeFinishBuildSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("ビルド完了通知テスト"))
        {
            // ダミーの BuildTarget とパスを渡す
            NoticeFinishBuild.OnPostprocessBuild(BuildTarget.StandaloneWindows, "DummyPath");
        }
    }
}
