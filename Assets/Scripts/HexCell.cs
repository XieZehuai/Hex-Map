using UnityEngine;

namespace HexMap
{
	/// <summary>
	/// 表示一个独立的六边形单元格，只保存数据，不负责与视觉（模型，网格等）相关的事情
	/// </summary>
	public class HexCell : MonoBehaviour
	{
		public HexCoordinates coordinates;
        public Color color;
	}
}