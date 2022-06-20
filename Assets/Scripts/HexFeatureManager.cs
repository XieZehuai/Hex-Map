using UnityEngine;

namespace HexMap
{
    /// <summary>
    /// 地图特性管理器
    /// <para>
    /// 在每个 HexGridChunk 中，使用 HexMesh 控制地形细节，而在地形之上，则使用
    /// HexFeatureManager 控制地面细节，如树木、草地、建筑物等
    /// </para>
    /// </summary>
    public class HexFeatureManager : MonoBehaviour
    {
        [SerializeField] private HexFeatureCollection[] urbanCollections = default;
        [SerializeField] private HexFeatureCollection[] farmCollections = default;
        [SerializeField] private HexFeatureCollection[] plantCollections = default;
        [SerializeField] private HexMesh walls = default;
        [SerializeField] private Transform wallTower = default;
        [SerializeField] private Transform bridge = default;
        [SerializeField] private Transform[] special = default;

        private Transform container;

        public void Clear()
        {
            if (container != null)
            {
                Destroy(container.gameObject);
            }

            container = new GameObject("Features Container").transform;
            container.SetParent(transform, false);

            walls.Clear();
        }

        public void Apply()
        {
            walls.Apply();
        }

        public void AddFeature(HexCell cell, Vector3 position)
        {
            if (cell.IsSpecial) return;

            HexHash hash = HexMetrics.SampleHashGrid(position);

            Transform prefab = PickPrefab(urbanCollections, cell.UrbanLevel, hash.a, hash.d);
            Transform otherPrefab = PickPrefab(farmCollections, cell.FarmLevel, hash.b, hash.d);
            float usedHash = hash.a;

            if (prefab != null)
            {
                if (otherPrefab != null && hash.b < hash.a)
                {
                    prefab = otherPrefab;
                    usedHash = hash.b;
                }
            }
            else if (otherPrefab != null)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }

            otherPrefab = PickPrefab(plantCollections, cell.PlantLevel, hash.c, hash.d);
            if (prefab != null)
            {
                if (otherPrefab != null && hash.c < usedHash)
                {
                    prefab = otherPrefab;
                }
            }
            else if (otherPrefab != null)
            {
                prefab = otherPrefab;
            }
            else
            {
                return;
            }

            Transform instance = Instantiate(prefab, container);

