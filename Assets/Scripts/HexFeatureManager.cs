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

        public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver, bool hasRoad)
        {
            // 两个相邻单元格之间一个有墙一个没墙时，在它们中间的连接处添加墙壁
            if (nearCell.Walled != farCell.Walled)
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

        private void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight)
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
        }

        private void AddWallSegment(Vector3 pivot, HexCell pivotCell, Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell)
        {
            AddWallSegment(pivot, left, pivot, right);
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
    }
}