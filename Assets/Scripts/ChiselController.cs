using Oculus.Haptics;
using System;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace MRSculpture
{
    /// <summary>
    /// 彫刻用のノミ Chisel を制御するクラス
    /// </summary>
    public class ChiselController : MonoBehaviour
    {
        private const string DUMMY_CHISEL_NAME = "DUMMY_CHISEL";

        /// <summary>
        /// 石材
        /// </summary>
        [SerializeField]
        [Header("石材")]
        private GameObject _stone;

        /// <summary>
        /// 石材の Transform
        /// </summary>
        private Transform _stoneTransform;

        /// <summary>
        /// 石材コントローラ
        /// </summary>
        private StoneController _stoneController;

        /// <summary>
        /// 衝撃判定用 Collider
        /// </summary>
        [SerializeField]
        [Header("衝撃判定用 Collider")]
        private Collider _collider;

        /// <summary>
        /// Collider の Transform
        /// </summary>
        private Transform _colliderTransform;

        /// <summary>
        /// ハンマー
        /// </summary>
        [SerializeField]
        [Header("ハンマー")]
        private GameObject _hammer;

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
        [SerializeField]
        [Range(0, 200)]
        [Header("Impact Range の最大値")]
        private int _maxImpactRange = 70;

        /// <summary>
        /// ハンマーの衝撃感度
        /// </summary>
        [SerializeField]
        [Range(0, 50)]
        [Header("ハンマーの衝撃感度")]
        [Tooltip("0 を指定すると Update 毎に Carve が実行されます")]
        private int _sensitivity = 20;

        /// <summary>
        /// ボクセル DataChunk
        /// </summary>
        private DataChunk _voxelDataChunk;

        /// <summary>
        /// 触覚フィードバック用 HapticSource
        /// </summary>
        [SerializeField]
        [Header("触覚フィードバック用 Haptic Source")]
        private HapticSource _hapticSource;

        /// <summary>
        /// 音響フィードバック用 AudioSource
        /// </summary>
        [SerializeField]
        [Header("音響フィードバック用 Audio Source")]
        private AudioSource _audioSource;

        /// <summary>
        /// 視覚フィードバック用 ParticleSystem
        /// </summary>
        [SerializeField]
        [Header("視覚フィードバック用 Particle System")]
        private ParticleSystem _particleSystem;

        /// <summary>
        /// パーティクルのインスタンス
        /// </summary>
        private ParticleSystem _carvedParticle;

        /// <summary>
        /// ボクセルの削除解像度 (1で等倍)
        /// </summary>
        private int _lowPolyLevel;

        /// <summary>
        /// 石材の初期スケール (X軸)
        /// </summary>
        private float _initialStoneScaleX;

        private GameObject _dummyChisel;
        private bool _isAimed = false;

        private void Awake()
        {
            _stoneTransform = _stone.transform;
            _initialStoneScaleX = _stoneTransform.localScale.x;
            _stoneController = _stone.GetComponent<StoneController>();
            _colliderTransform = _collider.transform;
            _hammerController = _hammer.GetComponent<HammerController>();

            _carvedParticle = Instantiate(_particleSystem);
        }

        /// <summary>
        /// ボクセル DataChunk をアタッチする
        /// </summary>
        /// <param name="voxelDataChunk">ボクセル DataChunk</param>
        public void AttachDataChunk(ref DataChunk voxelDataChunk)
        {
            _voxelDataChunk = voxelDataChunk;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"MRSculpture : DataChunk attached to {name}.");
#endif
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) && !_isAimed)
            {
                SetAim();
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) && _isAimed)
            {
                UnsetAim();
            }

            if (_sensitivity > 0)
            {
                _impactRange = Mathf.Min(_maxImpactRange, (int)(_hammerController.ImpactMagnitude * _sensitivity));
            }
            else
            {
                _impactRange = 0;
            }

            UpdateLowPolyLevelByStoneScale();

            if (_impactRange > 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_sensitivity > 0)
                {
                    Debug.Log($"MRSculpture : {name} cought ImpactRange = {_impactRange}");
                }
