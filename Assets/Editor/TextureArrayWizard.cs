using UnityEngine;
using UnityEditor;

namespace HexMap.Editor
{
    public class TextureArrayWizard : ScriptableWizard
    {
        private const string wizardTitle = "Create Texture Array";
        private const string buttonName = "Create";

        public Texture2D[] textures;

        [MenuItem("Assets/Create/Texture Array")]
        private static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<TextureArrayWizard>(wizardTitle, buttonName);
        }

        private void OnWizardCreate()
        {
            if (textures.Length == 0) return;

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Texture Array", "Texture Array", "asset", "Save Texture Array"
            );

            if (path.Length == 0) return;

            Texture2D texture = textures[0];
            int width = texture.width;
            int height = texture.height;
            int length = textures.Length;
            TextureFormat textureFormat = texture.format;
            bool mipChain = texture.mipmapCount > 1;

            Texture2DArray textureArray = new Texture2DArray(width, height, length, textureFormat, mipChain);

            textureArray.anisoLevel = texture.anisoLevel;
            textureArray.filterMode = texture.filterMode;
            textureArray.wrapMode = texture.wrapMode;

            for (int i = 0; i < textures.Length; i++)
            {
                for (int m = 0; m < texture.mipmapCount; m++)
                {
                    Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
                }
            }

            AssetDatabase.CreateAsset(textureArray, path);
        }
    }
}