using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using MRSculpture.Job;

namespace MRSculpture
{
    public class VoxelMeshGenerator : MonoBehaviour
    {
        [SerializeField] private int3 _boundsSize = new(100, 100, 100);
        private NativeArray<float> voxel;
        [SerializeField] private MeshFilter meshFilter;
        private Mesh mesh;
        private NativeArray<Vector3> vertices;
        private NativeArray<int> triangles;

        private void Start()
        {
            voxel = new NativeArray<float>(_boundsSize.x * _boundsSize.y * _boundsSize.z, Allocator.Persistent);
            FillVoxel(ref voxel, in _boundsSize);

            mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            // 頂点バッファサイズ
            int maxVertexBufferSize = _boundsSize.x * _boundsSize.y * _boundsSize.z;
            // 三角面バッファサイズ
            int maxTriangleBufferSize = _boundsSize.x * _boundsSize.y * _boundsSize.z * 18;

            // 頂点位置->頂点番号を記憶する配列
            NativeArray<int> indexBuffer = new(maxVertexBufferSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            // 頂点配列
            NativeArray<Vector3> vertexBuffer = new(maxVertexBufferSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            // 三角面の配列
            NativeArray<int> triangleBuffer = new(maxTriangleBufferSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            // 頂点の総数
            int vertexCount = 0;
            // 三角面の総数
            int triangleCount = 0;

            for (int y = 0; y < _boundsSize.y - 1; y++)
            {
                ExecuteLayer(in voxel, in _boundsSize, y, ref indexBuffer, ref vertexBuffer, ref triangleBuffer, ref vertexCount, ref triangleCount);
            }

            vertices = vertexBuffer.GetSubArray(0, vertexCount);
            triangles = triangleBuffer.GetSubArray(0, triangleCount);

            mesh.SetVertices(vertices);
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        void FillVoxel(ref NativeArray<float> voxel, in int3 size)
        {
            float centerX = size.x / 2f;
            float centerY = size.y / 2f;
            float centerZ = size.z / 2f;
            float rx = centerX * 0.9f;
            float ry = centerY * 0.9f;
            float rz = centerZ * 0.9f;
            float halfCube = Mathf.Min(centerX, Mathf.Min(centerY, centerZ)) * 0.9f;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        if (Mathf.Abs(x - y) < 7.0f || Mathf.Abs((size.x - x) - y) < 7.0f)
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
                            voxel[x + z * size.x + y * size.x * size.z] = dist;
                        }
                        else
                        {
                            voxel[x + z * size.x + y * size.x * size.z] = 1.0f;
                        }
                    }
                }
            }
        }

        public void ExecuteLayer(
            in NativeArray<float> voxel,
            in int3 size,
            int y,
            ref NativeArray<int> indexBuffer,
            ref NativeArray<Vector3> vertexBuffer,
            ref NativeArray<int> triangleBuffer,
            ref int vertexCount,
            ref int triangleCount)
        {
            for (int x = 0; x < size.x - 1; x++)
            {
                for (int z = 0; z < size.z - 1; z++)
                {
                    //if (!MakeVertex(in voxel, in size, x, y, z, ref indexBuffer, ref vertexBuffer, ref vertexCount, out int kind)) continue;

                    //MakeSurface(x, y, z, in size, kind, in indexBuffer, ref triangleBuffer, ref triangleCount);

                    JobHandle handle = new NaiveSurfaceNetJob()
                    {
                        voxel = voxel,
                        size = size,
                        indexBuffer = indexBuffer,
                        vertexBuffer = vertexBuffer,
                        triangleBuffer = triangleBuffer,
                        yLayer = y,
                        maxVertexBufferSize = indexBuffer.Length,
                        maxTriangleBufferSize = triangleBuffer.Length,
                        vertexCount = vertexCount,
                        triangleCount = triangleCount
                    }.Schedule();
                    handle.Complete();
                }
            }
        }

