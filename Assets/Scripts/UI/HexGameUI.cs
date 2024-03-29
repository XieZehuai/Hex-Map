﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace HexMap.UI
{
    public class HexGameUI : MonoBehaviour
    {
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

            if (toggle)
            {
                Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
            }
            else
            {
                Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
            }
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
                    grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
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
                selectedUnit.Travel(grid.GetPath());
                grid.ClearPath();
            }
        }
    }
}