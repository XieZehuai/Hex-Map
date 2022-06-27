using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 表示一个由一系列六边形组成的网格
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        [SerializeField] private bool useCollider = false;
        [SerializeField] private bool useColor = false;
        [SerializeField] private bool useUV = false;
        [SerializeField] private bool useUV2 = false;
        [SerializeField] private bool useTerrainType = false;
        [SerializeField] private bool drawWireFrame = false;

        private Mesh hexMesh;
        private MeshCollider meshCollider;

        [NonSerialized] private List<Vector3> vertices;
        [NonSerialized] private List<Color> colors;
        [NonSerialized] private List<Vector2> uvs;
        [NonSerialized] private List<Vector2> uv2s;
        [NonSerialized] private List<int> triangles;
        [NonSerialized] private List<Vector3> terrainTypes;

        private void Awake()
        {
            if (useCollider)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
            hexMesh.name = "Hex Mesh";
        }

        public void Clear()
        {
            hexMesh.Clear();

            vertices = ListPool<Vector3>.Get();
            triangles = ListPool<int>.Get();

            if (useColor)
            {
                colors = ListPool<Color>.Get();
            }
            if (useUV)
            {
                uvs = ListPool<Vector2>.Get();
            }
            if (useUV2)
            {
                uv2s = ListPool<Vector2>.Get();
            }
            if (useTerrainType)
            {
                terrainTypes = ListPool<Vector3>.Get();
            }
        }

        /// <summary>
        /// 生成网格
        /// </summary>
        public void Apply()
        {
            hexMesh.SetVertices(vertices);
            hexMesh.SetTriangles(triangles, 0);
            ListPool<Vector3>.Add(vertices);
            ListPool<int>.Add(triangles);

            if (useColor)
            {
                hexMesh.SetColors(colors);
                ListPool<Color>.Add(colors);
            }
            if (useUV)
            {
                hexMesh.SetUVs(0, uvs);
                ListPool<Vector2>.Add(uvs);
            }
            if (useUV2)
            {
                hexMesh.SetUVs(1, uv2s);
                ListPool<Vector2>.Add(uv2s);
            }
            if (useTerrainType)
            {
                hexMesh.SetUVs(2, terrainTypes);
                ListPool<Vector3>.Add(terrainTypes);
            }

            hexMesh.RecalculateNormals();
            if (useCollider)
            {
                meshCollider.sharedMesh = hexMesh;
            }
        }

        /// <summary>
        /// 使用给定的三个顶点生成一个三角形，顶点顺序遵循左手定则
        /// </summary>
        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(HexMetrics.Perturb(v1));
            vertices.Add(HexMetrics.Perturb(v2));
            vertices.Add(HexMetrics.Perturb(v3));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
        }

        /// <summary>
        /// 为当前三角形添加三个顶点的颜色，顶点顺序遵循左手定则
        /// </summary>
        public void AddTriangleColor(Color c1, Color c2, Color c3)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
        }

        /// <summary>
        /// 为当前三角形添加三个顶点的颜色
        /// </summary>
        public void AddTriangleColor(Color c1)
        {
            colors.Add(c1);
            colors.Add(c1);
            colors.Add(c1);
        }

        public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
        }

        public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            uv2s.Add(uv1);
            uv2s.Add(uv2);
            uv2s.Add(uv3);
        }

        public void AddTriangleTerrainTypes(Vector3 types)
        {
            terrainTypes.Add(types);
            terrainTypes.Add(types);
            terrainTypes.Add(types);
        }

        /// <summary>
        /// 使用给定的四个顶点生成一个四边形，四边形由两个三角形组成，顶点顺序分别为
        /// [v1, v3, v2] 和 [v2, v3, v4]，顶点顺序遵循左手定则
        /// </summary>
        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(HexMetrics.Perturb(v1));
            vertices.Add(HexMetrics.Perturb(v2));
            vertices.Add(HexMetrics.Perturb(v3));
            vertices.Add(HexMetrics.Perturb(v4));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
        }

        public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            int vertexIndex = vertices.Count;

            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
        }

        /// <summary>
        /// 为当前四边形添加顶点的颜色，四边形由两个三角形组成，颜色顺序分别为
        /// [c1, c3, c2] 和 [c2, c3, c4]，顶点顺序遵循左手定则
        /// </summary>
        public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
        {
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
            colors.Add(c4);
        }

        /// <summary>
        /// 为当前四边形添加顶点的颜色，四边形由两个三角形组成，颜色顺序分别为
        /// [c1, c2, c1] 和 [c1, c2, c2]，顶点顺序遵循左手定则
        /// </summary>
        public void AddQuadColor(Color c1, Color c2)
        {
            colors.Add(c1);
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c2);
        }

        public void AddQuadColor(Color color)
        {
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }

        public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            uvs.Add(uv1);
            uvs.Add(uv2);
            uvs.Add(uv3);
            uvs.Add(uv4);
        }

        public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
        {
            uvs.Add(new Vector2(uMin, vMin));
            uvs.Add(new Vector2(uMax, vMin));
            uvs.Add(new Vector2(uMin, vMax));
            uvs.Add(new Vector2(uMax, vMax));
        }

        public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            uv2s.Add(uv1);
            uv2s.Add(uv2);
            uv2s.Add(uv3);
            uv2s.Add(uv4);
        }

        public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
        {
            uv2s.Add(new Vector2(uMin, vMin));
            uv2s.Add(new Vector2(uMax, vMin));
            uv2s.Add(new Vector2(uMin, vMax));
            uv2s.Add(new Vector2(uMax, vMax));
        }

        public void AddQuadTerrainTypes(Vector3 types)
        {
            terrainTypes.Add(types);
            terrainTypes.Add(types);
            terrainTypes.Add(types);
            terrainTypes.Add(types);
        }

        private void OnDrawGizmos()
        {
            if (drawWireFrame && hexMesh != null && hexMesh.vertexCount > 0)
            {
                Gizmos.DrawWireMesh(hexMesh, 0);
            }
        }
    }
}
