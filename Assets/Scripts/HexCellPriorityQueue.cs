using System.Collections.Generic;
using UnityEngine;

namespace HexMap
{
    public class HexCellPriorityQueue
    {
        private List<HexCell> list = new List<HexCell>();
        private int count;
        private int minimum = int.MaxValue;

        public int Count => count;

        public void Enqueue(HexCell cell)
        {
            int priority = cell.SearchPriority;
            if (minimum >= priority)
            {
                minimum = priority;
            }

            while (list.Count <= priority)
            {
                list.Add(null);
            }

            cell.NextWithSamePriority = list[priority];
            list[priority] = cell;
            count++;
        }

        public HexCell Dequeue()
        {
            count--;

            for (; minimum < list.Count; minimum++)
            {
                HexCell cell = list[minimum];
                if (cell != null)
                {
                    list[minimum] = cell.NextWithSamePriority;
                    return cell;
                }
            }

            return null;
        }

        public void Change(HexCell cell, int oldPriority)
        {
            HexCell current = list[oldPriority];
            HexCell next = current.NextWithSamePriority;

            if (current == cell)
            {
                list[oldPriority] = next;
            }
            else
            {
                while (next != cell)
                {
                    current = next;
                    next = current.NextWithSamePriority;
                }

                current.NextWithSamePriority = cell.NextWithSamePriority;
            }

            Enqueue(cell);
			count--;
        }

        public void Clear()
        {
            list.Clear();
            count = 0;
            minimum = int.MaxValue;
        }
    }
}