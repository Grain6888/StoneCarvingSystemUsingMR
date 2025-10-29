using UnityEditor;
using UnityEditor.Callbacks;
using System.Media;

public class NoticeFinishBuild
{
    private static SoundPlayer _player; // 再生中のSoundPlayerを保持

    private static NoticeFinishBuildSettings LoadSettings()
    {
        return AssetDatabase.LoadAssetAtPath<NoticeFinishBuildSettings>("Assets/Editor/NoticeFinishBuildSettings.asset");
    }

    [MenuItem("Notice/Build Complete", true)]
    private static bool ToggleNotificationValidate()
    {
        NoticeFinishBuildSettings settings = LoadSettings();
        Menu.SetChecked("Notice/Build Complete", settings != null && settings.enableNotification);
        return true;
    }

    [MenuItem("Notice/Build Complete")]
    private static void ToggleNotification()
    {
        NoticeFinishBuildSettings settings = LoadSettings();
        if (settings != null)
        {
            settings.enableNotification = !settings.enableNotification;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }

    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        NoticeFinishBuildSettings settings = LoadSettings();
        if (settings != null && !settings.enableNotification)
            return;

        string soundPath = AssetDatabase.GetAssetPath(settings.soundClip);
        PlayCustomSound(soundPath);

        string dialogTitle = settings.dialogTitle;
        string dialogMessage = settings.dialogMessage;
        string dialogOk = settings.dialogOk;
        bool choice = EditorUtility.DisplayDialog(dialogTitle, dialogMessage, dialogOk);
        if (choice) StopCustomSound();
    }


    private static void PlayCustomSound(string soundPath)
    {
        StopCustomSound(); // 既存の再生を停止

        if (System.IO.File.Exists(soundPath))
        {
            _player = new SoundPlayer(soundPath);
            _player.Load();
            _player.Play();
        }
        else
        {
            EditorApplication.Beep();
        }
    }

    private static void StopCustomSound()
    {
        if (_player != null)
        {
            _player.Stop();
            _player.Dispose();
            _player = null;
        }
    }
}
