using Oculus.Haptics;
using UnityEngine;
using System;

namespace MRSculpture
{
    public class ChiselController : MonoBehaviour
    {
        [SerializeField] private GameObject _stone;
        private Transform _stoneTransform;
        private StoneController _stoneController;

        [SerializeField] private Collider _collider;
        private Transform _colliderTransform;

        [SerializeField] private GameObject _hammer;
        private HammerController _hammerController;

        [SerializeField] private int _maxImpactRange = 70;
        private int _impactRange;

        private DataChunk _voxelDataChunk;

        [SerializeField] private HapticSource _hapticSource;
        [SerializeField] private AudioSource _audioSource;

        private void Awake()
        {
            _stoneTransform = _stone.transform;
            _stoneController = _stone.GetComponent<StoneController>();
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

            ExtractVoxel(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);

            Matrix4x4 targetMatrix = _stoneTransform.localToWorldMatrix;
            Collider[] hitColliders = new Collider[10];
            int removedCount = 0;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        _voxelDataChunk.GetWorldPosition(x, y, z, targetMatrix, out Vector3 cellWorldPos);
                        int hits = Physics.OverlapSphereNonAlloc(cellWorldPos, 0f, hitColliders);
                        if (Array.IndexOf(hitColliders, _collider, 0, hits) >= 0 && _voxelDataChunk.HasFlag(x, y, z, CellFlags.IsFilled))
                        {
                            _voxelDataChunk.RemoveFlag(x, y, z, CellFlags.IsFilled);
                            removedCount++;
                        }
                    }
                }
            }

            if (removedCount > 0)
            {
                PlayFeedback();
            }
        }

        private void ExtractVoxel(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
        {
            // 衝撃中心のワールド座標を取得
            Vector3 impactCenterWorldPosition = _colliderTransform.position;
            // ワールド座標 → 石材のローカル座標へ変換
            Vector3 currentImpactCenterLocalPosition = _stoneTransform.InverseTransformPoint(impactCenterWorldPosition);
            // ローカル座標をボクセル単位に変換
            Vector3Int center = Vector3Int.RoundToInt(currentImpactCenterLocalPosition);

            // 各軸の探索範囲を計算（範囲外はクランプ）
            minX = Mathf.Max(0, center.x - _impactRange);
            maxX = Mathf.Min(_voxelDataChunk.xLength - 1, center.x + _impactRange);
            minY = Mathf.Max(0, center.y - _impactRange);
            maxY = Mathf.Min(_voxelDataChunk.yLength - 1, center.y + _impactRange);
            minZ = Mathf.Max(0, center.z - _impactRange);
            maxZ = Mathf.Min(_voxelDataChunk.zLength - 1, center.z + _impactRange);
        }

        private void PlayFeedback()
        {
            float amplitude = Mathf.Clamp01(_impactRange / 10f);

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
    }
}
