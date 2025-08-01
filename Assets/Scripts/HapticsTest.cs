using UnityEngine;

public class HapticsTest : MonoBehaviour
{
    private OVRHapticsClip _clip;

    [SerializeField] private AnimationCurve _ampEnv;

    private void Awake()
    {
        Debug.Log(OVRHaptics.Config.MinimumBufferSamplesCount); // 1
        Debug.Log(OVRHaptics.Config.MaximumBufferSamplesCount); // 256
        Debug.Log(OVRHaptics.Config.OptimalBufferSamplesCount); // 20
        Debug.Log(OVRHaptics.Config.SampleRateHz); // 320
        Debug.Log(OVRHaptics.Config.SampleSizeInBytes); // 1

        _clip = new OVRHapticsClip(160);

        // Random clip
        //new System.Random().NextBytes(_clip.Samples);

        for (int i = 0; i < _clip.Capacity; i++)
        {
            float val = 0.5f + 0.5f * Mathf.Sin((i / 160f) * 5f * Mathf.PI * 2f);
            val *= _ampEnv.Evaluate(i / 160f);
            _clip.WriteSample((byte)(val * 255f));
        }
    }

    public void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
        {
            OVRHaptics.LeftChannel.Preempt(_clip);
        }

        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            OVRHaptics.RightChannel.Preempt(_clip);
        }
    }
}
