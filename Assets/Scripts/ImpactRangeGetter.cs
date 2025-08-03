using Oculus.Haptics;
using UnityEngine;

namespace MRSculpture
{
    public class ImpactRangeGetter : MonoBehaviour
    {
        [SerializeField] private GameObject _chisel;

        [SerializeField] private OVRInput.Controller _controllerWithHammer;

        private float _impactMagnitude = 0.0f;

        public float ImpactMagnitude => _impactMagnitude;
        public HapticSource hapticSource;
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
            if (other.gameObject.name != _chisel.name)
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
            // ハプティクスを再生
            hapticSource.Play(Controller.Right);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name != _chisel.name)
            {
                return;
            }
            _impactMagnitude = 0.0f;
        }
    }
}
