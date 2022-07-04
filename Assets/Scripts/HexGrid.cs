using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HexMap
{
    /// <summary>
    /// 表示一个完整的六边形地图
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        [Tooltip("地图的宽度，大小必须是 HexMetrics.chunkSizeX（目前是 5）的倍数")]
        [SerializeField] private int cellCountX = 20;
        [Tooltip("地图的高度，大小必须是 HexMetrics.chunkSizeZ（目前是 5）的倍数")]
        [SerializeField] private int cellCountZ = 15;
        [SerializeField] private HexCell cellPrefab = default;
        [SerializeField] private TextMeshProUGUI cellLabelPrefab = default;
        [SerializeField] private HexGridChunk chunkPrefab = default;
        [SerializeField] private Texture2D noiseSource = default;
        [SerializeField] private int seed = 1234;

        private int chunkCountX;
        private int chunkCountZ;
        private HexCell[] cells;
        private HexGridChunk[] chunks;

        public int CellCountX => cellCountX;
        public int CellCountZ => cellCountZ;

        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);

            CreateMap(cellCountX, cellCountZ);
        }

        public bool CreateMap(int x, int z)
        {
            if (x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
                z <= 0 || z % HexMetrics.chunkSizeZ != 0)
            {
                Debug.LogError("不支持的地图大小");
                return false;
            }

            ClearMap();

            cellCountX = x;
            cellCountZ = z;
            chunkCountX = cellCountX / HexMetrics.chunkSizeX;
            chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
            CreateChunks();
            CreateCells();

            return true;
        }

        private void ClearMap()
        {
            if (chunks != null)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    Destroy(chunks[i].gameObject);
                }
            }
        }

        private void OnEnable()
        {
            if (HexMetrics.noiseSource == null)
            {
                HexMetrics.noiseSource = noiseSource;
                HexMetrics.InitializeHashGrid(seed);
            }
        }

        /// <summary>
        /// 生成所有的区块
        /// </summary>
        private void CreateChunks()
        {
            chunks = new HexGridChunk[chunkCountX * chunkCountZ];

            for (int z = 0, i = 0; z < chunkCountZ; z++)
            {
                for (int x = 0; x < chunkCountX; x++, i++)
                {
                    HexGridChunk chunk = chunks[i] = Instantiate(chunkPrefab, transform);
                }
            }
        }

        /// <summary>
        /// 生成单元格，按 cellCountX * cellCountZ 的大小生成一个 2 维的地图，再在 CreateCell 方法里
        /// 调整单元格的坐标，使其交错形成六边形地图
        /// </summary>
        private void CreateCells()
        {
            cells = new HexCell[cellCountZ * cellCountX];

            for (int z = 0, i = 0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {
                    CreateCell(x, z, i++);
                }
            }
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
            HexCell cell = cells[i] = Instantiate(cellPrefab);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
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
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                    if (x > 0)
                    {
                        cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                    }
                }
                else
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                    if (x < cellCountX - 1)
                    {
                        cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                    }
                }
            }

            // 显示单元格坐标UI
            TextMeshProUGUI label = Instantiate(cellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            cell.uiRect = label.rectTransform;

            cell.Elevation = 0;

            AddCellToChunk(x, z, cell);
        }

        private void AddCellToChunk(int x, int z, HexCell cell)
        {
            int chunkX = x / HexMetrics.chunkSizeX;
            int chunkZ = z / HexMetrics.chunkSizeZ;
            HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

            int localX = x - chunkX * HexMetrics.chunkSizeX;
            int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
            chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
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
            int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
            return cells[index];
        }

        public HexCell GetCell(HexCoordinates coordinates)
        {
            int z = coordinates.Z;
            if (z < 0 || z >= cellCountZ) return null;

            int x = coordinates.X + z / 2;
            if (x < 0 || x >= cellCountX) return null;

            return cells[x + z * cellCountX];
        }

        public void FindDistanceTo(HexCell cell)
        {
            StopAllCoroutines();
            StartCoroutine(SearchPath(cell));
        }

        private IEnumerator SearchPath(HexCell cell)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].Distance = int.MaxValue;
            }

            WaitForSeconds delay = new WaitForSeconds(1f / 60f);

            List<HexCell> frontier = new List<HexCell>();
            cell.Distance = 0;
            frontier.Add(cell);

            while (frontier.Count > 0)
            {
                yield return delay;

                HexCell current = frontier[0];
                frontier.RemoveAt(0);

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = current.GetNeighbor(d);

                    if (neighbor == null) continue;
                    if (neighbor.IsUnderWater) continue;

                    HexEdgeType edgeType = current.GetEdgeType(neighbor);
                    if (edgeType == HexEdgeType.Cliff) continue;

                    int distance = current.Distance;
                    if (current.HasRoadThroughEdge(d))
                    {
                        distance += 1;
                    }
                    // 当前单元格与相邻单元格之间被墙壁隔开且中间没有道路
                    else if (current.Walled != neighbor.Walled)
                    {
                        continue;
                    }
                    else
                    {
                        distance += edgeType == HexEdgeType.Flat ? 5 : 10;
                        distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                    }

                    if (neighbor.Distance == int.MaxValue)
                    {
                        neighbor.Distance = distance;
                        frontier.Add(neighbor);
                    }
                    else if (distance < neighbor.Distance)
                    {
                        neighbor.Distance = distance;
                    }

                    frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
                }
            }
        }

        public void ShowUI(bool visible)
        {
            foreach (var chunk in chunks)
            {
                chunk.ShowUI(visible);
            }
        }

        public void RefreshAllChunks()
        {
            foreach (var chunk in chunks)
            {
                chunk.Refresh();
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(cellCountX);
            writer.Write(cellCountZ);

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].Save(writer);
            }
        }

        public void Load(BinaryReader reader, int header)
        {
            StopAllCoroutines();

            int x = 15, z = 15;
            if (header >= 1)
            {
                x = reader.ReadInt32();
                z = reader.ReadInt32();
            }

            if (x != cellCountX || z != cellCountZ)
            {
                if (!CreateMap(x, z))
                {
                    return;
                }
            }

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].Load(reader);
            }
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].Refresh();
            }
        }
    }
}