using UnityEngine;
using UnityEngine.EventSystems;

namespace HexMap.UI
{
    /// <summary>
    /// 六边形地图编辑器
    /// </summary>
    public class HexMapEditor : MonoBehaviour
    {
        public HexGrid hexGrid;
        public Material terrainMaterial;

        #region 地形编辑选项
        private int activeTerrainTypeIndex = -1;
        private bool applyElevation;
        private int activeElevation; // 当前选中的海拔高度

        private bool applyWaterLevel;
        private int activeWaterLevel;

        private OptionalToggle riverMode; // 河流的编辑模式
        private OptionalToggle roadMode; // 道路的编辑模式
        private OptionalToggle walledMode; // 墙壁的编辑模式
        #endregion

        #region 单元格细节编辑选项
        private bool applyUrbanLevel;
        private int activeUrbanLevel;

        private bool applyFarmLevel;
        private int activeFarmLevel;

        private bool applyPlantLevel;
        private int activePlantLevel;

        private bool applySpecialIndex;
        private int activeSpecialIndex;
        #endregion

        private int brushSize; // 笔刷大小，覆盖范围为 2 * brushSize + 1
        // 用于检测鼠标拖动输入
        private bool isDrag;
        private bool isDragOnUI; // 是否在 UI 上拖动鼠标，防止在编辑面板上调整参数时鼠标划到地图上
        private HexDirection dragDirection;
        private HexCell previousCell;

        private void Awake()
        {
            ShowGrid(false);
            SetEditMode(false);
        }

        private void Update()
        {
            bool isPointOverUI = EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && isPointOverUI)
            {
                isDragOnUI = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                isDragOnUI = false;
            }

            // 当点击鼠标左键并且鼠标不处于UI上时处理点击操作
            if (!isPointOverUI && !isDragOnUI)
            {
                if (Input.GetMouseButton(0))
                {
                    HandleInput();
                    return;
                }

                if (Input.GetKeyDown(KeyCode.U))
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        DestroyUnit();
                    }
                    else
                    {
                        CreateUnit();
                    }
                    return;
                }
            }

            previousCell = null;
        }

        private void HandleInput()
        {
            HexCell currentCell = GetCellUnderCursor();
            if (currentCell != null)
            {
                if (previousCell != null && previousCell != currentCell)
                {
                    ValidateDrag(currentCell);
                }
                else
                {
                    isDrag = false;
                }

                EditCells(currentCell);

                previousCell = currentCell;
            }
            else
            {
                previousCell = null;
            }
        }

        private HexCell GetCellUnderCursor()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return hexGrid.GetCell(ray);
        }

        private void ValidateDrag(HexCell currentCell)
        {
            for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
            {
                if (previousCell.GetNeighbor(dragDirection) == currentCell)
                {
                    isDrag = true;
                    return;
                }
            }

            isDrag = false;
        }

        private void EditCells(HexCell center)
        {
            int centerX = center.coordinates.X;
            int centerZ = center.coordinates.Z;

            for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
            {
                for (int x = centerX - r; x <= centerX + brushSize; x++)
                {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
            for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
            {
                for (int x = centerX - brushSize; x <= centerX + r; x++)
                {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
        }

        private void EditCell(HexCell cell)
        {
            if (cell == null) return;

            if (activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }
            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }
            if (applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
            }
            if (applyUrbanLevel)
            {
                cell.UrbanLevel = activeUrbanLevel;
            }
            if (applyFarmLevel)
            {
                cell.FarmLevel = activeFarmLevel;
            }
            if (applyPlantLevel)
            {
                cell.PlantLevel = activePlantLevel;
            }
            if (applySpecialIndex)
            {
                cell.SpecialIndex = activeSpecialIndex;
            }

            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            if (roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }
            if (walledMode != OptionalToggle.Ignore)
            {
                cell.Walled = walledMode == OptionalToggle.Yes;
            }

            if (isDrag)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell != null)
                {
                    if (riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                    if (roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }

        private void CreateUnit()
        {
            HexCell cell = GetCellUnderCursor();
            if (cell != null && cell.Unit == null)
            {
                HexUnit unit = Instantiate(HexUnit.unitPrefab);
                float orientation = Random.Range(0f, 360f);
                hexGrid.AddUnit(unit, cell, orientation);
            }
        }

        private void DestroyUnit()
        {
            HexCell cell = GetCellUnderCursor();
            if (cell != null && cell.Unit != null)
            {
                hexGrid.RemoveUnit(cell.Unit);
            }
        }

        #region UGUI 绑定事件，设置各种参数

        public void SetTerrainTypeIndex(int index)
        {
            activeTerrainTypeIndex = index;
        }

        public void SetApplyElevation(bool toggle)
        {
            applyElevation = toggle;
        }

        /// <summary>
        /// 设置当前选中的海拔高度
        /// </summary>
        public void SetElevation(float elevation)
        {
            activeElevation = (int)elevation;
        }

        public void SetApplyWaterLevel(bool toggle)
        {
            applyWaterLevel = toggle;
        }

        public void SetWaterLevel(float level)
        {
            activeWaterLevel = (int)level;
        }

        public void SetBrushSize(float size)
        {
            brushSize = (int)size;
        }

        public void SetRiverMode(int mode)
        {
            riverMode = (OptionalToggle)mode;
        }

        public void SetRoadMode(int mode)
        {
            roadMode = (OptionalToggle)mode;
        }

        public void SetApplyUrbanLevel(bool toggle)
        {
            applyUrbanLevel = toggle;
        }

        public void SetUrbanLevel(float level)
        {
            activeUrbanLevel = (int)level;
        }

        public void SetApplyFarmLevel(bool toggle)
        {
            applyFarmLevel = toggle;
        }

        public void SetFarmLevel(float level)
        {
            activeFarmLevel = (int)level;
        }

        public void SetApplyPlantLevel(bool toggle)
        {
            applyPlantLevel = toggle;
        }

        public void SetPlantLevel(float level)
        {
            activePlantLevel = (int)level;
        }

        public void SetApplySpecialIndex(bool toggle)
        {
            applySpecialIndex = toggle;
        }

        public void SetSepcialIndex(float index)
        {
            activeSpecialIndex = (int)index;
        }

        public void SetWalledMode(int mode)
        {
            walledMode = (OptionalToggle)mode;
        }

        public void RefreshEntireGrid()
        {
            hexGrid.RefreshAllChunks();
        }

        public void ShowGrid(bool visible)
        {
            if (visible)
            {
                terrainMaterial.EnableKeyword("GRID_ON");
            }
            else
            {
                terrainMaterial.DisableKeyword("GRID_ON");
            }
        }

        public void SetEditMode(bool toggle)
        {
            enabled = toggle;
        }

        #endregion
    }
}
