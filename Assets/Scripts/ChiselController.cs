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

        [SerializeField, Range(0, 200)] private int _maxImpactRange = 70;
        private int _impactRange;

        private DataChunk _voxelDataChunk;

        [SerializeField] private HapticSource _hapticSource;
        [SerializeField] private AudioSource _audioSource;

        private int _lowPolyLevel;
        private float _initialStoneScaleX;

        private void Awake()
        {
            _stoneTransform = _stone.transform;
            _initialStoneScaleX = _stoneTransform.localScale.x;
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

            UpdateLowPolyLevelByStoneScale();

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
            for (int y = minY; y <= maxY; y += _lowPolyLevel)
            {
                for (int x = minX; x <= maxX; x += _lowPolyLevel)
                {
                    for (int z = minZ; z <= maxZ; z += _lowPolyLevel)
                    {
                        _voxelDataChunk.GetWorldPosition(x, y, z, targetMatrix, out Vector3 cellWorldPos);
                        int hits = Physics.OverlapSphereNonAlloc(cellWorldPos, 0f, hitColliders);
                        if (Array.IndexOf(hitColliders, _collider, 0, hits) >= 0)
                        {
                            for (int yy = y - _lowPolyLevel / 2; yy <= y + _lowPolyLevel / 2; yy++)
                            {
                                for (int xx = x - _lowPolyLevel / 2; xx <= x + _lowPolyLevel / 2; xx++)
                                {
                                    for (int zz = z - _lowPolyLevel / 2; zz <= z + _lowPolyLevel / 2; zz++)
                                    {
                                        if (xx < 0 || xx >= _voxelDataChunk.xLength ||
                                            yy < 0 || yy >= _voxelDataChunk.yLength ||
                                            zz < 0 || zz >= _voxelDataChunk.zLength) continue;
                                        if (!_voxelDataChunk.HasFlag(xx, yy, zz, CellFlags.IsFilled)) continue;
                                        _voxelDataChunk.RemoveFlag(xx, yy, zz, CellFlags.IsFilled);
                                        removedCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (removedCount > 0)
            {
                PlayFeedback();
            }
        }

        private void UpdateLowPolyLevelByStoneScale()
        {
            if (_stoneTransform == null) return;

            float scale = _stoneTransform.localScale.x;
            float minScale = _initialStoneScaleX * 0.25f;
            float maxScale = _initialStoneScaleX;
            float clampedScale = Mathf.Clamp(scale, minScale, maxScale);

            if (scale >= _initialStoneScaleX)
            {
                _lowPolyLevel = 1;
            }
            else
            {
                float t = (clampedScale - minScale) / (maxScale - minScale);
                int newLowPolyLevel = Mathf.RoundToInt(Mathf.Lerp(6, 2, t));
                newLowPolyLevel = Mathf.Clamp(newLowPolyLevel, 2, 6);
                _lowPolyLevel = newLowPolyLevel;
            }
        }

        private void ExtractVoxel(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
        {
            // 衝撃中心のワールド座標を取得
            Vector3 impactCenterWorldPosition = _colliderTransform.position;
            // ワールド座標 → 石材のローカル座標へ変換
            Vector3 impactCenterLocalPosition = _stoneTransform.InverseTransformPoint(impactCenterWorldPosition);

            // colliderのローカルスケール（石材基準）を取得
            Vector3 colliderLocalScale = new(
                _colliderTransform.localScale.x / _stoneTransform.localScale.x,
                _colliderTransform.localScale.y / _stoneTransform.localScale.y,
                _colliderTransform.localScale.z / _stoneTransform.localScale.z
            );

            minX = Mathf.Max(0, Mathf.FloorToInt(impactCenterLocalPosition.x - colliderLocalScale.x));
            maxX = Mathf.Min(_voxelDataChunk.xLength - 1, Mathf.CeilToInt(impactCenterLocalPosition.x + colliderLocalScale.x));
            minY = Mathf.Max(0, Mathf.FloorToInt(impactCenterLocalPosition.y - colliderLocalScale.y));
            maxY = Mathf.Min(_voxelDataChunk.yLength - 1, Mathf.CeilToInt(impactCenterLocalPosition.y + colliderLocalScale.y));
            minZ = Mathf.Max(0, Mathf.FloorToInt(impactCenterLocalPosition.z - colliderLocalScale.z));
            maxZ = Mathf.Min(_voxelDataChunk.zLength - 1, Mathf.CeilToInt(impactCenterLocalPosition.z + colliderLocalScale.z));
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
