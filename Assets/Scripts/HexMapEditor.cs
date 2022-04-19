using UnityEngine;
using UnityEngine.EventSystems;

namespace HexMap
{
    /// <summary>
    /// 六边形地图编辑器
    /// </summary>
    public class HexMapEditor : MonoBehaviour
    {
        public Color[] colors;
        public HexGrid hexGrid;

        private Color activeColor; // 当前选中的颜色
        private int activeElevation; // 当前选中的海拔高度

        private void Awake()
        {
            SelectColor(0);
        }

        private void Update()
        {
            // 当点击鼠标左键并且鼠标不处于UI上时处理点击操作
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(inputRay, out RaycastHit hit))
            {
                EditCell(hexGrid.GetCell(hit.point));
            }
        }

        private void EditCell(HexCell cell)
        {
            cell.color = activeColor;
            cell.Elevation = activeElevation;
            hexGrid.Refresh();
        }

        /// <summary>
        /// 设置当前选中的颜色
        /// </summary>
        public void SelectColor(int index)
        {
            activeColor = colors[index];
        }

        /// <summary>
        /// 设置当前选中的海拔高度
        /// </summary>
        public void SetElevation(float elevation)
        {
            activeElevation = (int)elevation;
        }
    }
}
