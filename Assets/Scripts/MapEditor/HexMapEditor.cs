using UnityEngine;
using UnityEngine.EventSystems;

namespace HexMap.Editor
{
    /// <summary>
    /// 六边形地图编辑器
    /// </summary>
    public class HexMapEditor : MonoBehaviour
    {
        public Color[] colors;
        public HexGrid hexGrid;

        private bool applyColor;
        private Color activeColor; // 当前选中的颜色

        private bool applyElevation = true;
        private int activeElevation; // 当前选中的海拔高度

        private bool applyWaterLevel = true;
        private int activeWaterLevel;

        private bool applyUrbanLevel;
        private int activeUrbanLevel;

        private int brushSize; // 笔刷大小，覆盖范围为 2 * brushSize + 1
        private OptionalToggle riverMode; // 河流的编辑模式
        private OptionalToggle roadMode; // 道路的编辑模式

        // 用于检测鼠标拖动输入
        private bool isDrag;
        private bool isDragOnUI; // 是否在 UI 上拖动鼠标，防止在编辑面板上调整参数时鼠标划到地图上
        private HexDirection dragDirection;
        private HexCell previousCell;

        private void Awake()
        {
            SelectColor(-1);
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
            if (Input.GetMouseButton(0) && !isPointOverUI && !isDragOnUI)
            {
                HandleInput();
            }
            else
            {
                previousCell = null;
            }
        }

        private void HandleInput()
        {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(inputRay, out RaycastHit hit))
            {
                HexCell currentCell = hexGrid.GetCell(hit.point);

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

            if (applyColor)
            {
                cell.Color = activeColor;
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

            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            if (roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
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

        /// <summary>
        /// 设置当前选中的颜色
        /// </summary>
        public void SelectColor(int index)
        {
            applyColor = index >= 0;
            if (applyColor)
            {
                activeColor = colors[index];
            }
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

        public void ShowUI(bool visible)
        {
            hexGrid.ShowUI(visible);
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

        public void RefreshEntireGrid()
        {
            hexGrid.RefreshAllChunks();
        }
    }
}
