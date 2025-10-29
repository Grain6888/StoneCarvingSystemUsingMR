using UnityEngine;

[CreateAssetMenu(fileName = "NoticeFinishBuildSettings", menuName = "Settings/NoticeFinishBuildSettings")]
public class NoticeFinishBuildSettings : ScriptableObject
{
    public AudioClip soundClip;
    public string dialogTitle;
    [TextArea(1, 100)] public string dialogMessage;
    public string dialogOk;
    public bool enableNotification = true;
}
