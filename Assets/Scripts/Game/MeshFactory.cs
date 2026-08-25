using UnityEngine;

// 程序化生成网格（避免依赖内置资源名）
public static class MeshFactory
{
    // 单位四边形 1x1，UV 0..1
    public static Mesh Quad(){
        Mesh m = new Mesh();
        m.vertices = new []{ new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(0.5f,0.5f,0), new Vector3(-0.5f,0.5f,0) };
        m.uv = new []{ new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        m.triangles = new []{ 0,2,1, 0,3,2 };
        m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
    // 单位平面 1x1（XZ 平面），UV 0..1
    public static Mesh Plane(){
        Mesh m = new Mesh();
        m.vertices = new []{ new Vector3(-0.5f,0,-0.5f), new Vector3(0.5f,0,-0.5f), new Vector3(0.5f,0,0.5f), new Vector3(-0.5f,0,0.5f) };
        m.uv = new []{ new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        m.triangles = new []{ 0,2,1, 2,3,1 };
        m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
}
