using System;
using System.IO;
using UnityEngine;
using TMPro;

namespace HexMap.UI
{
    public class SaveLoadMenu : MonoBehaviour
    {
        public const int SAVE_FILE_VERSION = 3;

        public TextMeshProUGUI menuLabel;
        public TextMeshProUGUI actionButtonLabel;
        public TMP_InputField nameInput;

        public RectTransform listContent;
        public SaveLoadItem itemPrefab;

        public HexGrid hexGrid;

        private bool saveMode;

        public void Open(bool saveMode)
        {
            this.saveMode = saveMode;

            AdjustUILabel();
            FillList();

            gameObject.SetActive(true);
            HexMapCamera.Instance.Lock();
        }

        private void AdjustUILabel()
        {
            if (saveMode)
            {
                menuLabel.text = "Save Map";
                actionButtonLabel.text = "Save";
            }
            else
            {
                menuLabel.text = "Load Map";
                actionButtonLabel.text = "Load";
            }
        }

        private void FillList()
        {
            for (int i = 0; i < listContent.childCount; i++)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }

            string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
            Array.Sort(paths);

            for (int i = 0; i < paths.Length; i++)
            {
                SaveLoadItem item = Instantiate(itemPrefab, listContent);
                item.menu = this;
                item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
            HexMapCamera.Instance.Unlock();
        }

        public void Action()
        {
            string path = GetSelectedPath();
            if (path == null)
            {
                return;
            }

            if (saveMode)
            {
                Save(path);
            }
            else
            {
                Load(path);
            }

            Close();
        }

        public void Delete()
        {
            string path = GetSelectedPath();
            if (path == null)
            {
                return;
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            nameInput.text = "";
            FillList();
        }

        public void SelectItem(string name)
        {
            nameInput.text = name;
        }

        private string GetSelectedPath()
        {
            string mapName = nameInput.text;
            if (mapName.Length == 0)
            {
                return null;
            }

            return Path.Combine(Application.persistentDataPath, mapName + ".map");
        }

        private void Save(string path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                writer.Write(SAVE_FILE_VERSION);
                hexGrid.Save(writer);
            }
        }

        private void Load(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("File does not exist " + path);
                return;
            }

            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                int header = reader.ReadInt32();

                if (header <= SAVE_FILE_VERSION)
                {
                    hexGrid.Load(reader, header);
                    HexMapCamera.Instance.ValidatePosition();
                }
                else
                {
                    Debug.LogWarning("Unknown map format " + header);
                }
            }
        }
    }
}