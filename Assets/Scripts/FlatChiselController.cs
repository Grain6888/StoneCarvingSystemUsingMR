using UnityEngine;
using Oculus.Haptics;

namespace MRSculpture
{
    public class FlatChiselController : MonoBehaviour
    {
        [SerializeField] private HapticSource _hapticSource;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private OVRInput.Controller _controllerWithFlatChisel;
        private GameObject _questController;

        private int _impactRange;
        [SerializeField] private GameObject _center;
        private Transform _centerPosition;
        [SerializeField] private GameObject _target;
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
            _impactRange = impactRange;

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
            Vector3 depthDirection = -transform.right;
            Vector3 heightDirection = transform.forward;
            Vector3 widthDirection = transform.up;

            float height = _impactRange / 2;
            float width = _impactRange;
            float depth = _impactRange;

            // 探索範囲を決定（直方体を囲むAABBでループ）
            // 直方体の8頂点を計算しAABBを求める
            Vector3[] corners = new Vector3[8];
            corners[0] = center + height * heightDirection + width * widthDirection + depth * depthDirection;
            corners[1] = center + height * heightDirection + width * widthDirection - depth * depthDirection;
            corners[2] = center + height * heightDirection - width * widthDirection + depth * depthDirection;
            corners[3] = center + height * heightDirection - width * widthDirection - depth * depthDirection;
            corners[4] = center - height * heightDirection + width * widthDirection + depth * depthDirection;
            corners[5] = center - height * heightDirection + width * widthDirection - depth * depthDirection;
            corners[6] = center - height * heightDirection - width * widthDirection + depth * depthDirection;
            corners[7] = center - height * heightDirection - width * widthDirection - depth * depthDirection;

            Vector3 min = corners[0];
            Vector3 max = corners[0];
            foreach (var corner in corners)
            {
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }

            // X方向の探索範囲（visibleDistance分だけ前後に拡張、範囲外はクランプ）
            int minX = Mathf.Max(0, Mathf.FloorToInt(min.x));
            int maxX = Mathf.Min(voxelDataChunk.xLength - 1, Mathf.CeilToInt(max.x));
            // Y方向の探索範囲
            int minY = Mathf.Max(0, Mathf.FloorToInt(min.y));
            int maxY = Mathf.Min(voxelDataChunk.yLength - 1, Mathf.CeilToInt(max.y));
            // Z方向の探索範囲
            int minZ = Mathf.Max(0, Mathf.FloorToInt(min.z));
            int maxZ = Mathf.Min(voxelDataChunk.zLength - 1, Mathf.CeilToInt(max.z));

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

                        // 直方体内か判定
                        Vector3 rel = cellLocalPos - center;
                        float dHeight = Vector3.Dot(rel, heightDirection);
                        float dWidth = Vector3.Dot(rel, widthDirection);
                        float dDepth = Vector3.Dot(rel, depthDirection);

                        if (Mathf.Abs(dHeight) <= height && Mathf.Abs(dWidth) <= width && Mathf.Abs(dDepth) <= depth)
                        {
                            if (xzLayer.HasFlag(x, 0, z, CellFlags.IsFilled))
                            {
                                xzLayer.RemoveFlag(x, 0, z, CellFlags.IsFilled);
                                removedCount++;
                                layerBufferNeedsUpdate = true;
                            }
                        }
                    }
                }
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

            if (gameObject.name == "FlatChiselDummy") return;

            _dummyInstance = Instantiate(gameObject, gameObject.transform.position, gameObject.transform.rotation);
            _dummyInstance.name = "FlatChiselDummy";
            _centerPosition = _dummyInstance.transform.Find("ImpactCenter");

            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh != null) mesh.enabled = false;
        }

        private void UpTriggerButton()
        {
            _isPressingTrigger = false;

            if (gameObject.name == "FlatChiselDummy") return;

            Destroy(_dummyInstance);
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh != null) mesh.enabled = true;
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
