using UnityEngine;
using Unity.Collections;

public class VoxelMeshGenerator : MonoBehaviour
{
    [SerializeField] private int size = 32;
    private float[] voxel;
    [SerializeField] private MeshFilter meshFilter;

    void Start()
    {
        voxel = new float[size * size * size];
        FillVoxel(voxel, size); // 任意のSDFデータを生成

        Execute(voxel, size, out NativeArray<Vector3> vertices, out NativeArray<int> triangles);

        Mesh mesh = new();
        mesh.SetVertices(vertices);
        mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        vertices.Dispose();
        triangles.Dispose();
    }

    void FillVoxel(float[] voxel, int size)
    {
        var center = size / 2f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var dz = z - center;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                    voxel[x + y * size + z * size * size] = dist - center * 0.8f; // 球状SDF
                }
            }
        }
    }

    public void Execute(float[] voxel, int size, out NativeArray<Vector3> vertices, out NativeArray<int> triangles)
    {
        // 頂点位置->頂点番号を記憶する配列
        NativeArray<int> idxBuf = new(size * size * size, Allocator.Temp);
        // 頂点配列
        // サイズを余分に確保し，最後に不要な部分を切り落とす
        NativeArray<Vector3> vertexBuf = new(size * size * size, Allocator.Temp);
        // 三角面の配列
        // サイズを余分に確保し，最後に不要な部分を切り落とす
        NativeArray<int> triangleBuf = new(size * size * size * 18, Allocator.Temp);
        // 頂点の総数
        int vertexCount = 0;
        // 三角面の総数
        int triangleCount = 0;

        for (int x = 0; x < size - 1; x++)
        {
            for (int y = 0; y < size - 1; y++)
            {
                for (int z = 0; z < size - 1; z++)
                {
                    // ビットマスクで8つの点の状態を記憶
                    // iの位置の点が内側ならばi + 1番目のビットを立てる
                    // 頂点の位置と番号の対応は次のように決める        
                    //          7----6
                    //         /|   /|
                    //        4----5 |
                    //        | 3--|-2
                    //        |/   |/
                    // (x,y,z)0----1
                    int kind = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (0 > voxel[ToIdx(x, y, z, i, size)]) kind |= 1 << i;
                    }

                    // 8つの点がすべて内側またはすべて外側の場合はスキップ
                    if (kind == 0 || kind == 255) continue;

                    // 頂点の位置を算出
                    Vector3 vertex = Vector3.zero;
                    int crossCount = 0;

                    // 現在焦点を当てている立方体上の辺をすべて列挙
                    for (int i = 0; i < 12; i++)
                    {
                        int p0 = edgeTable[i][0];
                        int p1 = edgeTable[i][1];

                        // 異なる側同士の点でつながってない場合はスキップ
                        // ビットマスクからp0 + 1とp1 + 1ビット目(p0とp1の位置の点の状態)を取り出す
                        if ((kind >> p0 & 1) == (kind >> p1 & 1)) continue;

                        // 両端の点のボクセルデータ上の値を取り出す
                        float val0 = voxel[ToIdx(x, y, z, p0, size)];
                        float val1 = voxel[ToIdx(x, y, z, p1, size)];

                        // 線形補間によって値が0となる辺上の位置を算出して加算
                        vertex += Vector3.Lerp(ToVec(x, y, z, p0), ToVec(x, y, z, p1), (0 - val0) / (val1 - val0));
                        crossCount++;
                    }

                    vertex /= crossCount;

                    vertexBuf[vertexCount] = vertex;
                    idxBuf[ToIdx(x, y, z, 0, size)] = vertexCount;
                    vertexCount++;

                    // 面の追加は0 < x, y, z < size - 1で行う
                    if (x == 0 || y == 0 || z == 0) continue;

                    // ビットマスクから1ビット目(0の位置の点の状態)を取り出す
                    bool outside = (kind & 1) != 0;

                    // 面を構築する頂点を取り出す
                    // 頂点の位置と番号の対応は次のように決める   
                    //    1----0(x, y, z)
                    //   /|   /|
                    //  2----3 |
                    //  | 5--|-4
                    //  |/   |/
                    //  6----7
                    int v0 = idxBuf[ToIdxNeg(x, y, z, 0, size)];
                    int v1 = idxBuf[ToIdxNeg(x, y, z, 1, size)];
                    int v2 = idxBuf[ToIdxNeg(x, y, z, 2, size)];
                    int v3 = idxBuf[ToIdxNeg(x, y, z, 3, size)];
                    int v4 = idxBuf[ToIdxNeg(x, y, z, 4, size)];
                    int v5 = idxBuf[ToIdxNeg(x, y, z, 5, size)];
                    //int v6 = idxBuf[ToIdxNeg(x, y, z, 6, size)]; // 使われない
                    int v7 = idxBuf[ToIdxNeg(x, y, z, 7, size)];

                    // ビットマスクから2ビット目(1の位置の点の状態)を取り出す。異なる側同士の点からなる辺ならば交わるような面を追加
                    if ((kind >> 1 & 1) != 0 != outside)
                    {
                        triangleCount = MakeFace(triangleBuf, triangleCount, v0, v3, v7, v4, outside);
                    }
                    // ビットマスクから4ビット目(3の位置の点の状態)を取り出す
                    if ((kind >> 3 & 1) != 0 != outside)
                    {
                        triangleCount = MakeFace(triangleBuf, triangleCount, v0, v4, v5, v1, outside);
                    }
                    // ビットマスクから5ビット目(4の位置の点の状態)を取り出す
                    if ((kind >> 4 & 1) != 0 != outside)
                    {
                        triangleCount = MakeFace(triangleBuf, triangleCount, v0, v1, v2, v3, outside);
                    }
                }
            }
        }

        idxBuf.Dispose();

        vertices = vertexBuf.GetSubArray(0, vertexCount);
        triangles = triangleBuf.GetSubArray(0, triangleCount);
    }

    // v0, v1, v2, v3から構築される面を追加する
    static int MakeFace(NativeArray<int> triangleBuf, int triangleCount, int v0, int v1, int v2, int v3, bool outside)
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
    static int ToIdx(int x, int y, int z, int i, int size)
    {
        x += neighborTable[i][0];
        y += neighborTable[i][1];
        z += neighborTable[i][2];
        return x + y * size + z * size * size;
    }

    // 整数座標から配列に入るときの順序を取得
    // -X-Y-Z方向に広がる立方体上のi番目の頂点として順序を取得
    static int ToIdxNeg(int x, int y, int z, int i, int size)
    {
        x -= neighborTable[i][0];
        y -= neighborTable[i][1];
        z -= neighborTable[i][2];
        return x + y * size + z * size * size;
    }

    // 整数座標から実数座標を取得
    // +X+Y+Z方向に広がる立方体上のi番目の頂点として実数座標を取得
    static Vector3 ToVec(int i, int j, int k, int neighbor)
    {
        i += neighborTable[neighbor][0];
        j += neighborTable[neighbor][1];
        k += neighborTable[neighbor][2];
        return new Vector3(i, j, k);
    }

    // 立方体上の頂点の番号の決め方
    static readonly int[][] neighborTable = new int[][]
    {
        new int[] { 0, 0, 0 },
        new int[] { 1, 0, 0 },
        new int[] { 1, 0, 1 },
        new int[] { 0, 0, 1 },
        new int[] { 0, 1, 0 },
        new int[] { 1, 1, 0 },
        new int[] { 1, 1, 1 },
        new int[] { 0, 1, 1 },
    };

    // 辺のつながり方
    static readonly int[][] edgeTable = new int[][]
    {
        new int[] { 0, 1 },
        new int[] { 1, 2 },
        new int[] { 2, 3 },
        new int[] { 3, 0 },
        new int[] { 4, 5 },
        new int[] { 5, 6 },
        new int[] { 6, 7 },
        new int[] { 7, 4 },
        new int[] { 0, 4 },
        new int[] { 1, 5 },
        new int[] { 2, 6 },
        new int[] { 3, 7 },
    };
}