        // 立方体セルのビットマスク(kind)と頂点位置(vertex)を計算する関数
        private static bool MakeVertex(
            in NativeArray<float> voxel,
            in int3 size,
            int x, int y, int z,
            ref NativeArray<int> indexBuffer,
            ref NativeArray<Vector3> vertexBuffer,
            ref int vertexCount,
            out int kind)
        {
            kind = 0b0000000;
            // ビットマスクで8つの点の状態を記憶
            // iの位置の点が内側ならばi + 1番目のビットを立てる
            for (int i = 0; i < 8; i++)
            {
                if (0 > voxel[ToIndexPositive(x, y, z, i, in size)]) kind |= 1 << i;
            }

            // 8つの点がすべて外側(00000000)またはすべて内側(11111111)の場合はスキップ
            if (kind == 0b00000000 || kind == 0b11111111)
            {
                return false;
            }

            // 頂点位置の計算
            Vector3 vertex = Vector3.zero;
            int crossCount = 0;
            // 現在焦点を当てている立方体上の辺をすべて列挙
            for (int i = 0; i < 12; i++)
            {
                int startVertex = edgeTable[i][0];
                int endVertex = edgeTable[i][1];

                // 両端が外側(0)もしくは内側(1)の場合はスキップ
                // ビットマスクからstartVertex + 1とendVertex + 1ビット目(startVertexとendVertexの位置の点の状態)を取り出す
                //        | 1            | 1
                // -------|---  ==  -----|---
                // start 0| 0       end 0| 0
                //       1| 1           1| 1
                if ((kind >> startVertex & 1) == (kind >> endVertex & 1)) continue;

                // 両端の点のボクセルデータ上の値を取り出す
                float startValue = voxel[ToIndexPositive(x, y, z, startVertex, in size)];
                float endValue = voxel[ToIndexPositive(x, y, z, endVertex, in size)];

                // 線形補間によって値が0となる辺上の位置を算出して加算
                Vector3 startVector = ToVector(x, y, z, startVertex);
                Vector3 endVector = ToVector(x, y, z, endVertex);
                vertex += Vector3.Lerp(startVector, endVector, (0 - startValue) / (endValue - startValue));
                crossCount++;
            }

            vertex /= crossCount;

            vertexBuffer[vertexCount] = vertex;
            indexBuffer[ToIndexPositive(x, y, z, 0, in size)] = vertexCount;
            vertexCount++;

            return true;
        }

        // 面の追加処理を関数として分離
        private static void MakeSurface(
            int x, int y, int z,
            in int3 size,
            int kind,
            in NativeArray<int> indexBuffer,
            ref NativeArray<int> triangleBuffer,
            ref int triangleCount)
        {
            // 面の追加は0 < x, y, z < size - 1で行う
            if (x == 0 || y == 0 || z == 0) return;

            // ビットマスクから1ビット目(0の位置の点の状態)を取り出す
            bool outside = (kind & 1) != 0;

            // 面を構築する頂点を取り出す
            // v6は対角線状の頂点なので面張りしない
            int v0 = indexBuffer[ToIndexNegative(x, y, z, 0, in size)];
            int v1 = indexBuffer[ToIndexNegative(x, y, z, 1, in size)];
            int v2 = indexBuffer[ToIndexNegative(x, y, z, 2, in size)];
            int v3 = indexBuffer[ToIndexNegative(x, y, z, 3, in size)];
            int v4 = indexBuffer[ToIndexNegative(x, y, z, 4, in size)];
            int v5 = indexBuffer[ToIndexNegative(x, y, z, 5, in size)];
            int v7 = indexBuffer[ToIndexNegative(x, y, z, 7, in size)];

            // ビットマスクから2ビット目(1の位置の点の状態)を取り出す
            if ((kind >> 1 & 1) != 0 != outside)
            {
                triangleCount = MakeFace(ref triangleBuffer, triangleCount, v0, v3, v7, v4, outside);
            }
            // ビットマスクから4ビット目(3の位置の点の状態)を取り出す
            if ((kind >> 3 & 1) != 0 != outside)
            {
                triangleCount = MakeFace(ref triangleBuffer, triangleCount, v0, v4, v5, v1, outside);
            }
            // ビットマスクから5ビット目(4の位置の点の状態)を取り出す
            if ((kind >> 4 & 1) != 0 != outside)
            {
                triangleCount = MakeFace(ref triangleBuffer, triangleCount, v0, v1, v2, v3, outside);
            }
        }

