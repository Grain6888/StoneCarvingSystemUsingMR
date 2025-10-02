using UnityEngine;

public class NaiveSurfaceNets : MonoBehaviour
{
    [SerializeField] private ComputeShader _computeShader = null;
    [SerializeField] private Material _material = null;
    [SerializeField] private int _voxelSize = 32;

    private GraphicsBuffer _sdfVoxelBuffer;
    private GraphicsBuffer _vertexIdBuffer;
    private GraphicsBuffer _vertexBuffer;
    private GraphicsBuffer _indexBuffer;
    private GraphicsBuffer _neighborBuffer;
    private GraphicsBuffer _edgeBuffer;
    private GraphicsBuffer _indirectArgBuffer;
    private GraphicsBuffer _normalBuffer;
    private Bounds _bounds;
    private int _klVertices, _klIndices, _klIndirectArgs;
    private Vector3Int _tgVertices, _tgIndices;

    // SDFデータを保持
    private float[] _sdfVoxels;

    // デバッグ用
    private Vector3[] _debugVertices;
    private int[] _debugIndices;

    private static readonly int _spSdfVoxelSize = Shader.PropertyToID("SdfVoxelSize");
    private static readonly int _spSdfVoxels = Shader.PropertyToID("SdfVoxels");
    private static readonly int _spVertexIds = Shader.PropertyToID("VertexIds");
    private static readonly int _spVertices = Shader.PropertyToID("Vertices");
    private static readonly int _spIndices = Shader.PropertyToID("Indices");
    private static readonly int _spNeighbors = Shader.PropertyToID("Neighbors");
    private static readonly int _spEdges = Shader.PropertyToID("Edges");
    private static readonly int _spIndirectArgs = Shader.PropertyToID("IndirectArgs");

