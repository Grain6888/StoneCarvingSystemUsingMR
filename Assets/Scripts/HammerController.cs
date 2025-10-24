using Oculus.Haptics;
using Oculus.Interaction.Input;
using UnityEngine;

namespace MRSculpture
{
    public class HammerController : MonoBehaviour
    {
        [SerializeField] private OVRInput.Controller _controllerWithHammer;
        [SerializeField] private AudioSource _audioSource;
        private GameObject _questController;

        private float _impactMagnitude = 0.0f;

        public float ImpactMagnitude => _impactMagnitude;
        [SerializeField] private HapticSource _hapticSource;

        private bool _isPressingGrip = false;

        private void Awake()
        {
            if (_controllerWithHammer == OVRInput.Controller.LTouch)
            {
                _questController = GameObject.Find("OVRLeftControllerVisual");
            }
            else if (_controllerWithHammer == OVRInput.Controller.RTouch)
            {
                _questController = GameObject.Find("OVRRightControllerVisual");
            }
        }

        private void Update()
        {
            if (_impactMagnitude <= 0.0f) return;
            if (_impactMagnitude > 0.0f) _impactMagnitude = 0.0f;
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger)) DownTriggerButton();
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger)) UpTriggerButton();
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

        private void DownTriggerButton()
        {
            _isPressingGrip = true;
        }

        private void UpTriggerButton()
        {
            _isPressingGrip = false;
        }
    }
}
