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
            Vector3 center = currentImpactCenterLocalPosition;

            _impactRange = impactRange;

            Vector3 forwardWorld = transform.forward;
            Vector3 forwardLocal = _targetTransform.InverseTransformDirection(forwardWorld).normalized;

            // 法線に直交する2軸を取得
            Vector3 axis1, axis2;
            // 法線がY軸に近い場合はX/Zを使う
            if (Mathf.Abs(Vector3.Dot(forwardLocal, Vector3.up)) > 0.9f)
            {
                axis1 = Vector3.right;
            }
            else
            {
                axis1 = Vector3.up;
            }
            axis2 = Vector3.Cross(forwardLocal, axis1).normalized;
            axis1 = Vector3.Cross(axis2, forwardLocal).normalized;

            float xLength = _impactRange;
            float yLength = _impactRange * 2;
            float zLength = _impactRange * 4;

            int removedCount = 0;

            // 探索範囲を決定（直方体を囲むAABBでループ）
            // 直方体の8頂点を計算しAABBを求める
            Vector3[] corners = new Vector3[8];
            int index = 0;
            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    for (int k = -1; k <= 1; k += 2)
                    {
                        corners[index++] = center + i * xLength * forwardLocal + j * yLength * axis1 + k * zLength * axis2;
                    }
                }
            }
            Vector3 min = corners[0];
            Vector3 max = corners[0];
            foreach (var corner in corners)
            {
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }

            int minX = Mathf.Max(0, Mathf.FloorToInt(min.x));
            int maxX = Mathf.Min(voxelDataChunk.xLength - 1, Mathf.CeilToInt(max.x));
            int minY = Mathf.Max(0, Mathf.FloorToInt(min.y));
            int maxY = Mathf.Min(voxelDataChunk.yLength - 1, Mathf.CeilToInt(max.y));
            int minZ = Mathf.Max(0, Mathf.FloorToInt(min.z));
            int maxZ = Mathf.Min(voxelDataChunk.zLength - 1, Mathf.CeilToInt(max.z));

            // 直方体内判定
            for (int y = minY; y <= maxY; y++)
            {
                DataChunk xzLayer = voxelDataChunk.GetXZLayer(y);
                bool layerBufferNeedsUpdate = false;

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Vector3 pos = new(x + 0.5f, y + 0.5f, z + 0.5f);
                        // 直方体内か判定
                        Vector3 rel = pos - center;
                        float dNormal = Vector3.Dot(rel, forwardLocal);
                        float d1 = Vector3.Dot(rel, axis1);
                        float d2 = Vector3.Dot(rel, axis2);

                        if (Mathf.Abs(dNormal) <= xLength &&
                            Mathf.Abs(d1) <= yLength &&
                            Mathf.Abs(d2) <= zLength)
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
