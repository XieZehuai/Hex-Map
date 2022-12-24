using System.IO;
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
        [SerializeField] private HexUnit unitPrefab = default;

        private int chunkCountX;
        private int chunkCountZ;
        private HexCell[] cells;
        private HexGridChunk[] chunks;
        private List<HexUnit> units = new List<HexUnit>();

        private HexCellPriorityQueue searchFrontier;
        private int searchFrontierPhase;
        private HexCell currentPathFrom, currentPathTo;
        private bool currentPathExists;

        private HexCellShaderData cellShaderData;

        public int CellCountX => cellCountX;
        public int CellCountZ => cellCountZ;

        public bool HasPath => currentPathExists;

        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
            cellShaderData = gameObject.AddComponent<HexCellShaderData>();
            cellShaderData.Grid = this;

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
            cellShaderData.Initialize(cellCountX, cellCountZ);
            CreateChunks();
            CreateCells();

            return true;
        }

        private void ClearMap()
        {
            ClearPath();
            ClearUnits();

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
                HexUnit.unitPrefab = unitPrefab;
                ResetVisibility();
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
            cell.name = "Hex Cell " + cell.coordinates.ToString();
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.ShaderData = cellShaderData;
            cell.Index = i;

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

        public HexCell GetCell(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return GetCell(hit.point);
            }

            return null;
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

        public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            ClearPath();
            currentPathFrom = fromCell;
            currentPathTo = toCell;
            currentPathExists = SearchPath(fromCell, toCell, unit);
            ShowPath(unit.Speed);
        }

        // speed 表示单个回合可以移动的最大距离，因为了距离因素后，寻路时就不能只考虑最短距离，
        // 而是要同时考虑距离和回合数，有点像背包算法
        private bool SearchPath(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            int speed = unit.Speed;
            searchFrontierPhase += 2;

            if (searchFrontier == null)
            {
                searchFrontier = new HexCellPriorityQueue();
            }
            else
            {
                searchFrontier.Clear();
            }

            fromCell.EnableHighlight(Color.blue);
            fromCell.SearchPhase = searchFrontierPhase;
            fromCell.Distance = 0;
            searchFrontier.Enqueue(fromCell);

            while (searchFrontier.Count > 0)
            {
                HexCell current = searchFrontier.Dequeue();
                current.SearchPhase += 1;

                if (current == toCell)
                {
                    return true;
                }

                int currentTurn = (current.Distance - 1) / speed; // 移动到当前单元格需要的回合数

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = current.GetNeighbor(d);

                    if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase) continue;
                    if (neighbor.IsUnderWater || neighbor.Unit != null) continue;

                    if (!unit.IsValidDestination(neighbor)) continue;

                    int moveCost = unit.GetMoveCost(current, neighbor, d);
                    if (moveCost < 0) continue;

                    int distance = current.Distance + moveCost; // 从当前单元格移动到邻居单元格的实际距离
                    int turn = (distance - 1) / speed; // 需要的回合数
                    // 如果移动到邻居单元格需要到下一回合才能实现，就把移动到当前单元格后剩余
                    // 的移动点数加到移动到邻居单元格需要的距离上，以此降低当前路径的优先级
                    if (turn > currentTurn)
                    {
                        // 移动到当前单元格总的可用移动点数为 (currentTurn + 1) * speed，也就是 turn * speed，
                        // 而移动到当前单元格需要的移动点数为 current.Distance，所以剩余的移动点数就是 
                        // turn * speed - current.Distance，本来移动到邻居单元格的消耗为 distance = current.Distance + moveCost，
                        // 加上剩余点数后就变成了 distance = turn * speed + moveCost
                        distance = turn * speed + moveCost;
                    }

                    if (neighbor.SearchPhase < searchFrontierPhase)
                    {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);

                        searchFrontier.Enqueue(neighbor);
                    }
                    else if (distance < neighbor.Distance)
                    {
                        int oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        searchFrontier.Change(neighbor, oldPriority);
                    }
                }
            }

            return false;
        }

        private void ShowPath(int speed)
        {
            if (currentPathExists)
            {
                HexCell current = currentPathTo;
                while (current != currentPathFrom)
                {
                    int turn = (current.Distance - 1) / speed;
                    current.SetLabel(turn.ToString());
                    current.EnableHighlight(Color.white);
                    current = current.PathFrom;
                }
            }

            currentPathFrom.EnableHighlight(Color.blue);
            currentPathTo.EnableHighlight(Color.red);
        }

        public List<HexCell> GetPath()
        {
            if (!currentPathExists)
            {
                return null;
            }

            List<HexCell> path = ListPool<HexCell>.Get();
            for (HexCell cell = currentPathTo; cell != currentPathFrom; cell = cell.PathFrom)
            {
                path.Add(cell);
            }
            path.Add(currentPathFrom);
            path.Reverse();

            return path;
        }

        public void ClearPath()
        {
            if (currentPathExists)
            {
                HexCell current = currentPathTo;
                while (current != currentPathFrom)
                {
                    current.SetLabel(null);
                    current.DisableHighlight();
                    current = current.PathFrom;
                }

                current.DisableHighlight();
                currentPathExists = false;
            }
            else if (currentPathFrom != null)
            {
                currentPathFrom.DisableHighlight();
                currentPathTo.DisableHighlight();
            }

            currentPathFrom = currentPathTo = null;
        }

        public void IncreaseVisibility(HexCell fromCell, int range)
        {
            List<HexCell> cells = GetVisibleCells(fromCell, range);
            cells.ForEach(cell => cell.IncreaseVisibility());
            ListPool<HexCell>.Add(cells);
        }

        public void DecreaseVisibility(HexCell fromCell, int range)
        {
            List<HexCell> cells = GetVisibleCells(fromCell, range);
            cells.ForEach(cell => cell.DecreaseVisibility());
            ListPool<HexCell>.Add(cells);
        }

        private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
        {
            List<HexCell> visibleCells = ListPool<HexCell>.Get();

            searchFrontierPhase += 2;

            if (searchFrontier == null)
            {
                searchFrontier = new HexCellPriorityQueue();
            }
            else
            {
                searchFrontier.Clear();
            }

            range += fromCell.ViewElevation;
            fromCell.SearchPhase = searchFrontierPhase;
            fromCell.Distance = 0;
            searchFrontier.Enqueue(fromCell);

            HexCoordinates fromCoordinates = fromCell.coordinates;
            while (searchFrontier.Count > 0)
            {
                HexCell current = searchFrontier.Dequeue();
                current.SearchPhase += 1;
                visibleCells.Add(current);

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = current.GetNeighbor(d);

                    if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase) continue;

                    int distance = current.Distance + 1;
                    if (distance + neighbor.ViewElevation > range ||
                        distance > fromCoordinates.DistanceTo(neighbor.coordinates)) continue;

                    if (neighbor.SearchPhase < searchFrontierPhase)
                    {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = distance;
                        neighbor.SearchHeuristic = 0;

                        searchFrontier.Enqueue(neighbor);
                    }
                    else if (distance < neighbor.Distance)
                    {
                        int oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        searchFrontier.Change(neighbor, oldPriority);
                    }
                }
            }

            return visibleCells;
        }

        public void ResetVisibility()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].ResetVisibility();
            }
            for (int i = 0; i < units.Count; i++)
            {
                HexUnit unit = units[i];
                IncreaseVisibility(unit.Location, unit.VisionRange);
            }
        }

        public void AddUnit(HexUnit unit, HexCell location, float orientation)
        {
            units.Add(unit);
            unit.Grid = this;
            unit.transform.SetParent(transform, false);
            unit.Location = location;
            unit.Orientation = orientation;
        }

        public void RemoveUnit(HexUnit unit)
        {
            units.Remove(unit);
            unit.Die();
        }

        private void ClearUnits()
        {
            for (int i = 0; i < units.Count; i++)
            {
                units[i].Die();
            }

            units.Clear();
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

            writer.Write(units.Count);
            for (int i = 0; i < units.Count; i++)
            {
                units[i].Save(writer);
            }
        }

        public void Load(BinaryReader reader, int header)
        {
            ClearPath();
            ClearUnits();

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
            else
            {
                cellShaderData.Initialize(x, z);
            }

            bool originalImmediateMode = cellShaderData.ImmediateMode;
            cellShaderData.ImmediateMode = true;

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].Load(reader, header);
            }
            for (int i = 0; i < chunks.Length; i++)
            {
                chunks[i].Refresh();
            }

            if (header >= 2)
            {
                int unitCount = reader.ReadInt32();
                for (int i = 0; i < unitCount; i++)
                {
                    HexUnit.Load(reader, this);
                }
            }

            cellShaderData.ImmediateMode = originalImmediateMode;
        }
    }
}