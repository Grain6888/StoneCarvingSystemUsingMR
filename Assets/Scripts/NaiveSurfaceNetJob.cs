using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MRSculpture.Job
{
    [BurstCompile]
    public struct NaiveSurfaceNetJob : IJob
    {
        [ReadOnly] public NativeArray<float> voxel;
        [ReadOnly] public int3 size;

        // 頂点位置->頂点番号を記憶する配列
        //[NativeDisableParallelForRestriction]
        public NativeArray<int> indexBuffer;
        // 頂点配列
        //[NativeDisableParallelForRestriction]
        public NativeArray<Vector3> vertexBuffer;
        // 三角面の配列
        //[NativeDisableParallelForRestriction]
        public NativeArray<int> triangleBuffer;

        // 現在のyレイヤ
        [ReadOnly] public int yLayer;
        // 頂点バッファサイズ
        public int maxVertexBufferSize;
        // 三角面バッファサイズ
        public int maxTriangleBufferSize;

        // 頂点の総数
        public int vertexCount;
        // 三角面の総数
        public int triangleCount;

        public void Execute()
        {
            for (int x = 0; x < size.x - 1; x++)
            {
                for (int z = 0; z < size.z - 1; z++)
                {
                    if (!MakeVertex(in voxel, in size, x, yLayer, z, ref indexBuffer, ref vertexBuffer, ref vertexCount, out int kind))
                        continue;

                    MakeSurface(x, yLayer, z, in size, kind, in indexBuffer, ref triangleBuffer, ref triangleCount);
                }
            }
            Debug.Log("vertexCount: " + vertexCount);
            Debug.Log("triangleCount: " + triangleCount);
        }

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
                MakeFace(ref triangleBuffer, triangleCount, v0, v3, v7, v4, outside);
            }
            // ビットマスクから4ビット目(3の位置の点の状態)を取り出す
            if ((kind >> 3 & 1) != 0 != outside)
            {
                MakeFace(ref triangleBuffer, triangleCount, v0, v4, v5, v1, outside);
            }
            // ビットマスクから5ビット目(4の位置の点の状態)を取り出す
            if ((kind >> 4 & 1) != 0 != outside)
            {
                MakeFace(ref triangleBuffer, triangleCount, v0, v1, v2, v3, outside);
            }
        }

        // v0, v1, v2, v3から構築される面を追加する
        static void MakeFace(
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
    }
}
