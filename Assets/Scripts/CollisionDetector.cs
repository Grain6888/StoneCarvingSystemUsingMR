using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
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
        // 衝突した相手のGameObject名をデバッグログに出力
        Debug.Log("UNCHI Collision Detected: " + collision.gameObject.name);

        // 衝突時のインパルス（力のベクトル）を取得
        Vector3 impulse = collision.impulse;

        // インパルスの大きさ（衝撃の強さ）を計算
        float impactMagnitude = impulse.magnitude;

        // 衝撃の強さをデバッグログに出力
        Debug.Log("UNCHI Impact Magnitude: " + impactMagnitude);

        // 衝突した全ての接触点について処理
        foreach (ContactPoint contact in collision.contacts)
        {
            // 接触点の座標を取得
            Vector3 contactPoint = contact.point;
            // 接触点の法線（面の向き）を取得
            Vector3 contactNormal = contact.normal;

            // 接触点の座標をデバッグログに出力
            Debug.Log("UNCHI Contact Point: " + contactPoint);
            // 接触点の法線をデバッグログに出力
            Debug.Log("UNCHI Contact Normal: " + contactNormal);
        }
    }
}
