using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics
{
    public partial class ItemIdTable
    {
        /// <summary>
        /// 讀取 ItemIdTable.json 並回傳 Dictionary&lt;int, ItemModel&gt;
        /// </summary>
        public static Dictionary<int, ItemModel> GetItemTable()
        {
            var items = LoadItemsFromJson();
            return items.ToDictionary(
                i => i.Id,
                i => new ItemModel
                {
                    ConfigBaseId = i.Id,
                    Name = i.Name,
                    Type = i.Type
                });
        }

        /// <summary>
        /// 讀取 ItemIdTable.json 並回傳 Dictionary&lt;int, string&gt; (僅名稱)
        /// </summary>
        public static Dictionary<int, string> GetIdTable()
        {
            var items = LoadItemsFromJson();
            return items.ToDictionary(i => i.Id, i => i.Name);
        }

        /// <summary>
        /// 從 JSON 檔案載入物品清單
        /// </summary>
        private static List<ItemBaseModel> LoadItemsFromJson()
        {
            const string fileName = "ItemIdTable.json";
            var foundPath = FindConfigFile(fileName);

            if (foundPath == null)
            {
                throw new FileNotFoundException($"Could not find '{fileName}' in expected locations.");
            }

            var json = File.ReadAllText(foundPath, Encoding.UTF8);
            
            // 設定 JSON 反序列化選項
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // 不區分大小寫
                Converters = { new JsonStringEnumConverter() }  // 支援字串轉 Enum
            };
            
            var items = JsonSerializer.Deserialize<List<ItemBaseModel>>(json, options);

            return items ?? new List<ItemBaseModel>();
        }

        /// <summary>
        /// 尋找設定檔案的路徑
        /// </summary>
        private static string FindConfigFile(string fileName)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(AppContext.BaseDirectory, "src", "TorchLight.Statistics", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "TorchLight.Statistics", fileName)
            };

            return candidates.FirstOrDefault(File.Exists);
        }        
    }
}
