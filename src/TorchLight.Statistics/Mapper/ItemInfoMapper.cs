using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TorchLight.Statistics.Configuration;
using TorchLight.Statistics.Core;
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
        private static List<ItemBaseModel> _itemConfigs = [];
        private static ConfigFileWatcher<ItemBaseModel> _configWatcher;
        private static readonly JsonSerializerOptions _ops = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };             

        /// <summary>
        /// 設定檔路徑
        /// </summary>
        private static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "ItemInfo.json");

        /// <summary>
        /// 當物品設定更新時觸發
        /// </summary>
        public static event Action<bool, string> OnConfigUpdated;

        /// <summary>
        /// 初始化物品映射器（從 JSON 載入）
        /// </summary>
        public static void Initialize()
        {
            LoadFromJson();

            // 初始化檔案監控器
            _configWatcher = new ConfigFileWatcher<ItemBaseModel>(ConfigFilePath, LoadConfigsFromFile, OnConfigFileUpdated);
            _configWatcher.Initialize(_itemConfigs);
        }

        /// <summary>
        /// 讀取 ItemInfo.json 並回傳 Dictionary&lt;int, ItemModel&gt;
        /// </summary>
        public static Dictionary<int, ItemModel> GetItemTable()
        {
            lock (_lock)
            {
                return _itemConfigs.ToDictionary(
                    i => i.Id,
                    i => new ItemModel
                    {
                        ConfigBaseId = i.Id,
                        Name = i.Name,
                        Type = i.Type
                    });
            }
        }

        /// <summary>
        /// 從 JSON 檔案載入物品清單
        /// </summary>
        private static void LoadFromJson()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(ConfigFilePath))
                    {
                        Log.Warning("找不到物品設定檔: {Path}，使用預設設定", ConfigFilePath);
                        LoadDefaultConfig();
                        SaveToJson();
                        return;
                    }

                    var json = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                    var items = JsonSerializer.Deserialize<List<ItemBaseModel>>(json, _ops);

                    if (items == null || items.Count == 0)
                    {
                        Log.Error("物品設定檔格式錯誤或為空，使用預設設定");
                        LoadDefaultConfig();
                        return;
                    }

                    _itemConfigs = items;
                    Log.Information("已載入物品設定: {ItemCount} 個物品", _itemConfigs.Count);
                    OnConfigUpdated?.Invoke(true, "物品設定已成功載入");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "載入物品設定檔失敗，使用預設設定");
                    LoadDefaultConfig();
                    OnConfigUpdated?.Invoke(false, $"載入失敗: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 從檔案載入設定（供 ConfigFileWatcher 使用）
        /// </summary>
        private static List<ItemBaseModel> LoadConfigsFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<ItemBaseModel>>(json, _ops);

            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException("設定檔格式錯誤或為空");
            }

            lock (_lock)
            {
                _itemConfigs = items;
            }

            return items;
        }

        /// <summary>
        /// 設定檔更新回調
        /// </summary>
        private static void OnConfigFileUpdated(bool success, string message)
        {
            OnConfigUpdated?.Invoke(success, message);
        }

        /// <summary>
        /// 載入預設設定
        /// </summary>
        private static void LoadDefaultConfig()
        {
            _itemConfigs = [.. AppConfiguration.DefaultItemConfigs];
        }

        /// <summary>
        /// 儲存設定到 JSON 檔案
        /// </summary>
        public static bool SaveToJson()
        {
            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_itemConfigs, _ops);

                    // 暫時停止檔案監控
                    _configWatcher?.PauseWatching();

                    File.WriteAllText(ConfigFilePath, json, Encoding.UTF8);

                    // 恢復檔案監控
                    _configWatcher?.ResumeWatching();

                    Log.Information("物品設定已儲存至: {Path}", ConfigFilePath);
                    OnConfigUpdated?.Invoke(true, "物品設定已成功儲存");
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "儲存物品設定檔失敗");
                    OnConfigUpdated?.Invoke(false, $"儲存失敗: {ex.Message}");
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
                var config = _itemConfigs.FirstOrDefault(i => i.Id == itemId);
                return config?.Enable ?? false;
            }
        }

        /// <summary>
        /// 根據物品ID獲取物品名稱
        /// </summary>
        public static string GetItemName(int itemId)
        {
            lock (_lock)
            {
                var config = _itemConfigs.FirstOrDefault(i => i.Id == itemId);
                return config?.Name ?? $"未知物品 ({itemId})";
            }
        }

        /// <summary>
        /// 根據物品ID獲取物品類型
        /// </summary>
        public static ItemType GetItemType(int itemId)
        {
            lock (_lock)
            {
                var config = _itemConfigs.FirstOrDefault(i => i.Id == itemId);
                return config?.Type ?? ItemType.Unknown;
            }
        }

        /// <summary>
        /// 根據物品ID獲取頁面類型
        /// </summary>
        public static PageIdType GetPageIdType(int itemId)
        {
            lock (_lock)
            {
                var config = _itemConfigs.FirstOrDefault(i => i.Id == itemId);
                return config?.PageIdType ?? PageIdType.Other;
            }
        }

        /// <summary>
        /// 獲取所有物品設定
        /// </summary>
        public static List<ItemBaseModel> GetAllItemConfigs()
        {
            lock (_lock)
            {
                return [.. _itemConfigs.OrderBy(i => i.Type).ThenBy(i => i.Name)];
            }
        }

        /// <summary>
        /// 停止檔案監控
        /// </summary>
        public static void StopFileWatcher()
        {
            _configWatcher?.Dispose();
            _configWatcher = null;
        }
    }
}