#endif
                Carve();
                _stoneController.UpdateMesh();
            }

            if (_sensitivity == 0)
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
            var diffs = new System.Collections.Generic.List<(int index, uint before, uint after)>();

            if (_sensitivity > 0)
            {
                float scaling = _initialStoneScaleX * _impactRange / transform.localScale.x;
                _colliderTransform.localScale = Vector3.one * scaling;
            }
            ExtractVoxel(out Vector3Int min, out Vector3Int max);
            Matrix4x4 targetMatrix = _stoneTransform.localToWorldMatrix;
            int removedCount = 0;
            for (int y = min.y; y <= max.y; y += _lowPolyLevel)
            {
                for (int x = min.x; x <= max.x; x += _lowPolyLevel)
                {
                    for (int z = min.z; z <= max.z; z += _lowPolyLevel)
                    {
                        _voxelDataChunk.GetWorldPosition(x, y, z, targetMatrix, out Vector3 cellWorldPos);
                        if (_collider.ClosestPoint(cellWorldPos) == cellWorldPos)
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
                                        int idx = _voxelDataChunk.GetIndex(xx, yy, zz);
                                        var arr = _voxelDataChunk.DataArray;
                                        uint before = arr[idx].status;
                                        if (!_voxelDataChunk.HasFlag(idx, CellFlags.IsFilled)) continue;
                                        _voxelDataChunk.RemoveFlag(xx, yy, zz, CellFlags.IsFilled);
                                        uint after = arr[idx].status;
                                        diffs.Add((idx, before, after));
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
                _stoneController.SetCarveDiffs(diffs);
                PlayFeedback(removedCount);
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
        /// <param name="min">処理範囲の最小座標</param>
        /// <param name="max">処理範囲の最大座標</param>
        private void ExtractVoxel(out Vector3Int min, out Vector3Int max)
        {
            Bounds bounds = _collider.bounds;

            // 8頂点をワールド空間で取得
            Vector3[] worldCorners = new Vector3[8];
            Vector3 bmin = bounds.min;
            Vector3 bmax = bounds.max;
            worldCorners[0] = new Vector3(bmin.x, bmin.y, bmin.z);
            worldCorners[1] = new Vector3(bmax.x, bmin.y, bmin.z);
            worldCorners[2] = new Vector3(bmin.x, bmax.y, bmin.z);
            worldCorners[3] = new Vector3(bmax.x, bmax.y, bmin.z);
            worldCorners[4] = new Vector3(bmin.x, bmin.y, bmax.z);
            worldCorners[5] = new Vector3(bmax.x, bmin.y, bmax.z);
            worldCorners[6] = new Vector3(bmin.x, bmax.y, bmax.z);
            worldCorners[7] = new Vector3(bmax.x, bmax.y, bmax.z);

            // 各頂点をstoneのローカル空間に変換
            Vector3 localMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 localMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 local = _stoneTransform.InverseTransformPoint(worldCorners[i]);
                localMin = Vector3.Min(localMin, local);
                localMax = Vector3.Max(localMax, local);
            }

            min = Vector3Int.FloorToInt(localMin);
            max = Vector3Int.CeilToInt(localMax);
        }

        /// <summary>
        /// フィードバックを再生する
        /// </summary>
        private void PlayFeedback(in int removedCount)
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

            if (_carvedParticle != null)
            {
                _carvedParticle.transform.position = _colliderTransform.position;

                EmissionModule carvedParticleEmissionModule = _carvedParticle.emission;
                Burst burstSetting = carvedParticleEmissionModule.GetBurst(0);
                burstSetting.count = Math.Max(2, removedCount / 512);
                carvedParticleEmissionModule.SetBurst(0, burstSetting);

                MainModule carvedParticleMainModule = _carvedParticle.main;
                float scaleMaginification = _stoneTransform.localScale.x / _initialStoneScaleX;
                float min_size = 0.01f * scaleMaginification;
                float max_size = 0.03f * scaleMaginification;
                carvedParticleMainModule.startSizeX = new MinMaxCurve(min_size, max_size);
                carvedParticleMainModule.startSizeY = new MinMaxCurve(min_size, max_size);
                carvedParticleMainModule.startSizeZ = new MinMaxCurve(min_size, max_size);
                _carvedParticle.Play();
            }
        }

        private void SetAim()
        {
            _isAimed = true;
            _dummyChisel = Instantiate(gameObject, transform.position, transform.rotation);
            _dummyChisel.name = DUMMY_CHISEL_NAME;

            // dummyChisel配下のImpactCenterを探してコライダを設定
            var impactCenter = _dummyChisel.transform.Find("ImpactCenter");
            if (impactCenter != null)
            {
                _collider = impactCenter.GetComponent<Collider>();
                _colliderTransform = _collider.transform;
            }

            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.gameObject != _dummyChisel)
                {
                    mr.enabled = false;
                }
            }
        }

        private void UnsetAim()
        {
            _isAimed = false;

            // this.gameObject配下のImpactCenterを探してコライダを設定
            var impactCenter = transform.Find("ImpactCenter");
            if (impactCenter != null)
            {
                _collider = impactCenter.GetComponent<Collider>();
                _colliderTransform = _collider.transform;
            }

            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.gameObject != _dummyChisel)
                {
                    mr.enabled = true;
                }
            }
            if (_dummyChisel.name == DUMMY_CHISEL_NAME)
            {
                Destroy(_dummyChisel);
            }
        }

        private void OnDestroy()
        {
            if (_carvedParticle != null)
            {
                Destroy(_carvedParticle.gameObject);
            }
        }
    }
}
