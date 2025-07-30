using UnityEngine;

namespace MRSculpture
{
    public class CollisionDetector : MonoBehaviour
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

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.name != "Hammer")
            {
                _impactMagnitude = 0.0f;
                return;
            }

            // 衝突時のインパルス（力のベクトル）を取得
            Vector3 impulse = collision.impulse;
            // インパルスの大きさ（衝撃の強さ）を計算
            _impactMagnitude = impulse.magnitude;

            if (_impactMagnitude <= 0.0f)
            {
                _impactMagnitude = 0.0f;
                return;
            }

            // 衝突した全ての接触点について処理
            foreach (ContactPoint contact in collision.contacts)
            {
                // 接触点の座標を取得
                Vector3 contactPoint = contact.point;
                // 接触点の法線（面の向き）を取得
                Vector3 contactNormal = contact.normal;
            }
        }

        void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.name != "Hammer")
            {
                return;
            }
            // 衝突が終了したときに衝撃の強さをリセット
            _impactMagnitude = 0.0f;
        }
    }
}