    private void Start()
    {
        // SDFデータ生成
        _sdfVoxels = FillVoxel(_voxelSize);

        // バッファ初期化
        int size = _voxelSize;
        _sdfVoxelBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size, sizeof(float));
        _sdfVoxelBuffer.SetData(_sdfVoxels);
        _vertexIdBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, size * size * size, sizeof(int));
        _vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, size * size * size, sizeof(float) * 3);
        _vertexBuffer.SetCounterValue(0);
        _indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, size * size * size * 18, sizeof(int));
        _indexBuffer.SetCounterValue(0);
        _indirectArgBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, sizeof(uint));
        _indirectArgBuffer.SetData(new uint[4] { 0, 1, 0, 0 });

        // Neighbors
        var neighbors = new Vector3Int[]
        {
            new Vector3Int( 0, 0, 0 ),
            new Vector3Int( 1, 0, 0 ),
            new Vector3Int( 1, 0, 1 ),
            new Vector3Int( 0, 0, 1 ),
            new Vector3Int( 0, 1, 0 ),
            new Vector3Int( 1, 1, 0 ),
            new Vector3Int( 1, 1, 1 ),
            new Vector3Int( 0, 1, 1 ),
        };
        _neighborBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, neighbors.Length, sizeof(int) * 3);
        _neighborBuffer.SetData(neighbors);

        // Edges
        var edges = new Vector2Int[]
        {
            new Vector2Int( 0, 1 ),
            new Vector2Int( 1, 2 ),
            new Vector2Int( 2, 3 ),
            new Vector2Int( 3, 0 ),
            new Vector2Int( 4, 5 ),
            new Vector2Int( 5, 6 ),
            new Vector2Int( 6, 7 ),
            new Vector2Int( 7, 4 ),
            new Vector2Int( 0, 4 ),
            new Vector2Int( 1, 5 ),
            new Vector2Int( 2, 6 ),
            new Vector2Int( 3, 7 ),
        };
        _edgeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, edges.Length, sizeof(int) * 2);
        _edgeBuffer.SetData(edges);

        float bsize = size;
        _bounds = new Bounds(new Vector3(bsize * 0.5f, bsize * 0.5f, bsize * 0.5f), new Vector3(bsize, bsize, bsize));
        _material.SetBuffer(_spVertices, _vertexBuffer);
        _material.SetBuffer(_spIndices, _indexBuffer);

        uint numThreadX, numThreadY, numThreadZ;
        _computeShader.SetInt(_spSdfVoxelSize, size);

        // Vertices
        _klVertices = _computeShader.FindKernel("GenerateVertices");
        _computeShader.SetBuffer(_klVertices, _spSdfVoxels, _sdfVoxelBuffer);
        _computeShader.SetBuffer(_klVertices, _spVertexIds, _vertexIdBuffer);
        _computeShader.SetBuffer(_klVertices, _spVertices, _vertexBuffer);
        _computeShader.SetBuffer(_klVertices, _spEdges, _edgeBuffer);
        _computeShader.SetBuffer(_klVertices, _spNeighbors, _neighborBuffer);

        _computeShader.GetKernelThreadGroupSizes(_klVertices, out numThreadX, out numThreadY, out numThreadZ);
        _tgVertices.x = ((size - 1) + (int)(numThreadX - 1)) / (int)numThreadX;
        _tgVertices.y = ((size - 1) + (int)(numThreadY - 1)) / (int)numThreadY;
        _tgVertices.z = ((size - 1) + (int)(numThreadZ - 1)) / (int)numThreadZ;

        // Indices
        _klIndices = _computeShader.FindKernel("GenerateIndices");
        _computeShader.SetBuffer(_klIndices, _spSdfVoxels, _sdfVoxelBuffer);
        _computeShader.SetBuffer(_klIndices, _spVertexIds, _vertexIdBuffer);
        _computeShader.SetBuffer(_klIndices, _spVertices, _vertexBuffer);
        _computeShader.SetBuffer(_klIndices, _spIndices, _indexBuffer);
        _computeShader.SetBuffer(_klIndices, _spNeighbors, _neighborBuffer);

        _computeShader.GetKernelThreadGroupSizes(_klIndices, out numThreadX, out numThreadY, out numThreadZ);
        _tgIndices.x = ((size - 2) + (int)(numThreadX - 1)) / (int)numThreadX;
        _tgIndices.y = ((size - 2) + (int)(numThreadY - 1)) / (int)numThreadY;
        _tgIndices.z = ((size - 2) + (int)(numThreadZ - 1)) / (int)numThreadZ;

        // IndirectArgs
        _klIndirectArgs = _computeShader.FindKernel("UpdateIndirectArgs");
        _computeShader.SetBuffer(_klIndirectArgs, _spIndices, _indexBuffer);
        _computeShader.SetBuffer(_klIndirectArgs, _spIndirectArgs, _indirectArgBuffer);

        // 法線バッファ生成
        _normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _indexBuffer.count, sizeof(int));
        // ComputeShaderにセット
        _computeShader.SetBuffer(_klVertices, "Normals", _normalBuffer);
        _computeShader.SetInt("normalStride", sizeof(int) * 3);
    }

    private void OnDestroy()
    {
        _sdfVoxelBuffer?.Dispose();
        _vertexIdBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _neighborBuffer?.Dispose();
        _edgeBuffer?.Dispose();
        _indirectArgBuffer?.Dispose();
        _normalBuffer?.Dispose();
    }

    private void LateUpdate()
    {
        // Dispatch
        _vertexBuffer.SetCounterValue(0);
        _indexBuffer.SetCounterValue(0);
        _computeShader.Dispatch(_klVertices, _tgVertices.x, _tgVertices.y, _tgVertices.z);
        _computeShader.Dispatch(_klIndices, _tgIndices.x, _tgIndices.y, _tgIndices.z);
        _computeShader.Dispatch(_klIndirectArgs, 1, 1, 1);
        GL.Flush();

        // 頂点・インデックスバッファの内容を取得
        int vertexCount = _vertexBuffer.count;
        int indexCount = _indexBuffer.count;

        if (_debugVertices == null || _debugVertices.Length != vertexCount)
            _debugVertices = new Vector3[vertexCount];
        if (_debugIndices == null || _debugIndices.Length != indexCount)
            _debugIndices = new int[indexCount];

        _vertexBuffer.GetData(_debugVertices);
        _indexBuffer.GetData(_debugIndices);

        if (_indexBuffer != null)
            Graphics.DrawProceduralIndirect(_material, _bounds, MeshTopology.Triangles, _indirectArgBuffer);
    }

    // FillVoxel: SDF配列を生成するメソッド（例）
    private float[] FillVoxel(int size)
    {
        var voxels = new float[size * size * size];
        float centerX = size / 2f;
        float centerY = size / 2f;
        float centerZ = size / 2f;
        float rx = centerX * 0.9f;
        float ry = centerY * 0.9f;
        float rz = centerZ * 0.9f;
        float halfCube = Mathf.Min(centerX, Mathf.Min(centerY, centerZ)) * 0.9f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (Mathf.Abs(x - y) < 7.0f || Mathf.Abs((size - x) - y) < 7.0f)
                    {
                        //voxel[x + z * size.x + y * size.x * size.z] = 1.0f;
                        //continue;
                    }

                    float dx = (x - centerX) / rx;
                    float dy = (y - centerY) / ry;
                    float dz = (z - centerZ) / rz;
                    // 立方体SDF: max(|dx|, |dy|, |dz|) - halfCube
                    float dist = Mathf.Max(dx, Mathf.Max(dy, dz)) - halfCube;
                    float ellipsoidSDF = Mathf.Sqrt(dx * dx + dy * dy + dz * dz) - 1.0f;

                    if (ellipsoidSDF <= 0.0f)
                    {
                        voxels[x + z * size + y * size * size] = dist;
                    }
                    else
                    {
                        voxels[x + z * size + y * size * size] = 1.0f;
                    }
                }
            }
        }

        return voxels;
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.blue;
    //    Gizmos.DrawWireCube(_bounds.center, _bounds.size);

    //    // 頂点デバッグ描画
    //    if (_debugVertices != null)
    //    {
    //        Gizmos.color = Color.red;
    //        foreach (var v in _debugVertices)
    //            Gizmos.DrawSphere(v, 0.1f);
    //    }

    //    // 三角形デバッグ描画
    //    if (_debugVertices != null && _debugIndices != null)
    //    {
    //        Gizmos.color = Color.green;
    //        for (int i = 0; i + 2 < _indexBuffer.count; i += 3)
    //        {
    //            Vector3 v0 = _debugVertices[_debugIndices[i]];
    //            Vector3 v1 = _debugVertices[_debugIndices[i + 1]];
    //            Vector3 v2 = _debugVertices[_debugIndices[i + 2]];
    //            Gizmos.DrawLine(v0, v1);
    //            Gizmos.DrawLine(v1, v2);
    //            Gizmos.DrawLine(v2, v0);
    //        }
    //    }
    //}
}
