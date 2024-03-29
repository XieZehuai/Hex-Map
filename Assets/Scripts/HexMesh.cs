﻿using System;
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
        [SerializeField] private bool useCellData = false;
        [SerializeField] private bool useUV = false;
        [SerializeField] private bool useUV2 = false;

        [SerializeField] private bool drawWireFrame = false;

        private Mesh hexMesh;
        private MeshCollider meshCollider;

        [NonSerialized] private List<Vector3> vertices;

        /// <summary>
        /// <para>
        /// 三角形对应地形的索引，一个三角形可以拥有 1 - 3 种地形，三个顶点用相同的值，实际每种地形
        /// 占比通过三个 cellWeight 值设置
        /// </para>
        /// 
        /// 当顶点在单元格内时，对应一种地形，xyz 分量使用相同的值；当顶点在单元格边缘上时，
        /// 当顶点在单元格边缘上时，被两个单元格共享，对应两种地形，xz 分量为第一个单元格的
        /// 地形，y 分量为第二个单元格的地形；当顶点在三个单元格之间的连接区域时，对应三种地
        /// 形，x 分量为第一个单元格的地形，y 分量为第二个单元格的地形，z 分量为第三个单元格的地形。
        /// </summary>
        [NonSerialized] private List<Vector3> cellIndices;
        /// <summary>
        /// 顶点对应每种地形的比例，r 对应第一种， g 对应第二种, b 对应第三种，实际地形为三种地形的线性插值结果
        /// </summary>
        [NonSerialized] private List<Color> cellWeights;

        [NonSerialized] private List<Vector2> uvs;
        [NonSerialized] private List<Vector2> uv2s;
        [NonSerialized] private List<int> triangles;

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

            if (useCellData)
            {
                cellWeights = ListPool<Color>.Get();
                cellIndices = ListPool<Vector3>.Get();
            }
            if (useUV)
            {
                uvs = ListPool<Vector2>.Get();
            }
            if (useUV2)
            {
                uv2s = ListPool<Vector2>.Get();
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

            if (useCellData)
            {
                hexMesh.SetColors(cellWeights);
                ListPool<Color>.Add(cellWeights);
                hexMesh.SetUVs(2, cellIndices);
                ListPool<Vector3>.Add(cellIndices);
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

        public void AddTriangleCellData(Vector3 indices, Color weights)
        {
            AddTriangleCellData(indices, weights, weights, weights);
        }

        public void AddTriangleCellData(Vector3 indices, Color weights1, Color weights2, Color weights3)
        {
            cellIndices.Add(indices);
            cellIndices.Add(indices);
            cellIndices.Add(indices);

            cellWeights.Add(weights1);
            cellWeights.Add(weights2);
            cellWeights.Add(weights3);
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

        public void AddQuadCellData(Vector3 indices, Color weights)
        {
            AddQuadCellData(indices, weights, weights, weights, weights);
        }

        public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2)
        {
            AddQuadCellData(indices, weights1, weights1, weights2, weights2);
        }

        public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4)
        {
            cellIndices.Add(indices);
            cellIndices.Add(indices);
            cellIndices.Add(indices);
            cellIndices.Add(indices);

            cellWeights.Add(weights1);
            cellWeights.Add(weights2);
            cellWeights.Add(weights3);
            cellWeights.Add(weights4);
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

        private void OnDrawGizmos()
        {
            if (drawWireFrame && hexMesh != null && hexMesh.vertexCount > 0)
            {
                Gizmos.DrawWireMesh(hexMesh, 0);
            }
        }
    }
}
