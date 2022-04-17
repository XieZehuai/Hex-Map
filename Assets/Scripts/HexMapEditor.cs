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
                hexGrid.ColorCell(hit.point, activeColor);
            }
        }

        public void SelectColor(int index)
        {
            activeColor = colors[index];
        }
    }
}
