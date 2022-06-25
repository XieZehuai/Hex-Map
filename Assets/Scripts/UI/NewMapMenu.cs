using UnityEngine;

namespace HexMap.UI
{
    /// <summary>
    /// 创建新地图 UI
    /// </summary>
    public class NewMapMenu : MonoBehaviour
    {
        public HexGrid hexGrid;

        public void Open()
        {
            gameObject.SetActive(true);
			HexMapCamera.Instance.Lock();
        }

        public void Close()
        {
            gameObject.SetActive(false);
			HexMapCamera.Instance.Unlock();
        }

        public void CreateSmallMap()
        {
            CreateMap(20, 15);
        }

        public void CreateMediumMap()
        {
            CreateMap(40, 30);
        }

        public void CreateLargeMap()
        {
            CreateMap(80, 60);
        }

        private void CreateMap(int x, int z)
        {
            hexGrid.CreateMap(x, z);
			HexMapCamera.Instance.ValidatePosition();
            Close();
        }
    }
}