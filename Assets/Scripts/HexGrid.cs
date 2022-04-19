﻿using UnityEngine;
using UnityEngine.UI;

namespace HexMap
{
    /// <summary>
    /// 表示由六边形单元格组成的地图
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        [SerializeField] private int width = 6;
        [SerializeField] private int height = 6;
        [SerializeField] private HexCell cellPrefab = default;
        [SerializeField] private Text cellLabelPrefab = default;
        [SerializeField] private Color defaultColor = Color.white; // 单元格的默认颜色
        [SerializeField] private bool showCellLabel = true;
        [SerializeField] private Texture2D noiseSource = default;

        private HexCell[] cells;
        private Canvas gridCanvas;
        private HexMesh hexMesh;

        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;
            gridCanvas = GetComponentInChildren<Canvas>();
            hexMesh = GetComponentInChildren<HexMesh>();

            // 生成单元格，按 width * height 的大小生成一个 2 维的地图，再在 CreateCell 方法里
            // 调整单元格的坐标，使其交错形成六边形地图
            cells = new HexCell[height * width];
            for (int z = 0, i = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    CreateCell(x, z, i++);
                }
            }
        }

        private void OnEnable()
        {
            HexMetrics.noiseSource = noiseSource;
        }

        private void Start()
        {
            // 生成网格，因为需要等所有单元格生成完后才能生成网格，所以放在Start里执行
            hexMesh.Triangulate(cells);
        }

        /// <summary>
        /// 创建一个新的单元格
        /// </summary>
        /// <param name="x">X轴坐标，范围从 0 到 width - 1</param>
        /// <param name="z">Z轴坐标，范围从 0 到 height - 1</param>
        /// <param name="i">当前单元格在cells数组中的索引</param>
        private void CreateCell(int x, int z, int i)
        {
            // 计算单元格的位置
            Vector3 position;
            position.x = (x + (z % 2 == 0 ? 0f : 0.5f)) * (HexMetrics.innerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.outerRadius * 1.5f);

            // 创建单元格
            HexCell cell = cells[i] = Instantiate(cellPrefab, transform);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.color = defaultColor;
            cell.name = "Hex Cell " + cell.coordinates.ToString();

            // 设置单元格对应的邻居关系
            if (x > 0)
            {
                cell.SetNeighbor(HexDirection.W, cells[i - 1]);
            }
            if (z > 0)
            {
                if (z % 2 == 0)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                    if (x > 0)
                    {
                        cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                    }
                }
                else
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                    if (x < width - 1)
                    {
                        cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                    }
                }
            }

            // 现实单元格坐标UI
            Text label = Instantiate(cellLabelPrefab, gridCanvas.transform);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.coordinates.ToStringOnSeparateLines();
            cell.uiRect = label.rectTransform;
            label.enabled = showCellLabel;

            cell.Elevation = 0;
        }

        /// <summary>
        /// 获取目标位置对应的单元格
        /// </summary>
        /// <param name="position">世界空间下坐标，坐标范围须在地图网格内</param>
        public HexCell GetCell(Vector3 position)
        {
            // 为避免当前物体位移对于点击位置坐标的影响，把点击坐标转换到以当前物体
            // 为原点的坐标空间下再计算六角坐标
            position = transform.InverseTransformPoint(position);
            HexCoordinates coordinates = HexCoordinates.FromPosition(position);
            int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
            return cells[index];
        }

        /// <summary>
        /// 刷新地图
        /// </summary>
        public void Refresh()
        {
            hexMesh.Triangulate(cells);
        }
    }
}