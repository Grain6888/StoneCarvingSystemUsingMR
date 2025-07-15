using Unity.Mathematics;
using UnityEngine;

public class Behaviour : MonoBehaviour
{
    /// <summary>
    /// 彫刻素材の生成範囲
    /// </summary>
    public int3 size = new(100, 100, 100);

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

    private void Awake()
    {
        // DataChunkを生成し，3Dデータを保持
        _voxelDataChunk = new DataChunk(size.x, size.y, size.z);

        // ローカル → ワールド座標系の変換行列
        Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

        _renderer = new Renderer(_voxelMesh, _voxelMaterial, localToWorldMatrix);

        DataChunk initialXZLayer = _voxelDataChunk.GetXZLayer(0);
        for (int i = 0; i < initialXZLayer.Length; i++)
        {
            if (i % 2 == 0)
            {
                continue;
            }
            initialXZLayer.AddFlag(i, CellFlags.IsFilled);
        }
    }

    /// <summary>
    /// 現在のY層
    /// </summary>
    private int _currentYIndex;

    /// <summary>
    /// 全層描画済みフラグ
    /// </summary>
    private bool _isAllLayerRendered = false;

    /// <summary>
    /// フレームカウンター
    /// </summary>
    private int _frameCounter = 0;

    // Update is called once per frame
    void Update()
    {
        if (_currentYIndex < _voxelDataChunk.yLength)
        {
            _frameCounter++;
            if (_frameCounter < 10)
            {
                return;
            }
            _frameCounter = 0;

            if (_currentYIndex == 0)
            {
                _renderer.AddRenderBuffer(_voxelDataChunk.GetXZLayer(0), 0);
            }
            else
            {
                DataChunk currentXZLayer = _voxelDataChunk.GetXZLayer(_currentYIndex);

                for (int i = 0; i < currentXZLayer.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        continue;
                    }
                    currentXZLayer.AddFlag(i, CellFlags.IsFilled);
                }

                _renderer.AddRenderBuffer(currentXZLayer, _currentYIndex);
            }

            _currentYIndex++;
            return;
        }

        if (_currentYIndex >= _voxelDataChunk.yLength)
        {
            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(size.x, size.y, size.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);
            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));

            if (!_isAllLayerRendered)
            {
                _isAllLayerRendered = true;
            }
            return;
        }
    }

    private void OnDestroy()
    {
        _renderer.Dispose();
        _voxelDataChunk.Dispose();
    }

    private void OnDrawGizmos()
    {
        if (_voxelDataChunk.Equals(default(DataChunk))) return;

        // 現在のY層
        int y = _currentYIndex;
        // スケールをボクセルサイズとして使用
        Vector3 voxelSize = transform.localScale;
        if (y < 0 || y >= _voxelDataChunk.yLength) return;

        Bounds bounds = _voxelDataChunk.GetXZLayerBounds(y, voxelSize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
