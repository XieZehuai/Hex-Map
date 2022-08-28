﻿using System.IO;
using UnityEngine;

namespace HexMap
{
    public class HexUnit : MonoBehaviour
    {
        public static HexUnit unitPrefab;

        private HexCell location;
        private float orientation;

        public HexCell Location
        {
            get { return location; }
            set
            {
                if (location != null)
                {
                    location.Unit = null;
                }

                location = value;
                value.Unit = this;
                transform.localPosition = value.Position;
            }
        }

        public float Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;
                transform.localRotation = Quaternion.Euler(0f, value, 0f);
            }
        }

        public void ValidateLocation()
        {
            transform.localPosition = location.Position;
        }

        public bool IsValidDestination(HexCell cell)
        {
            return !cell.IsUnderWater && cell.Unit == null;
        }

        public void Die()
        {
            location.Unit = null;
            Destroy(gameObject);
        }

        public void Save(BinaryWriter writer)
        {
            location.coordinates.Save(writer);
            writer.Write(orientation);
        }

        public static void Load(BinaryReader reader, HexGrid grid)
        {
            HexCoordinates coordinates = HexCoordinates.Load(reader);
            float orientation = reader.ReadSingle();

            HexUnit unit = Instantiate(unitPrefab);
            HexCell cell = grid.GetCell(coordinates);
            grid.AddUnit(unit, cell, orientation);
        }
    }
}