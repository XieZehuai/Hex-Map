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

        private Transform container;

        public void Clear()
        {
            if (container != null)
            {
                Destroy(container.gameObject);
            }

            container = new GameObject("Features Container").transform;
            container.SetParent(transform, false);
        }

        public void Apply()
        {

        }

        public void AddFeature(HexCell cell, Vector3 position)
        {
            HexHash hash = HexMetrics.SampleHashGrid(position);

            Transform prefab = PickPrefab(cell.UrbanLevel, hash.a, hash.b);
            if (prefab == null) return;

            Transform instance = Instantiate(prefab, container);

            position.y += instance.localScale.y * 0.5f;
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, hash.c * 360f, 0f);
        }

        private Transform PickPrefab(int level, float hash, float choice)
        {
            if (level > 0)
            {
                float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);

                for (int i = 0; i < thresholds.Length; i++)
                {
                    if (hash < thresholds[i])
                    {
                        return urbanCollections[i].Pick(choice);
                    }
                }
            }

            return null;
        }
    }
}