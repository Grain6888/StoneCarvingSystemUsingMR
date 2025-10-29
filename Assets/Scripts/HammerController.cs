using Oculus.Haptics;
using UnityEngine;

namespace MRSculpture
{
    public class HammerController : MonoBehaviour
    {
        [SerializeField] private OVRInput.Controller _controllerWithHammer;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private GameObject _LControllerVisual;
        [SerializeField] private GameObject _RControllerVisual;
        private SkinnedMeshRenderer _controller;

        private float _impactMagnitude = 0.0f;

        public float ImpactMagnitude => _impactMagnitude;
        [SerializeField] private HapticSource _hapticSource;

        private bool _isPressingHandTrigger = false;

        private void Awake()
        {
            if (_controllerWithHammer == OVRInput.Controller.LTouch)
            {
                _controller = _LControllerVisual.GetComponent<SkinnedMeshRenderer>();
                Debug.Log("MRSculpture : HammerController: Left controller assigned.");
            }
            else if (_controllerWithHammer == OVRInput.Controller.RTouch)
            {
                _controller = _RControllerVisual.GetComponent<SkinnedMeshRenderer>();
                Debug.Log("MRSculpture : HammerController: Right controller assigned.");
            }
            else
            {
                Debug.LogError("MRSculpture : HammerController: Invalid controller assignment.");
            }
        }

        private void Update()
        {
            if (_impactMagnitude <= 0.0f) return;
            if (_impactMagnitude > 0.0f) _impactMagnitude = 0.0f;
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger)) DownHandTrigger();
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger)) UpHandTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("HandTool"))
            {
                _impactMagnitude = 0.0f;
                return;
            }
            // 衝突時のインパルス（力のベクトル）を取得
            Vector3 impulse = OVRInput.GetLocalControllerVelocity(_controllerWithHammer);
            // インパルスの大きさ（衝撃の強さ）を計算
            _impactMagnitude = impulse.magnitude;
            if (_impactMagnitude <= 0.0f)
            {
                _impactMagnitude = 0.0f;
                return;
            }

            PlayFeedback();
        }

        private void PlayFeedback()
        {
            float amplitude = Mathf.Clamp01(_impactMagnitude * 0.5f);

            if (_hapticSource != null)
            {
                _hapticSource.amplitude = amplitude;
                _hapticSource.Play();
            }

            if (_audioSource != null)
            {
                _audioSource.volume = amplitude;
                _audioSource.Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.gameObject.CompareTag("HandTool"))
            {
                return;
            }
            _impactMagnitude = 0.0f;
        }

        private void DownHandTrigger()
        {
            Debug.Log("MRSculpture : HammerController: Hand trigger pressed.");
            _controller.enabled = false;
            _isPressingHandTrigger = true;
        }

        private void UpHandTrigger()
        {
            Debug.Log("MRSculpture : HammerController: Hand trigger released.");
            _controller.enabled = true;
            _isPressingHandTrigger = false;
        }
    }
}