            position.y += instance.localScale.y * 0.5f;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, hash.e * 360f, 0f);
        }

        private Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
        {
            if (level > 0)
            {
                float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);

                for (int i = 0; i < thresholds.Length; i++)
                {
                    if (hash < thresholds[i])
                    {
                        return collection[i].Pick(choice);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 在两个单元格间的连接处添加墙壁，如果可以添加的话
        /// </summary>
        public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver, bool hasRoad)
        {
            // 两个相邻单元格之间一个有墙一个没墙时，在它们中间的连接处添加墙壁
            if (CanAddWall(nearCell, farCell))
            {
                AddWallSegment(near.v1, far.v1, near.v2, far.v2);

                if (hasRiver || hasRoad)
                {
                    AddWallCap(near.v2, far.v2);
                    AddWallCap(far.v4, near.v4);
                }
                else
                {
                    AddWallSegment(near.v2, far.v2, near.v3, far.v3);
                    AddWallSegment(near.v3, far.v3, near.v4, far.v4);
                }

                AddWallSegment(near.v4, far.v4, near.v5, far.v5);
            }
        }

        /// <summary>
        /// 判断在两个单元格之间是否可以添加墙壁
        /// </summary>
        private static bool CanAddWall(HexCell nearCell, HexCell farCell)
        {
            return nearCell.Walled != farCell.Walled &&
                !nearCell.IsUnderWater && !farCell.IsUnderWater &&
                nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff;
        }

        /// <summary>
        /// 在三个单元格之间的连接处添加墙壁，如果可以的话
        /// </summary>
        public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3)
        {
            if (cell1.Walled)
            {
                if (cell2.Walled)
                {
                    if (!cell3.Walled)
                    {
                        AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                    }
                }
                else if (cell3.Walled)
                {
                    AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
                }
                else
                {
                    AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
                }
            }
            else if (cell2.Walled)
            {
                if (cell3.Walled)
                {
                    AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
                }
                else
                {
                    AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
            }
        }

        /// <summary>
        /// 添加三角形的墙壁片段，用于连接三个单元格之间的墙壁
        /// </summary>
        private void AddWallSegment(Vector3 pivot, HexCell pivotCell, Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            if (pivotCell.IsUnderWater) return;

            bool hasLeftWall = !leftCell.IsUnderWater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
            bool hasRightWall = !rightCell.IsUnderWater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

            if (hasLeftWall)
            {
                if (hasRightWall)
                {
                    bool hasTower = false;
                    if (leftCell.Elevation == rightCell.Elevation)
                    {
                        HexHash hash = HexMetrics.SampleHashGrid((pivot + left + right) * (1f / 3f));
                        hasTower = hash.e < HexMetrics.wallTowerThreshold;
                    }

                    AddWallSegment(pivot, left, pivot, right, hasTower);
                }
                else if (leftCell.Elevation < rightCell.Elevation)
                {
                    AddWallWedge(pivot, left, right);
                }
                else
                {
                    AddWallCap(pivot, left);
                }
            }
            else if (hasRightWall)
            {
                if (rightCell.Elevation < leftCell.Elevation)
                {
                    AddWallWedge(right, pivot, left);
                }
                else
                {
                    AddWallCap(right, pivot);
                }
            }
        }

        /// <summary>
        /// 添加四边形的墙壁片段，用于连接两个单元格之间的墙壁
        /// </summary>
        private void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight,
            bool addTower = false)
        {
            nearLeft = HexMetrics.Perturb(nearLeft);
            farLeft = HexMetrics.Perturb(farLeft);
            nearRight = HexMetrics.Perturb(nearRight);
            farRight = HexMetrics.Perturb(farRight);

            Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
            Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

            Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
            Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

            float leftTop = left.y + HexMetrics.wallHeight;
            float rightTop = right.y + HexMetrics.wallHeight;

            Vector3 v1, v2, v3, v4;
            v1 = v3 = left - leftThicknessOffset;
            v2 = v4 = right - rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            walls.AddQuadUnperturbed(v1, v2, v3, v4);

            Vector3 t1 = v3, t2 = v4;

            v1 = v3 = left + leftThicknessOffset;
            v2 = v4 = right + rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            walls.AddQuadUnperturbed(v2, v1, v4, v3);

            walls.AddQuadUnperturbed(t1, t2, v3, v4);

            if (addTower)
            {
                AddWallTower(left, right);
            }
        }

        /// <summary>
        /// 在墙壁之间添加塔楼（类似长城）
        /// </summary>
        private void AddWallTower(Vector3 left, Vector3 right)
        {
            Transform tower = Instantiate(wallTower, container);
            tower.transform.localPosition = (left + right) * 0.5f;
            Vector3 rightDirection = right - left;
            rightDirection.y = 0f;
            tower.transform.right = rightDirection;
        }

        private void AddWallCap(Vector3 near, Vector3 far)
        {
            near = HexMetrics.Perturb(near);
            far = HexMetrics.Perturb(far);

            Vector3 center = HexMetrics.WallLerp(near, far);
            Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

            Vector3 v1, v2, v3, v4;

            v1 = v3 = center - thickness;
            v2 = v4 = center + thickness;
            v3.y = v4.y = center.y + HexMetrics.wallHeight;
            walls.AddQuadUnperturbed(v1, v2, v3, v4);
        }

        private void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
        {
            near = HexMetrics.Perturb(near);
            far = HexMetrics.Perturb(far);
            point = HexMetrics.Perturb(point);

            Vector3 center = HexMetrics.WallLerp(near, far);
            Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

            Vector3 v1, v2, v3, v4;
            Vector3 pointTop = point;
            point.y = center.y;

            v1 = v3 = center - thickness;
            v2 = v4 = center + thickness;
            v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;

            walls.AddQuadUnperturbed(v1, point, v3, pointTop);
            walls.AddQuadUnperturbed(point, v2, pointTop, v4);
            walls.AddTriangleUnperturbed(pointTop, v3, v4);
        }

        /// <summary>
        /// 在被河流分开的道路中间添加桥梁
        /// </summary>
        public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
        {
            roadCenter1 = HexMetrics.Perturb(roadCenter1);
            roadCenter2 = HexMetrics.Perturb(roadCenter2);

            Transform instance = Instantiate(bridge, container);
            instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
            instance.forward = roadCenter2 - roadCenter1;

            float length = Vector3.Distance(roadCenter1, roadCenter2);
            instance.localScale = new Vector3(1f, 1f, length * (1f / HexMetrics.bridgeDesignLength));
        }

        public void AddSpecialFeature(HexCell cell, Vector3 position)
        {
            Transform instance = Instantiate(special[cell.SpecialIndex - 1], container);
            instance.localPosition = HexMetrics.Perturb(position);

            HexHash hash = HexMetrics.SampleHashGrid(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        }
    }
}