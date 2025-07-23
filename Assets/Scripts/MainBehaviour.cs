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

        public GameObject leftController = null;
        public GameObject rightController = null;

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
            Vector3 leftControllerPosition = leftController.transform.position;

            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            //// ワールド座標→ローカル座標（必要なら）
            //Vector3 localPos = transform.InverseTransformPoint(leftControllerPosition);

            //// ローカル座標→ボクセルグリッド座標
            //int x = Mathf.FloorToInt(localPos.x);
            //int y = Mathf.FloorToInt(localPos.y);
            //int z = Mathf.FloorToInt(localPos.z);

            //if (x >= 0 && x < _voxelDataChunk.xLength &&
            //    y >= 0 && y < _voxelDataChunk.yLength &&
            //    z >= 0 && z < _voxelDataChunk.zLength)
            //{
            //    DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
            //    //xzLayer.RemoveAllFlags(x, y, z);
            //    for (int i = 0; i < xzLayer.Length; i++)
            //    {
            //        xzLayer.RemoveAllFlags(i);
            //        //if (i != 0)
            //        //{
            //        //    xzLayer.AddFlag(i, CellFlags.IsFilled);
            //        //}
            //    }
            //    _renderer.UpdateRenderBuffer(xzLayer, y);
            //    Debug.Log("x:" + x + "y:" + y + "z:" + z);
            //}

            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
            _voxelDataChunk.Dispose();
        }

        private void OnDrawGizmos()
        {
            if (_voxelDataChunk.Equals(default(DataChunk))) return;

            // 距離判定の基準点（例：Sceneビューのカメラ位置）
            Vector3 referencePos = Camera.current != null ? Camera.current.transform.position : Vector3.zero;
            float visibleDistance = 3.0f; // しきい値（必要に応じて調整）

            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
                for (int i = 0; i < xzLayer.Length; i++)
                {
                    if (!xzLayer.HasFlag(i, CellFlags.IsFilled)) continue;

                    xzLayer.GetPosition(i, out int x, out _, out int z);
                    Vector3 localPos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Vector3 worldPos = transform.TransformPoint(localPos);

                    // 距離判定
                    if ((worldPos - referencePos).sqrMagnitude > visibleDistance * visibleDistance)
                        continue;

                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(worldPos, Vector3.one);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(worldPos, $"({x},{y},{z})");
#endif
                }
            }
        }
    }
}
