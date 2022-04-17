namespace HexMap
{
    /// <summary>
    /// 表示六边形六条边对应的六个方向 
    /// </summary>
    public enum HexDirection
    {
        /// <summary>
        /// North East
        /// </summary>
        NE,
        /// <summary>
        /// East
        /// </summary>
        E,
        /// <summary>
        /// South East
        /// </summary>
        SE,
        /// <summary>
        /// South West
        /// </summary>
        SW,
        /// <summary>
        /// West
        /// </summary>
        W,
        /// <summary>
        /// North West
        /// </summary>
        NW
    }

    public static class HexDirectionExtensions
    {
        /// <summary>
        /// 当前方向的反方向
        /// </summary>
        public static HexDirection Opposite(this HexDirection direction)
        {
            return (int)direction < 3 ? (direction + 3) : (direction - 3);
        }

        /// <summary>
        /// 当前方向的上一个方向
        /// </summary>
        public static HexDirection Previous(this HexDirection direction)
        {
            return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
        }

        /// <summary>
        /// 当前方向的下一个方向
        /// </summary>
        public static HexDirection Next(this HexDirection direction)
        {
            return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
        }
    }
}
