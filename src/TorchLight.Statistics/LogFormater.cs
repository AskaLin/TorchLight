using System.Text.RegularExpressions;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics
{
    public partial class LogFormater
    {

        private readonly Dictionary<int, string> _idTable;

        private readonly List<string> _ignorePages =
        [
            "PageId = 100", // 忽略裝備欄
            "PageId = 101", // 忽略技能欄
        ];

        public LogFormater()
        {
            _idTable = IdTable.GetIdTable();
        }

        public ItemModel GetInitBagData(string line)
            => !line.Contains("BagMgr@:InitBagData") || PassIgnorePages(line) ? null : GetItemData(line);
        public ItemModel GetModfyBagItem(string line)
            => !line.Contains("BagMgr@:Modfy BagItem") || PassIgnorePages(line) ? null : GetItemData(line);

        public bool IsInitBagData(string line) => line.Contains("BagMgr@:InitBagData") && !PassIgnorePages(line);
        public bool IsModfyBagItem(string line) => line.Contains("BagMgr@:Modfy BagItem") && !PassIgnorePages(line);

        public ItemModel GetItemData(string line)
        {
            // Regex 擷取時間、ConfigBaseId 與 Num    
            var match = LogLineRegex().Match(line);
            if (match.Success)
            {
                var result = new ItemModel
                {
                    PageId = Convert.ToInt16(match.Groups["page"].Value),
                    SoltId = Convert.ToInt16(match.Groups["slot"].Value),
                    ConfigBaseId = Convert.ToInt32(match.Groups["config"].Value),
                    Num = Convert.ToInt16(match.Groups["num"].Value)
                };

                // 將 Unreal 格式轉為 DateTime
                string rawTime = match.Groups["time"].Value;
                result.Time = DateTime.ParseExact(rawTime, "yyyy.MM.dd-HH.mm.ss:fff", null).AddHours(8);
                result.Name = _idTable.TryGetValue(result.ConfigBaseId, out string value) ? value : "未知物品";            
                return result;
            }
            else
            {
                Console.WriteLine("未匹配到資料。");
                throw new Exception("未匹配到資料。");
            }
        }
        private bool PassIgnorePages(string line)
        {
            foreach (var ignorePage in _ignorePages)
            {
                if (line.Contains(ignorePage))
                {
                    return true;
                }
            }
            return false;
        }

        // [2025.10.28-11.36.16:232][961]GameLog: Display: [Game] BagMgr@:Modfy BagItem PageId = 102 SlotId = 3 ConfigBaseId = 100300 Num = 742
        // [2025.10.28-15.25.01:559][ 44]GameLog: Display: [Game] BagMgr@:InitBagData PageId = 103 SlotId = 59 ConfigBaseId = 6004 Num = 6        

        [GeneratedRegex(@"\[(?<time>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\].*?PageId\s*=\s*(?<page>\d+)\s+SlotId\s*=\s*(?<slot>\d+)\s+ConfigBaseId\s*=\s*(?<config>\d+)\s+Num\s*=\s*(?<num>\d+)", RegexOptions.Singleline)]
        private static partial Regex LogLineRegex();
    }
}
