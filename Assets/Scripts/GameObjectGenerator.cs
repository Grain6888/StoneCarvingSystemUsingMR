using UnityEngine;

public class GameObjectGenerator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject cube = new GameObject("CustomCube");

        MeshFilter mf = cube.AddComponent<MeshFilter>();
        MeshRenderer mr = cube.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();

        // 頂点を定義（1x1x1の立方体）
        Vector3[] vertices = {
            new Vector3(0, 0, 0), // 0
            new Vector3(1, 0, 0), // 1
            new Vector3(1, 1, 0), // 2
            new Vector3(0, 1, 0), // 3
            new Vector3(0, 0, 1), // 4
            new Vector3(1, 0, 1), // 5
            new Vector3(1, 1, 1), // 6
            new Vector3(0, 1, 1)  // 7
        };

        // 三角形を定義（各面を2つの三角形に分割）
        int[] triangles = {
            // 前面
            0, 2, 1, 0, 3, 2,
            // 背面
            5, 6, 4, 4, 6, 7,
            // 上面
            2, 3, 6, 3, 7, 6,
            // 下面
            0, 1, 4, 1, 5, 4,
            // 右面
            1, 2, 5, 2, 6, 5,
            // 左面
            0, 4, 3, 3, 4, 7
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        mf.mesh = mesh;
        mr.material = new Material(Shader.Find("Standard"));
    }

    // Update is called once per frame
    void Update()
    {

    }
}