        // v0, v1, v2, v3から構築される面を追加する
        static int MakeFace(
            ref NativeArray<int> triangleBuf,
            int triangleCount,
            int v0, int v1, int v2, int v3, bool outside)
        {
            if (outside)
            {
                triangleBuf[triangleCount++] = v0;
                triangleBuf[triangleCount++] = v3;
                triangleBuf[triangleCount++] = v2;
                triangleBuf[triangleCount++] = v2;
                triangleBuf[triangleCount++] = v1;
                triangleBuf[triangleCount++] = v0;
            }
            else
            {
                triangleBuf[triangleCount++] = v0;
                triangleBuf[triangleCount++] = v1;
                triangleBuf[triangleCount++] = v2;
                triangleBuf[triangleCount++] = v2;
                triangleBuf[triangleCount++] = v3;
                triangleBuf[triangleCount++] = v0;
            }
            return triangleCount;
        }

        // 整数座標から配列に入るときの順序を取得
        // +X+Y+Z方向に広がる立方体上のi番目の頂点として順序を取得
        static int ToIndexPositive(int x, int y, int z, int i, in int3 size)
        {
            x += neighborTable[i][0];
            y += neighborTable[i][1];
            z += neighborTable[i][2];
            return x + (z * size.x) + (y * size.x * size.z);
        }

        // 整数座標から配列に入るときの順序を取得
        // -X-Y-Z方向に広がる立方体上のi番目の頂点として順序を取得
        static int ToIndexNegative(int x, int y, int z, int i, in int3 size)
        {
            x -= neighborTable[i][0];
            y -= neighborTable[i][1];
            z -= neighborTable[i][2];
            return x + (z * size.x) + (y * size.x * size.z);
        }

        // 整数座標から実数座標を取得
        // +X+Y+Z方向に広がる立方体上のi番目の頂点として実数座標を取得
        static Vector3 ToVector(int i, int j, int k, int neighbor)
        {
            i += neighborTable[neighbor][0];
            j += neighborTable[neighbor][1];
            k += neighborTable[neighbor][2];
            return new Vector3(i, j, k);
        }

        // 立方体上の頂点の番号の決め方
        static readonly int[][] neighborTable = new int[][]
        {
        //    7----------6
        //   /|         /|
        //  / |        / |
        // 4----------5  |
        // |  |       |  |
        // |  |       |  |
        // |  3-------|--2
        // | /        | /
        // |/         |/
        // 0----------1
        new int[] { 0, 0, 0 }, // 0 原点
        new int[] { 1, 0, 0 }, // 1 +X方向
        new int[] { 1, 0, 1 }, // 2 +X+Z方向
        new int[] { 0, 0, 1 }, // 3 +Z方向
        new int[] { 0, 1, 0 }, // 4 +Y方向
        new int[] { 1, 1, 0 }, // 5 +X+Y方向
        new int[] { 1, 1, 1 }, // 6 +X+Y+Z方向
        new int[] { 0, 1, 1 }, // 7 +Y+Z方向
        };

        // 辺のつながり方
        static readonly int[][] edgeTable = new int[][]
        {
        //     ●←----0----●
        //    1|         ↗|
        //   ↙ |        3 |
        //  ●----2----→●  |
        //  |  9       |  8
        //  |  ↓       |  ↓
        //  |  ○←----4-|--●
        // 10 5       11 ↗
        //  ↓↙         ↓7
        //  ●----6----→●
        new int[] { 0, 1 }, // 0 
        new int[] { 1, 2 }, // 1 
        new int[] { 2, 3 }, // 2 
        new int[] { 3, 0 }, // 3 
        new int[] { 4, 5 }, // 4 
        new int[] { 5, 6 }, // 5 
        new int[] { 6, 7 }, // 6 
        new int[] { 7, 4 }, // 7 
        new int[] { 0, 4 }, // 8 
        new int[] { 1, 5 }, // 9 
        new int[] { 2, 6 }, // 10
        new int[] { 3, 7 }, // 11
        };

        private void OnDestroy()
        {
            vertices.Dispose();
            triangles.Dispose();
            voxel.Dispose();
        }
    }
}
