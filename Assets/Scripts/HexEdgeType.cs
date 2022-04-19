namespace HexMap
{
    /// <summary>
    /// 单元格之间连接的类型
    /// </summary>
    public enum HexEdgeType
    {
        /// <summary>
        /// 平地
		/// <para>
		/// 对于海拔相同的两个单元格，它们之间的连接是平地
		/// </para>
        /// </summary>
        Flat,

		/// <summary>
        /// 斜坡（梯田形状的斜坡）
		/// <para>
		/// 当于海拔相差一个单位的两个单元格，它们之间的连接是斜坡
		/// </para>
        /// </summary>
        Slope,

		/// <summary>
        /// 峭壁
		/// <para>
		/// 当于海拔相差超过一个单位的两个单元格，它们之间的连接是峭壁
		/// </para>
        /// </summary>
        Cliff,
    }
}