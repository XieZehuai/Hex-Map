using System.Collections.Generic;

namespace HexMap
{
    /// <summary>
    /// List对象池
    /// </summary>
    public static class ListPool<T>
    {
        private static Stack<List<T>> stack = new Stack<List<T>>();

		/// <summary>
		/// 获取一个类型为 T 的 list
		/// </summary>
        public static List<T> Get()
        {
            if (stack.Count > 0)
            {
                return stack.Pop();
            }

            return new List<T>();
        }

		/// <summary>
		/// 回收
		/// </summary>
        public static void Add(List<T> list)
        {
            list.Clear();
            stack.Push(list);
        }
    }
}