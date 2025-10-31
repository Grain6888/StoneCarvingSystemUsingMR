using UnityEngine;

public class CaptionController : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraRig;

    [SerializeField]
    private Canvas _captionCanvas;

    private void Awake()
    {
        _captionCanvas = GetComponent<Canvas>();
    }

    private void Update()
    {
        transform.LookAt(_cameraRig, Vector3.up);
    }

    public void HideCaption()
    {
        _captionCanvas.enabled = false;
    }

    public void ShowCaption()
    {
        _captionCanvas.enabled = true;
    }
}
