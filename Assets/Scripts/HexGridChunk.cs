using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 表示一个六边形网格地图区块
    /// <para>
    /// 为了构造更大的地图，需要把地图给分成多个单独的区块
    /// </para>
    /// </summary>
    public class HexGridChunk : MonoBehaviour
    {
        private HexCell[] cells;

        private HexMesh hexMesh;
        private Canvas gridCanvas;

        private void Awake()
        {
            gridCanvas = GetComponentInChildren<Canvas>();
            hexMesh = GetComponentInChildren<HexMesh>();

            cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        }

        private void LateUpdate()
        {
            hexMesh.Triangulate(cells);
            enabled = false;
        }

        public void AddCell(int index, HexCell cell)
        {
            cells[index] = cell;
            cell.chunk = this;
            cell.transform.SetParent(transform, false);
            cell.uiRect.SetParent(gridCanvas.transform, false);
        }

        public void Refresh()
        {
            enabled = true;
        }
    }
}