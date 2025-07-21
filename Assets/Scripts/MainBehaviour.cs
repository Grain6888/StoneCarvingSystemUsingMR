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
                    xzLayer.GetPosition(i, out int x, out _, out int z);
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float dz = z - centerZ;
                    float distanceSq = dx * dx + dy * dy + dz * dz;
                    if (distanceSq <= radius * radius)
                    {
                        xzLayer.AddFlag(i, CellFlags.IsFilled);
                    }
                }
                _renderer.AddRenderBuffer(xzLayer, y);
            }
        }

        void Update()
        {
            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);
            _frameCounter++;

            if (_firstUpdate && _frameCounter <= 100)
            {
                boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);
                _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
                return;
            }
            _firstUpdate = false;

            if (_frameCounter >= 0 && _currentLayerToClear < _voxelDataChunk.yLength)
            {
                _frameCounter = 0;
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(_currentLayerToClear);
                for (int i = 0; i < xzLayer.Length; i++)
                {
                    xzLayer.RemoveAllFlags(i);
                    xzLayer.AddFlag(i, CellFlags.IsFilled);
                }
                _renderer.UpdateRenderBuffer(xzLayer, _currentLayerToClear);
                _currentLayerToClear++;
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
