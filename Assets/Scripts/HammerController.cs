using Oculus.Haptics;
using UnityEngine;

namespace MRSculpture
{
    public class HammerController : MonoBehaviour
    {
        [SerializeField] private OVRInput.Controller _controllerWithHammer;
        [SerializeField] private AudioSource _audioSource;

        private float _impactMagnitude = 0.0f;

        public float ImpactMagnitude => _impactMagnitude;
        [SerializeField] private HapticSource _hapticSource;
        private int _frameCount = 0;

        void Update()
        {
            if (_impactMagnitude <= 0.0f)
            {
                return;
            }

            if (_frameCount < 5)
            {
                _frameCount++;
            }
            else
            {
                _impactMagnitude = 0.0f;
                _frameCount = 0;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name != "Chisel")
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
                _hapticSource.Play(Controller.Right);
            }

            if (_audioSource != null)
            {
                _audioSource.volume = amplitude;
                _audioSource.Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name != "Chisel")
            {
                return;
            }
            _impactMagnitude = 0.0f;
        }
    }
}
