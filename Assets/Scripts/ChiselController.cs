using Oculus.Haptics;
using UnityEngine;
using System;

namespace MRSculpture
{
    /// <summary>
    /// 彫刻用のノミ Chisel を制御するクラス
    /// </summary>
    public class ChiselController : MonoBehaviour
    {
        /// <summary>
        /// 彫刻対象の石材 GameObject
        /// </summary>
        [SerializeField] private GameObject _stone;

        /// <summary>
        /// Stone の Transform
        /// </summary>
        private Transform _stoneTransform;

        /// <summary>
        /// 石材コントローラ
        /// </summary>
        private StoneController _stoneController;

        /// <summary>
        /// 衝撃判定用 Collider
        /// </summary>
        [SerializeField] private Collider _collider;

        /// <summary>
        /// Collider の Transform
        /// </summary>
        private Transform _colliderTransform;

        /// <summary>
        /// ハンマー GameObject
        /// </summary>
        [SerializeField] private GameObject _hammer;

        /// <summary>
        /// ハンマーコントローラ
        /// </summary>
        private HammerController _hammerController;

        /// <summary>
        /// 現在の衝撃範囲
        /// </summary>
        private int _impactRange;

        /// <summary>
        /// ImpactRange の最大値
        /// </summary>
        [SerializeField, Range(0, 200)] private int _maxImpactRange = 70;

        /// <summary>
        /// ボクセル DataChunk
        /// </summary>
        private DataChunk _voxelDataChunk;

        /// <summary>
        /// 触覚フィードバック用 HapticSource
        /// </summary>
        [SerializeField] private HapticSource _hapticSource;

        /// <summary>
        /// 音響フィードバック用 AudioSource
        /// </summary>
        [SerializeField] private AudioSource _audioSource;

        /// <summary>
        /// ボクセルの削除解像度 (1で等倍)
        /// </summary>
        private int _lowPolyLevel;

        /// <summary>
        /// 石材の初期スケール (X軸)
        /// </summary>
        private float _initialStoneScaleX;

        private void Awake()
        {
            _stoneTransform = _stone.transform;
            _initialStoneScaleX = _stoneTransform.localScale.x;
            _stoneController = _stone.GetComponent<StoneController>();
            _colliderTransform = _collider.transform;
            _hammerController = _hammer.GetComponent<HammerController>();
        }

        /// <summary>
        /// ボクセル DataChunk をアタッチする
        /// </summary>
        /// <param name="voxelDataChunk">ボクセル DataChunk</param>
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

        /// <summary>
        /// Collider 内のボクセルを削除する
        /// </summary>
        public void Carve()
        {
            float scaling = _initialStoneScaleX * _impactRange;
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

        /// <summary>
        /// 石材の縮小率に応じて LowPolyLevel を更新する
        /// </summary>
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

        /// <summary>
        /// Collider の中心と大きさから処理対象の範囲を算出する
        /// </summary>
        /// <param name="minX">処理範囲の最小X座標</param>
        /// <param name="maxX">処理範囲の最大X座標</param>
        /// <param name="minY">処理範囲の最小Y座標</param>
        /// <param name="maxY">処理範囲の最大Y座標</param>
        /// <param name="minZ">処理範囲の最小Z座標</param>
        /// <param name="maxZ">処理範囲の最大Z座標</param>
        private void ExtractVoxel(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
        {
            Vector3 impactCenterWorldPosition = _colliderTransform.position;
            Vector3 impactCenterLocalPosition = _stoneTransform.InverseTransformPoint(impactCenterWorldPosition);

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

        /// <summary>
        /// フィードバックを再生する
        /// </summary>
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
