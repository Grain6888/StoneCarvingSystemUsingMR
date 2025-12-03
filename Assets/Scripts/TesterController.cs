using UnityEngine;

namespace MRSculpture
{
    public class TesterController : MonoBehaviour
    {
        private int _impactRange;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private Transform _colliderObject;
        private Collider _collider;

        private void Awake()
        {
            _collider = _colliderObject.GetComponent<Collider>();
        }


        public void Carve(ref DataChunk voxelDataChunk, in int impactRange)
        {
            _impactRange = 30;
            float scaling = 0.002f * _impactRange;
            _colliderObject.localScale = Vector3.one * scaling;
            Matrix4x4 targetMatrix = _targetTransform.localToWorldMatrix;

            // ワールド座標を取得
            Vector3 impactCenterWorldPosition = _colliderObject.position;
            // ワールド座標 → ターゲットのローカル座標へ変換
            Vector3 currentImpactCenterLocalPosition = _targetTransform.InverseTransformPoint(impactCenterWorldPosition);
            // ローカル座標をボクセル単位に合わせる
            Vector3Int center = Vector3Int.RoundToInt(currentImpactCenterLocalPosition);

            // X方向の探索範囲（impactRange分だけ前後に拡張、範囲外はクランプ）
            int minX = Mathf.Max(0, center.x - _impactRange);
            int maxX = Mathf.Min(voxelDataChunk.xLength - 1, center.x + _impactRange);
            // Y方向の探索範囲
            int minY = Mathf.Max(0, center.y - _impactRange);
            int maxY = Mathf.Min(voxelDataChunk.yLength - 1, center.y + _impactRange);
            // Z方向の探索範囲
            int minZ = Mathf.Max(0, center.z - _impactRange);
            int maxZ = Mathf.Min(voxelDataChunk.zLength - 1, center.z + _impactRange);

            Collider[] hitColliders = new Collider[10];
            // 各XZレイヤごとに処理
            for (int y = minY; y <= maxY; y++)
            {
                // Y層のXZ平面のDataChunkを取得
                DataChunk xzLayer = voxelDataChunk.GetXZLayer(y);
                for (int x = minX; x <= maxX; x++)
                {
                    // Z方向の範囲をループ
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        xzLayer.GetWorldPosition(x, y, z, targetMatrix, out Vector3 cellWorldPos);
                        int colliderCount = Physics.OverlapSphereNonAlloc(cellWorldPos, 0f, hitColliders);
                        for (int i = 0; i < colliderCount; i++)
                        {
                            if (hitColliders[i] == _collider)
                            {
                                xzLayer.RemoveFlag(x, 0, z, CellFlags.IsFilled);
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }
}
