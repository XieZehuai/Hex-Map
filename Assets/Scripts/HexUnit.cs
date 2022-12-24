using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexMap
{
    public class HexUnit : MonoBehaviour
    {
        public const int SPEED_PER_TURN = 24;

        public static HexUnit unitPrefab;

        private HexCell location;
        private HexCell currentTravelLocation;
        private float orientation;
        private List<HexCell> pathToTravel;

        private const float travelSpeed = 4f;
        private const float rotationSpeed = 180f;

        public int VisionRange
        {
            get
            {
                return 3;
            }
        }

        public HexCell Location
        {
            get { return location; }
            set
            {
                if (location != null)
                {
                    Grid.DecreaseVisibility(location, VisionRange);
                    location.Unit = null;
                }

                location = value;
                value.Unit = this;
                Grid.IncreaseVisibility(location, VisionRange);
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

        public int Speed
        {
            get
            {
                return SPEED_PER_TURN;
            }
        }

        public HexGrid Grid { get; set; }

        private void OnEnable()
        {
            // 如果在移动的过程中，重新编译代码，会导致协程停止，使单位停止移动，
            // 所以在重新编译后，重新设置单位的坐标
            if (location)
            {
                transform.localPosition = location.Position;

                if (currentTravelLocation)
                {
                    Grid.IncreaseVisibility(location, VisionRange);
                    Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
                    currentTravelLocation = null;
                }
            }
        }

        public void ValidateLocation()
        {
            transform.localPosition = location.Position;
        }

        public bool IsValidDestination(HexCell cell)
        {
            return cell.IsExplored && !cell.IsUnderWater && cell.Unit == null;
        }

        public int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction)
        {
            HexEdgeType edgeType = fromCell.GetEdgeType(toCell);

            if (edgeType == HexEdgeType.Cliff)
            {
                return -1;
            }

            int moveCost; // 从当前单元格移动到邻居单元格的消耗
            if (fromCell.HasRoadThroughEdge(direction))
            {
                moveCost = 1;
            }
            // 当前单元格与相邻单元格之间被墙壁隔开且中间没有道路
            else if (fromCell.Walled != toCell.Walled)
            {
                return -1;
            }
            else
            {
                moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
            }

            return moveCost;
        }

        public void Travel(List<HexCell> path)
        {
            location.Unit = null;
            location = path[path.Count - 1];
            location.Unit = this;

            pathToTravel = path;

            StopAllCoroutines();
            StartCoroutine(TravelPath());
        }

        private IEnumerator TravelPath()
        {
            Vector3 a, b, c = pathToTravel[0].Position;

            yield return LookAt(pathToTravel[1].Position);
            Grid.DecreaseVisibility(currentTravelLocation ?? pathToTravel[0], VisionRange);

            float t = Time.deltaTime * travelSpeed;

            for (int i = 1; i < pathToTravel.Count; i++)
            {
                currentTravelLocation = pathToTravel[i];
                a = c;
                b = pathToTravel[i - 1].Position;
                c = (b + currentTravelLocation.Position) * 0.5f;
                Grid.IncreaseVisibility(pathToTravel[i], VisionRange);

                for (; t < 1f; t += Time.deltaTime * travelSpeed)
                {
                    transform.localPosition = Bezier.GetPoint(a, b, c, t);
                    Vector3 direction = Bezier.GetDerivative(a, b, c, t);
                    direction.y = 0f;
                    transform.localRotation = Quaternion.LookRotation(direction);
                    yield return null;
                }

                Grid.DecreaseVisibility(pathToTravel[i], VisionRange);
                t -= 1f;
            }
            currentTravelLocation = null;

            a = c;
            b = location.Position;
            c = b;
            Grid.IncreaseVisibility(location, VisionRange);

            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 direction = Bezier.GetDerivative(a, b, c, t);
                direction.y = 0f;
                transform.localRotation = Quaternion.LookRotation(direction);
                yield return null;
            }

            transform.localPosition = location.Position;
            orientation = transform.localRotation.eulerAngles.y;

            ListPool<HexCell>.Add(pathToTravel);
            pathToTravel = null;
        }

        private IEnumerator LookAt(Vector3 point)
        {
            point.y = transform.localPosition.y;
            Quaternion fromRotation = transform.localRotation;
            Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);
            float angle = Quaternion.Angle(fromRotation, toRotation);
            float speed = rotationSpeed / angle;

            if (angle > 0f)
            {
                for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
                {
                    transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                    yield return null;
                }
            }

            transform.LookAt(point);
            orientation = transform.localRotation.eulerAngles.y;
        }

        public void Die()
        {
            if (location != null)
            {
                Grid.DecreaseVisibility(location, VisionRange);
            }

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