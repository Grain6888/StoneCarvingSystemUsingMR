using UnityEditor;
using UnityEditor.Callbacks;
using System.Media;

public class NoticeFinishBuild
{
    private static NoticeFinishBuildSettings LoadSettings()
    {
        return AssetDatabase.LoadAssetAtPath<NoticeFinishBuildSettings>("Assets/Editor/NoticeFinishBuildSettings.asset");
    }

    [MenuItem("THE IDOLM@STER/諸星きらり", true)]
    private static bool ToggleNotificationValidate()
    {
        NoticeFinishBuildSettings settings = LoadSettings();
        Menu.SetChecked("THE IDOLM@STER/諸星きらり", settings != null && settings.enableNotification);
        return true;
    }

    [MenuItem("THE IDOLM@STER/諸星きらり")]
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
        EditorUtility.DisplayDialog(dialogTitle, dialogMessage, dialogOk);
    }

    private static void PlayCustomSound(string soundPath)
    {
        if (System.IO.File.Exists(soundPath))
        {
            using (SoundPlayer player = new(soundPath))
            {
                player.Load();
                player.Play();
            }
        }
        else
        {
            EditorApplication.Beep();
        }
    }
}
