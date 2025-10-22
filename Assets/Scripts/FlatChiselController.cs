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
            Vector3 center = currentImpactCenterLocalPosition;

            _impactRange = impactRange;

            // 法線方向（ワールド→ローカル変換）
            Vector3 normalWorld = transform.up; // 通常はupが法線
            Vector3 normalLocal = _targetTransform.InverseTransformDirection(normalWorld).normalized;

            // 法線に直交する2軸を取得
            Vector3 axis1, axis2;
            // 法線がY軸に近い場合はX/Zを使う
            if (Mathf.Abs(Vector3.Dot(normalLocal, Vector3.up)) > 0.9f)
                axis1 = Vector3.right;
            else
                axis1 = Vector3.up;
            axis2 = Vector3.Cross(normalLocal, axis1).normalized;
            axis1 = Vector3.Cross(axis2, normalLocal).normalized;

            float halfNormal = _impactRange * 2;
            float halfOther = _impactRange;

            int removedCount = 0;

            // 探索範囲を決定（直方体を囲むAABBでループ）
            // 直方体の8頂点を計算しAABBを求める
            Vector3[] corners = new Vector3[8];
            int idx = 0;
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                    for (int k = -1; k <= 1; k += 2)
                        corners[idx++] = center
                            + normalLocal * halfNormal * i
                            + axis1 * halfOther * j
                            + axis2 * halfOther * k;

            Vector3 min = corners[0], max = corners[0];
            foreach (var c in corners)
            {
                min = Vector3.Min(min, c);
                max = Vector3.Max(max, c);
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
                        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        // 直方体内か判定
                        Vector3 rel = pos - center;
                        float dNormal = Vector3.Dot(rel, normalLocal);
                        float d1 = Vector3.Dot(rel, axis1);
                        float d2 = Vector3.Dot(rel, axis2);

                        if (Mathf.Abs(dNormal) <= halfNormal &&
                            Mathf.Abs(d1) <= halfOther &&
                            Mathf.Abs(d2) <= halfOther)
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
