using UnityEngine;
using TMPro;

namespace HexMap.UI
{
	public class SaveLoadItem : MonoBehaviour
	{
        public SaveLoadMenu menu;

        private string mapName;

        public string MapName
        {
            get => mapName;
            set
            {
                mapName = value;
                transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = value;
            }
        }

        public void Select()
        {
            menu.SelectItem(mapName);
        }
	}
}