using Serilog;
using TorchLight.Statistics.Configuration;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Mapper
{
    /// <summary>
    /// 物品映射器 - 負責物品ID與名稱的轉換，以及物品類型的判斷
    /// </summary>
    public class ItemInfoMapper
    {
        private static readonly object _lock = new();
        private static Dictionary<int, ItemBaseModel> _itemConfigs = [];

        /// <summary>
        /// 當物品設定更新時觸發
        /// </summary>
        public static event Action<bool, string> OnConfigUpdated;

        /// <summary>
        /// 初始化物品映射器（從 AppConfiguration 載入）
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                try
                {
                    // 從 AppConfiguration 載入物品ID字典                    
                    _itemConfigs = AppConfiguration.ItemIdDictionary;

                    Log.Information("已載入物品設定: {ItemCount} 個物品", _itemConfigs.Count);
                    OnConfigUpdated?.Invoke(true, "物品設定已成功載入");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "載入物品設定失敗");
                    OnConfigUpdated?.Invoke(false, $"載入失敗: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 獲取物品資訊（透過 int itemId）
        /// </summary>
        public static ItemBaseModel GetItemInfo(int itemId)
        {
            lock (_lock)
            {
                if (_itemConfigs.TryGetValue(itemId, out var config))
                {
                    return config;
                }
                return null;
            }
        }

        /// <summary>
        /// 新增或更新物品映射
        /// </summary>
        /// <param name="item">物品資訊</param>
        public static bool AddOrUpdateItem(ItemBaseModel item)
        {
            lock (_lock)
            {
                try
                {
                    _itemConfigs[item.Id] = item;

                    // 儲存到 JSON 檔案
                    if (!AppConfiguration.SaveItemMapperToJson())
                    {
                        Log.Warning("更新記憶體成功，但儲存檔案失敗");
                    }

                    Log.Information("已更新物品映射: {ItemId} - {ItemName} ({ItemType})",
                            item.Id, item.Name, item.Type);
                    OnConfigUpdated?.Invoke(true, "物品設定已更新");
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "新增或更新物品映射失敗");
                    return false;
                }
            }
        }

        /// <summary>
        /// 刪除物品映射
        /// </summary>
        public static bool DeleteItem(int itemId)
        {
            lock (_lock)
            {
                try
                {
                    if (!_itemConfigs.ContainsKey(itemId))
                    {
                        return false;
                    }

                    var itemName = _itemConfigs[itemId].Name;
                    _itemConfigs.Remove(itemId);

                    // 儲存到 JSON 檔案
                    if (!AppConfiguration.SaveItemMapperToJson())
                    {
                        Log.Warning("刪除記憶體成功，但儲存檔案失敗");
                    }

                    Log.Information("已刪除物品映射: {ItemId} - {ItemName}", itemId, itemName);
                    OnConfigUpdated?.Invoke(true, "物品設定已刪除");
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "刪除物品映射失敗");
                    return false;
                }
            }
        }

        /// <summary>
        /// 檢查物品是否啟用統計
        /// </summary>
        public static bool IsItemEnabled(int itemId)
        {
            lock (_lock)
            {
                if (_itemConfigs.TryGetValue(itemId, out var config))
                {
                    return config.Enable;
                }
                return false;
            }
        }

        /// <summary>
        /// 根據物品ID獲取物品名稱
        /// </summary>
        public static string GetItemName(int itemId)
        {
            lock (_lock)
            {
                if (_itemConfigs.TryGetValue(itemId, out var config))
                {
                    return config.Name;
                }
                return $"未知物品 ({itemId})";
            }
        }

        /// <summary>
        /// 根據物品ID獲取物品類型
        /// </summary>
        public static ItemType GetItemType(int itemId)
        {
            lock (_lock)
            {
                if (_itemConfigs.TryGetValue(itemId, out var config))
                {
                    return config.Type;
                }
                return ItemType.Unknown;
            }
        }

        /// <summary>
        /// 根據物品ID獲取頁面類型
        /// </summary>
        public static PageIdType GetPageIdType(int itemId)
        {
            lock (_lock)
            {
                if (_itemConfigs.TryGetValue(itemId, out var config))
                {
                    return config.PageIdType;
                }
                return PageIdType.Other;
            }
        }

        /// <summary>
        /// 獲取所有物品設定（按類型分組）
        /// </summary>
        public static Dictionary<ItemType, List<ItemBaseModel>> GetItemConfigsByTypeGrouped()
        {
            lock (_lock)
            {
                return _itemConfigs.Values
                       .GroupBy(i => i.Type)
                       .ToDictionary(
                            typeGroup => typeGroup.Key,
                            typeGroup => typeGroup.OrderBy(i => i.Id).ToList()
                        );
            }
        }

        /// <summary>
        /// 獲取所有物品設定
        /// </summary>
        public static List<ItemBaseModel> GetAllItemConfigs()
        {
            lock (_lock)
            {
                return [.. _itemConfigs.Values.OrderBy(i => i.Type).ThenBy(i => i.Id)];
            }
        }

        /// <summary>
        /// 獲取物品表（用於日誌處理）
        /// </summary>
        public static Dictionary<int, ItemModel> GetItemTable()
        {
            lock (_lock)
            {
                return _itemConfigs.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ItemModel
                    {
                        ConfigBaseId = kvp.Value.Id,
                        Name = kvp.Value.Name,
                        Type = kvp.Value.Type
                    });
            }
        }
    }
}
