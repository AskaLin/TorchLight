using TorchLight.Statistics.Enums;

namespace TorchLight.Statistics.Models
{
    public class MapRecordModel
    {
        /// <summary>
        /// 地圖ID(GeBuLinCunLuo01)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 地圖Token(1465431321654)
        /// </summary>
        public string RecordId { get; set; }

        public MapType Type { get; set; }
        /// <summary>
        /// 地圖名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 使用門票
        /// </summary>
        public string MapTicket { get; set; }
        public int MapTicketId { get; set; }

        /// <summary>
        /// 使用羅盤
        /// </summary>
        public string[] Compass { get; set; }

        /// <summary>
        /// 使用探針
        /// </summary>
        public string Probe { get; set; }

        /// <summary>
        /// 主要是紀錄 BaseId 與數量, PickedItemDataModel 後續看要不要拿來取得價格
        /// </summary>
        public Dictionary<int, PickedItemDataModel> PickRecord { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string UseTime => (EndTime - StartTime).ToString(@"hh\:mm\:ss");
    }
}
