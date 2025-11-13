using UnityEngine;
using UnityEngine.Rendering;

namespace MarchingCubes
{
    //
    // Marching Cubes Algorithmによる等値面メッシュビルダー
    //
    sealed class MeshBuilder : System.IDisposable
    {
        #region Public members

        public Mesh Mesh => _mesh;

        public MeshBuilder(int x, int y, int z, int budget, ComputeShader compute)
          => Initialize((x, y, z), budget, compute);

        public MeshBuilder(Vector3Int dims, int budget, ComputeShader compute)
          => Initialize((dims.x, dims.y, dims.z), budget, compute);

        public void Dispose()
          => ReleaseAll();

        public void BuildIsosurface(ComputeBuffer voxels, float target, float scale)
          => RunCompute(voxels, target, scale);

        #endregion

        #region Private members

        (int x, int y, int z) _grids;
        int _triangleBudget;
        ComputeShader _compute;

        void Initialize((int, int, int) dims, int budget, ComputeShader compute)
        {
            _grids = dims;
            _triangleBudget = budget;
            _compute = compute;

            AllocateBuffers();
            AllocateMesh(3 * _triangleBudget);
        }

        void ReleaseAll()
        {
            ReleaseBuffers();
            ReleaseMesh();
        }

        void RunCompute(ComputeBuffer voxels, float target, float scale)
        {
            _counterBuffer.SetCounterValue(0);

            // 等値面再構築
            _compute.SetInts("Dims", _grids);
            _compute.SetInt("MaxTriangle", _triangleBudget);
            _compute.SetFloat("Scale", scale);
            _compute.SetFloat("Isovalue", target);
            _compute.SetBuffer(0, "TriangleTable", _triangleTable);
            _compute.SetBuffer(0, "Voxels", voxels);
            _compute.SetBuffer(0, "VertexBuffer", _vertexBuffer);
            _compute.SetBuffer(0, "IndexBuffer", _indexBuffer);
            _compute.SetBuffer(0, "Counter", _counterBuffer);
            _compute.DispatchThreads(0, _grids);

            // 未使用領域のクリア
            _compute.SetBuffer(1, "VertexBuffer", _vertexBuffer);
            _compute.SetBuffer(1, "IndexBuffer", _indexBuffer);
            _compute.SetBuffer(1, "Counter", _counterBuffer);
            _compute.DispatchThreads(1, 1024, 1, 1);

            // バウンディングボックス
            var ext = new Vector3(_grids.x, _grids.y, _grids.z) * scale;
            _mesh.bounds = new Bounds(Vector3.zero, ext);
        }

        #endregion

        #region Compute buffer objects

        ComputeBuffer _triangleTable;
        ComputeBuffer _counterBuffer;

        void AllocateBuffers()
        {
            // マーチングキューブ用トライアングルテーブル
            _triangleTable = new ComputeBuffer(256, sizeof(ulong));
            _triangleTable.SetData(PrecalculatedData.TriangleTable);

            // 三角形数カウント用バッファ
            _counterBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        }

        void ReleaseBuffers()
        {
            _triangleTable.Dispose();
            _counterBuffer.Dispose();
        }

        #endregion

        #region Mesh objects

        Mesh _mesh;
        GraphicsBuffer _vertexBuffer;
        GraphicsBuffer _indexBuffer;

        void AllocateMesh(int vertexCount)
        {
            _mesh = new Mesh();

            // GraphicsBuffer を Raw (ByteAddress) バッファとしてアクセス可能にする
            _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            // 頂点座標: float32 x 3
            var vp = new VertexAttributeDescriptor
              (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

            // 法線: float32 x 3
            var vn = new VertexAttributeDescriptor
              (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

            // 頂点/インデックスバッファフォーマット設定
            _mesh.SetVertexBufferParams(vertexCount, vp, vn);
            _mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);

            // サブメッシュ初期化
            _mesh.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount),
                             MeshUpdateFlags.DontRecalculateBounds);

            // GraphicsBuffer 参照取得
            _vertexBuffer = _mesh.GetVertexBuffer(0);
            _indexBuffer = _mesh.GetIndexBuffer();
        }

        void ReleaseMesh()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            Object.Destroy(_mesh);
        }

        #endregion
    }
}
