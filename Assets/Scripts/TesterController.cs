using UnityEngine;

namespace MRSculpture
{
    public class TesterController : MonoBehaviour
    {
        [SerializeField] private GameObject _stone;
        private Transform _stoneTransform;
        private MainBehaviour _stoneController;

        [SerializeField] private Collider _collider;
        private Transform _colliderTransform;

        [SerializeField] private GameObject _hammer;
        private HammerController _hammerController;

        [SerializeField] private int _maxImpactRange = 70;
        private int _impactRange;

        private DataChunk _voxelDataChunk;

        private void Awake()
        {
            _stoneTransform = _stone.transform;
            _stoneController = _stone.GetComponent<MainBehaviour>();
            _colliderTransform = _collider.transform;
            _hammerController = _hammer.GetComponent<HammerController>();
        }

        public void AttachDataChunk(ref DataChunk voxelDataChunk)
        {
            _voxelDataChunk = voxelDataChunk;
        }

        private void Update()
        {
            _impactRange = Mathf.Min(_maxImpactRange, (int)(_hammerController.ImpactMagnitude * 15));
            _impactRange = 30;

            if (_impactRange > 0)
            {
                Carve();
                _stoneController.UpdateMesh();
            }
        }

        public void Carve()
        {
            float scaling = 0.002f * _impactRange;
            _colliderTransform.localScale = Vector3.one * scaling;
            Matrix4x4 targetMatrix = _stoneTransform.localToWorldMatrix;

            // ワールド座標を取得
            Vector3 impactCenterWorldPosition = _colliderTransform.position;
            // ワールド座標 → ターゲットのローカル座標へ変換
            Vector3 currentImpactCenterLocalPosition = _stoneTransform.InverseTransformPoint(impactCenterWorldPosition);
            // ローカル座標をボクセル単位に合わせる
            Vector3Int center = Vector3Int.RoundToInt(currentImpactCenterLocalPosition);

            // X方向の探索範囲（impactRange分だけ前後に拡張、範囲外はクランプ）
            int minX = Mathf.Max(0, center.x - _impactRange);
            int maxX = Mathf.Min(_voxelDataChunk.xLength - 1, center.x + _impactRange);
            // Y方向の探索範囲
            int minY = Mathf.Max(0, center.y - _impactRange);
            int maxY = Mathf.Min(_voxelDataChunk.yLength - 1, center.y + _impactRange);
            // Z方向の探索範囲
            int minZ = Mathf.Max(0, center.z - _impactRange);
            int maxZ = Mathf.Min(_voxelDataChunk.zLength - 1, center.z + _impactRange);

            Collider[] hitColliders = new Collider[10];
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        _voxelDataChunk.GetWorldPosition(x, y, z, targetMatrix, out Vector3 cellWorldPos);
                        if (System.Array.IndexOf(hitColliders, _collider, 0, Physics.OverlapSphereNonAlloc(cellWorldPos, 0f, hitColliders)) >= 0)
                        {
                            _voxelDataChunk.RemoveFlag(x, y, z, CellFlags.IsFilled);
                        }
                    }
                }
            }
        }
    }
}
