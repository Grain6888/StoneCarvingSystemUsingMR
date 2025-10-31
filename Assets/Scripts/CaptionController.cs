using UnityEngine;

public class CaptionController : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraRig;

    private void Update()
    {
        transform.LookAt(_cameraRig, Vector3.up);
    }
}
