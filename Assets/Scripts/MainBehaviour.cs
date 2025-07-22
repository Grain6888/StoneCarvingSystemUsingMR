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

        private int _currentLayerToClear = 0;
        private int _frameCounter = 0;
        private bool _firstUpdate = true;
        public int _startFlame = 1000;

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
                float centerX = (_boundsSize.x - 1) / 2.0f;
                float centerY = (_boundsSize.y - 1) / 2.0f;
                float centerZ = (_boundsSize.z - 1) / 2.0f;
                float radius = math.min(_boundsSize.x, math.min(_boundsSize.y, _boundsSize.z)) / 2.0f;

                for (int i = 0; i < xzLayer.Length; i++)
                {
                    //xzLayer.GetPosition(i, out int x, out _, out int z);
                    //float dx = x - centerX;
                    //float dy = y - centerY;
                    //float dz = z - centerZ;
                    //float distanceSq = dx * dx + dy * dy + dz * dz;
                    //if (distanceSq <= radius * radius)
                    //{
                    //    xzLayer.AddFlag(i, CellFlags.IsFilled);
                    //}
                    xzLayer.AddFlag(i, CellFlags.IsFilled);
                }
                _renderer.AddRenderBuffer(xzLayer, y);
            }
        }

        void Update()
        {
            Vector3 leftControllerPosition = leftController.transform.position;
            //Vector3 rightControllerPosition = rightController.transform.position;

            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            //_frameCounter++;

            //if (_firstUpdate && _frameCounter <= _startFlame)
            //{
            //    _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
            //    return;
            //}
            //_firstUpdate = false;

            //if (_frameCounter >= 0 && _currentLayerToClear < _voxelDataChunk.yLength)
            //{
            //    _frameCounter = 0;
            //    DataChunk xzLayer = _voxelDataChunk.GetXZLayer(_currentLayerToClear);
            //    float centerX = (_boundsSize.x - 1) / 2.0f;
            //    float centerY = (_boundsSize.y - 1) / 2.0f;
            //    float centerZ = (_boundsSize.z - 1) / 2.0f;
            //    float radius = math.min(_boundsSize.x, math.min(_boundsSize.y, _boundsSize.z)) / 2.0f;

            //    for (int i = 0; i < xzLayer.Length; i++)
            //    {
            //        xzLayer.GetPosition(i, out int x, out _, out int z);
            //        float dx = x - centerX;
            //        float dy = _currentLayerToClear - centerY;
            //        float dz = z - centerZ;
            //        float distanceSq = dx * dx + dy * dy + dz * dz;
            //        xzLayer.RemoveAllFlags(i);
            //        if (distanceSq <= radius * radius)
            //        {
            //            xzLayer.AddFlag(i, CellFlags.IsFilled);
            //        }
            //        //xzLayer.AddFlag(i, CellFlags.IsFilled);
            //    }
            //    _renderer.UpdateRenderBuffer(xzLayer, _currentLayerToClear);
            //    _currentLayerToClear++;
            //}

            // ワールド座標→ローカル座標（必要なら）
            Vector3 localPos = transform.InverseTransformPoint(leftControllerPosition);

            // ローカル座標→ボクセルグリッド座標
            int x = Mathf.FloorToInt(localPos.x);
            int y = Mathf.FloorToInt(localPos.y);
            int z = Mathf.FloorToInt(localPos.z);

            if (x >= 0 && x < _voxelDataChunk.xLength &&
                y >= 0 && y < _voxelDataChunk.yLength &&
                z >= 0 && z < _voxelDataChunk.zLength)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
                //xzLayer.RemoveAllFlags(x, y, z);
                for (int i = 0; i < xzLayer.Length; i++)
                {
                    xzLayer.RemoveAllFlags(i);
                    //if (i != 0)
                    //{
                    //    xzLayer.AddFlag(i, CellFlags.IsFilled);
                    //}
                }
                _renderer.UpdateRenderBuffer(xzLayer, y);
                Debug.Log("x:" + x + "y:" + y + "z:" + z);
            }

            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
            _voxelDataChunk.Dispose();
        }
    }
}
