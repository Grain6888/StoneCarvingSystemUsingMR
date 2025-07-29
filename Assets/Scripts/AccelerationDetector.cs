using UnityEngine;

namespace MRSculpture
{
    public class AccelerationDetector : MonoBehaviour
    {
        // 衝撃の強さを保持するフィールド
        private float _impactMagnitude = 0.0f;

        // 外部から取得できるプロパティ
        public float ImpactMagnitude => _impactMagnitude;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name != "Chisel")
            {
                _impactMagnitude = 0.0f;
                return;
            }
            // 衝突時のインパルス（力のベクトル）を取得
            Vector3 impulse = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            // インパルスの大きさ（衝撃の強さ）を計算
            _impactMagnitude = impulse.magnitude;
            if (_impactMagnitude <= 0.0f)
            {
                _impactMagnitude = 0.0f;
                return;
            }
            Debug.Log($"UNCHI Impact Magnitude: {_impactMagnitude}");
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
