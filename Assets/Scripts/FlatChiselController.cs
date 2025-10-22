using UnityEngine;
using Oculus.Haptics;

namespace MRSculpture
{
    public class FlatChiselController : MonoBehaviour
    {
        [SerializeField] private HapticSource _hapticSource;
        [SerializeField] private AudioSource _audioSource;

        private int _impactRange;
        [SerializeField] private GameObject _center;
        private Transform _centerPosition;
        [SerializeField] private GameObject _target;
        [SerializeField] private GameObject _dummy;
        private GameObject _dummyInstance;

        private Transform _targetTransform;

        private bool _isPressingTrigger = false;

        private void Awake()
        {
            _targetTransform = _target.transform;
        }

        public void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            {
                DownTriggerButton();
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
            {
                UpTriggerButton();
            }
        }

        public void Carve(ref DataChunk voxelDataChunk, in int impactRange, ref Renderer renderer)
        {
            // ワールド座標を取得
            Vector3 impactCenterWorldPosition;
            if (_isPressingTrigger)
            {
                impactCenterWorldPosition = _centerPosition.position;
            }
            else
            {
                impactCenterWorldPosition = _center.transform.position;
            }
            // ワールド座標 → ターゲットのローカル座標へ変換
            Vector3 currentImpactCenterLocalPosition = _targetTransform.InverseTransformPoint(impactCenterWorldPosition);
            // ローカル座標をボクセル単位に合わせる
            Vector3Int center = Vector3Int.RoundToInt(currentImpactCenterLocalPosition);

            _impactRange = impactRange;
            // X方向の探索範囲（visibleDistance分だけ前後に拡張、範囲外はクランプ）
            int minX = Mathf.Max(0, center.x - impactRange);
            int maxX = Mathf.Min(voxelDataChunk.xLength - 1, center.x + impactRange);
            // Y方向の探索範囲
            int minY = Mathf.Max(0, center.y - impactRange);
            int maxY = Mathf.Min(voxelDataChunk.yLength - 1, center.y + impactRange);
            // Z方向の探索範囲
            int minZ = Mathf.Max(0, center.z - impactRange);
            int maxZ = Mathf.Min(voxelDataChunk.zLength - 1, center.z + impactRange);

            // 距離判定用にvisibleDistanceの2乗を事前計算（パフォーマンス向上のため）
            float sqrVisibleDistance = impactRange * impactRange;
            int removedCount = 0;

            // 各XZレイヤごとに処理
            for (int y = minY; y <= maxY; y++)
            {
                DataChunk xzLayer = voxelDataChunk.GetXZLayer(y);
                bool layerBufferNeedsUpdate = false;

                // Y層のXZ平面のDataChunkを取得
                for (int x = minX; x <= maxX; x++)
                {
                    // Z方向の範囲をループ
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Vector3 cellLocalPos = new(x + 0.5f, y + 0.5f, z + 0.5f);

                        // 破壊中心との距離がvisibleDistance以内か判定
                        if ((cellLocalPos - center).sqrMagnitude > sqrVisibleDistance)
                            continue;

                        if (xzLayer.HasFlag(x, 0, z, CellFlags.IsFilled))
                        {
                            xzLayer.RemoveFlag(x, 0, z, CellFlags.IsFilled);
                            removedCount++;
                            layerBufferNeedsUpdate = true;
                        }
                    }
                }

                // 現在のレイヤに含まれる何らかのセルが更新された場合のみレンダーバッファを更新
                if (layerBufferNeedsUpdate)
                {
                    renderer.UpdateRenderBuffer(xzLayer, y);
                }
            }

            if (removedCount > 0)
            {
                PlayFeedback();
            }
        }

        private void DownTriggerButton()
        {
            _isPressingTrigger = true;
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                mesh.enabled = false;
            }

            _dummyInstance = Instantiate(_dummy, gameObject.transform.position, gameObject.transform.rotation);
            _centerPosition = _dummyInstance.transform.Find("ImpactCenter");
        }

        private void UpTriggerButton()
        {
            _isPressingTrigger = false;
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                mesh.enabled = true;
            }

            Destroy(_dummyInstance);
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
