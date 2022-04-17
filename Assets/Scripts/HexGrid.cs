using UnityEngine;
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
        [SerializeField] private Color defaultColor = Color.white;

        private HexCell[] cells;
        private Canvas gridCanvas;
        private HexMesh hexMesh;

        private void Awake()
        {
            gridCanvas = GetComponentInChildren<Canvas>();
            hexMesh = GetComponentInChildren<HexMesh>();

            cells = new HexCell[height * width];

            for (int z = 0, i = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    CreateCell(x, z, i++);
                }
            }
        }

        private void CreateCell(int x, int z, int i)
        {
            Vector3 position;
            position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.outerRadius * 1.5f);

            HexCell cell = cells[i] = Instantiate(cellPrefab, transform);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.color = defaultColor;

            Text label = Instantiate(cellLabelPrefab, gridCanvas.transform);
            label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
            label.text = cell.coordinates.ToStringOnSeparateLines();
        }

        private void Start()
        {
            hexMesh.Triangulate(cells);
        }

        /// <summary>
        /// 改变目标单元格的颜色
        /// </summary>
        /// <param name="position">单元格在世界空间下的坐标</param>
        /// <param name="color">要改变的颜色</param>
        public void ColorCell(Vector3 position, Color color)
        {
            // 为避免当前物体位移对于点击位置坐标的影响，把点击坐标转换到以当前物体
            // 为原点的坐标空间下再计算六角坐标
            position = transform.InverseTransformPoint(position);
            HexCoordinates coordinates = HexCoordinates.FromPosition(position);

            // 改变点击单元格的颜色
            int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
            HexCell cell = cells[index];
            cell.color = color;

            // 重建整个网格（可以优化）
            hexMesh.Triangulate(cells);
        }
    }
}