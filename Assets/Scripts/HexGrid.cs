using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HexMap
{
    /// <summary>
    /// 表示一个完整的六边形地图
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        [SerializeField] private int chunkCountX = 4;
        [SerializeField] private int chunkCountZ = 3;
        [SerializeField] private HexCell cellPrefab = default;
        [SerializeField] private Text cellLabelPrefab = default;
        [SerializeField] private HexGridChunk chunkPrefab = default;
        [SerializeField] private Texture2D noiseSource = default;
        [SerializeField] private int seed = 1234;
        [SerializeField] private Color[] colors = default;

        private int cellCountX;
        private int cellCountZ;
        private HexCell[] cells;
        private HexGridChunk[] chunks;

        public int ChunkCountX => chunkCountX;
        public int ChunkCountZ => chunkCountZ;

        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexMetrics.colors = colors;

            cellCountX = chunkCountX * HexMetrics.chunkSizeX;
            cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

            CreateChunks();
            CreateCells();
        }

        private void OnEnable()
        {
            if (HexMetrics.noiseSource == null)
            {
                HexMetrics.noiseSource = noiseSource;
                HexMetrics.InitializeHashGrid(seed);
                HexMetrics.colors = colors;
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

            // 现实单元格坐标UI
            Text label = Instantiate(cellLabelPrefab);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.coordinates.ToStringOnSeparateLines();
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
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].Save(writer);
            }
        }

        public void Load(BinaryReader reader)
        {
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