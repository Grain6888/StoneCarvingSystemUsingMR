using Unity.Mathematics;
using UnityEngine;

namespace MRSculpture
{
    public class MainBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 彫刻素材の生成範囲
        /// </summary>
        public int3 _boundsSize = new(100, 100, 100);

        /// <summary>
        /// 彫刻素材のボクセルデータを保存するDataChunk
        /// </summary>
        private DataChunk _voxelDataChunk;

        /// <summary>
        /// ボクセル → メッシュに変換・描画を管理するレンダラ
        /// </summary>
        private Renderer _renderer;

        /// <summary>
        /// ボクセルメッシュ
        /// </summary>
        [SerializeField] private Mesh _voxelMesh;

        /// <summary>
        /// ボクセルマテリアル
        /// </summary>
        [SerializeField] private Material _voxelMaterial;

        public GameObject leftControllerAnchor = null;
        public GameObject rightControllerAnchor = null;
        [SerializeField] private Transform mainBehaviourTransform;


        private void Awake()
        {
            // DataChunkを生成し，3Dデータを保持
            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            // ローカル → ワールド座標系の変換行列
            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

            _renderer = new Renderer(_voxelMesh, _voxelMaterial, localToWorldMatrix);

            // 全レイヤ分処理
            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);

                for (int i = 0; i < xzLayer.Length; i++)
                {
                    xzLayer.AddFlag(i, CellFlags.IsFilled);
                }
                _renderer.AddRenderBuffer(xzLayer, y);
            }
        }

        void Update()
        {
            Vector3 leftControllerWorldPosition = leftControllerAnchor.transform.position;
            float visibleDistance = 0.25f;

            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
                for (int i = 0; i < xzLayer.Length; i++)
                {
                    xzLayer.GetPosition(i, out int x, out _, out int z);
                    Vector3 localPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 worldPos = transform.TransformPoint(localPos);

                    if ((worldPos - leftControllerWorldPosition).sqrMagnitude > visibleDistance * visibleDistance)
                    {
                        xzLayer.RemoveFlag(i, CellFlags.IsSelected);
                    }
                    else
                    {
                        xzLayer.AddFlag(i, CellFlags.IsSelected);
                        xzLayer.RemoveFlag(i, CellFlags.IsFilled);
                    }
                }
                _renderer.UpdateRenderBuffer(xzLayer, y);
            }

            //Vector3 leftControllerLocalPosition = mainBehaviourTransform.InverseTransformPoint(leftControllerWorldPosition);

            //// 範囲内セルのインデックス範囲を計算
            //int minX = Mathf.Max(0, Mathf.FloorToInt(leftControllerLocalPosition.x - visibleDistance));
            //int maxX = Mathf.Min(_voxelDataChunk.xLength - 1, Mathf.FloorToInt(leftControllerLocalPosition.x + visibleDistance));
            //int minY = Mathf.Max(0, Mathf.FloorToInt(leftControllerLocalPosition.y - visibleDistance));
            //int maxY = Mathf.Min(_voxelDataChunk.yLength - 1, Mathf.FloorToInt(leftControllerLocalPosition.y + visibleDistance));
            //int minZ = Mathf.Max(0, Mathf.FloorToInt(leftControllerLocalPosition.z - visibleDistance));
            //int maxZ = Mathf.Min(_voxelDataChunk.zLength - 1, Mathf.FloorToInt(leftControllerLocalPosition.z + visibleDistance));

            //for (int y = minY; y <= maxY; y++)
            //{
            //    for (int x = minX; x <= maxX; x++)
            //    {
            //        for (int z = minZ; z <= maxZ; z++)
            //        {
            //            Vector3 cellLocalPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
            //            Vector3 cellWorldPos = transform.TransformPoint(cellLocalPos);

            //            // ワールド空間での距離判定
            //            if ((cellWorldPos - leftControllerLocalPosition).sqrMagnitude <= visibleDistance * visibleDistance)
            //            {
            //                _voxelDataChunk.AddFlag(x, y, z, CellFlags.IsSelected);
            //                //_voxelDataChunk.RemoveFlag(x, y, z, CellFlags.IsFilled);
            //            }
            //            else
            //            {
            //                _voxelDataChunk.RemoveFlag(x, y, z, CellFlags.IsSelected);
            //            }
            //        }
            //    }
            //}

            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
            _voxelDataChunk.Dispose();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_voxelDataChunk.Equals(default(DataChunk))) return;

            Vector3 referencePos = Camera.current != null ? Camera.current.transform.position : Vector3.zero;
            float visibleDistance = 3.0f;

            Vector3 scale = transform.lossyScale;

            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
                for (int i = 0; i < xzLayer.Length; i++)
                {
                    if (!xzLayer.HasFlag(i, CellFlags.IsFilled))
                    {
                        continue;
                    }

                    xzLayer.GetPosition(i, out int x, out _, out int z);
                    Vector3 localPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 worldPos = transform.TransformPoint(localPos);

                    if ((worldPos - referencePos).sqrMagnitude > visibleDistance * visibleDistance)
                    {
                        continue;
                    }

                    if (xzLayer.HasFlag(i, CellFlags.IsSelected))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(worldPos, scale);
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireCube(worldPos, scale);
                    }

                    UnityEditor.Handles.Label(worldPos, $"({worldPos.x},{worldPos.y},{worldPos.z})");
                }
            }
        }
#endif
    }
}
