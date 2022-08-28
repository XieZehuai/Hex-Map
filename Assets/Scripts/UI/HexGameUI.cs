using UnityEngine;
using UnityEngine.EventSystems;

namespace HexMap.UI
{
    public class HexGameUI : MonoBehaviour
    {
        public const int SPEED_PER_TURN = 24;

        public HexGrid grid;

        private HexCell currentCell;
        private HexUnit selectedUnit;

        private void Update()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    DoSelection();
                }
                else if (selectedUnit)
                {
                    if (Input.GetMouseButtonDown(1))
                    {
                        DoMove();
                    }
                    else
                    {
                        DoPathFinding();
                    }
                }
            }
        }

        public void SetEditMode(bool toggle)
        {
            enabled = !toggle;
            grid.ShowUI(!toggle);
            grid.ClearPath();
        }

        private bool UpdateCurrentCell()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            HexCell cell = grid.GetCell(ray);
            if (cell != currentCell)
            {
                currentCell = cell;
                return true;
            }

            return false;
        }

        private void DoSelection()
        {
            grid.ClearPath();
            UpdateCurrentCell();

            if (currentCell != null)
            {
                selectedUnit = currentCell.Unit;
            }
        }

        private void DoPathFinding()
        {
            if (UpdateCurrentCell())
            {
                if (currentCell != null && selectedUnit.IsValidDestination(currentCell))
                {
                    grid.FindPath(selectedUnit.Location, currentCell, SPEED_PER_TURN);
                }
                else
                {
                    grid.ClearPath();
                }
            }
        }

        private void DoMove()
        {
            if (grid.HasPath)
            {
                selectedUnit.Location = currentCell;
                grid.ClearPath();
            }
        }
    }
}