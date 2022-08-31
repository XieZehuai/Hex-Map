using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexMap
{
    public class HexUnit : MonoBehaviour
    {
        public static HexUnit unitPrefab;

        private HexCell location;
        private float orientation;
        private List<HexCell> pathToTravel;

        private const float travelSpeed = 4f;

        private void OnEnable()
        {
            // 如果在移动的过程中，重新编译代码，会导致协程停止，使单位停止移动，
            // 所以在重新编译后，重新设置单位的坐标
            if (location)
            {
                transform.localPosition = location.Position;
            }
        }

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

        public void Travel(List<HexCell> path)
        {
            Location = path[path.Count - 1];
            pathToTravel = path;

            StopAllCoroutines();
            StartCoroutine(TravelPath());
        }

        private IEnumerator TravelPath()
        {
            for (int i = 1; i < pathToTravel.Count; i++)
            {
                Vector3 a = pathToTravel[i - 1].Position;
                Vector3 b = pathToTravel[i].Position;

                for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed)
                {
                    transform.localPosition = Vector3.Lerp(a, b, t);
                    yield return null;
                }
            }
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

        // private void OnDrawGizmos()
        // {
        //     if (pathToTravel == null || pathToTravel.Count == 0)
        //     {
        //         return;
        //     }

        //     for (int i = 1; i < pathToTravel.Count; i++)
        //     {
        //         Vector3 a = pathToTravel[i - 1].Position;
        //         Vector3 b = pathToTravel[i].Position;

        //         for (float j = 0f; j < 1f; j += 0.1f)
        //         {
        //             Gizmos.DrawSphere(Vector3.Lerp(a, b, j), 2f);
        //         }
        //     }
        // }
    }
}