﻿using UnityEngine;

namespace HexMap
{
    public class HexCellShaderData : MonoBehaviour
    {
        private Texture2D cellTexture;
        private Color32[] cellTextureData;

        private bool shouldRefresh = false;

        public void Initialize(int x, int z)
        {
            if (cellTexture != null)
            {
                cellTexture.Resize(x, z);
            }
            else
            {
                cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true);
                cellTexture.filterMode = FilterMode.Point;
                cellTexture.wrapMode = TextureWrapMode.Clamp;

                Shader.SetGlobalTexture("_HexCellData", cellTexture);
            }

            Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));

            if (cellTextureData == null || cellTextureData.Length != x * z)
            {
                cellTextureData = new Color32[x * z];
            }
            else
            {
                for (int i = 0; i < cellTextureData.Length; i++)
                {
                    cellTextureData[i] = new Color32(0, 0, 0, 0);
                }
            }

            shouldRefresh = true;
        }

        public void RefreshTerrain(HexCell cell)
        {
            cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
            shouldRefresh = true;
        }

        public void RefreshVisibility(HexCell cell)
        {
            cellTextureData[cell.Index].r = cell.IsVisible ? (byte)255 : (byte)0;
            shouldRefresh = true;
        }

        private void LateUpdate()
        {
            if (shouldRefresh)
            {
                cellTexture.SetPixels32(cellTextureData);
                cellTexture.Apply();
                shouldRefresh = false;
            }
        }
    }
}