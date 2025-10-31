using Serilog;
using System.Runtime.InteropServices;
using System.Text.Json;
using TorchLight.Statistics.Enums;
using TorchLight.Statistics.Mapper;
using TorchLight.Statistics.Models;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics.UI;

/// <summary>
/// WebView2 API 橋接層 - 提供給 JavaScript 呼叫的 C# 方法
/// </summary>
[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class WebViewApi(MapPickRecordManager mapPickRecordManager, GameLogProcessor gameLogProcessor, MainWindow mainWindow)
{
    private readonly MapPickRecordManager _mapPickRecordManager = mapPickRecordManager;
    private readonly GameLogProcessor _gameLogProcessor = gameLogProcessor;
    private readonly MainWindow _mainWindow = mainWindow;

    private readonly JsonSerializerOptions _ops = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// 獲取所有地圖記錄
    /// </summary>
    public string GetMapRecords()
    {
        try
        {
            var records = _mapPickRecordManager.MapRecords.Select(r => GetMapRecord(r)).ToList();
            return JsonSerializer.Serialize(records, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取地圖記錄失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 獲取指定地圖的詳細資訊
    /// </summary>
    public string GetMapRecordDetail(string recordId)
    {
        try
        {
            var record = _mapPickRecordManager.MapRecords.FirstOrDefault(r => r.RecordId == recordId);

            if (record == null)
            {
                return JsonSerializer.Serialize(new { error = "找不到指定的記錄" });
            }

            return JsonSerializer.Serialize(GetMapRecord(record), _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取地圖記錄詳情失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static MapRecordDetail GetMapRecord(MapRecordModel model)
    {
        return new MapRecordDetail
        {
            RecordId = model.RecordId,
            Id = model.Id,
            Name = MapInfoMapper.GetMapName(model.Id), // 即時從 MapMapper 取得最新名稱
            MapTicket = model.MapTicket,
            Compass = [.. model.Compass.Where(c => !string.IsNullOrEmpty(c))],
            Probe = model.Probe,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            UseTime = model.UseTime,
            ItemCount = model.PickRecord?.Count ?? 0,
            TotalQuantity = model.PickRecord?.Sum(p => p.Value.Total) ?? 0,
            Items = model.PickRecord?.Select(p => new
            {
                p.Value.BaseId,
                p.Value.Name,
                p.Value.Total,
                p.Value.Slots
            }).OrderByDescending(i => i.Total).ToArray() ?? Array.Empty<object>()
        };
    }

    /// <summary>
    /// 獲取當前地圖資訊
    /// </summary>
    public string GetCurrentMapInfo()
    {
        var result = _gameLogProcessor.GetCurrentMapData();
        return JsonSerializer.Serialize(result, _ops);
    }

    /// <summary>
    /// 獲取統計資料
    /// </summary>
    public string GetStatistics()
    {
        try
        {
            var records = _mapPickRecordManager.MapRecords;

            var stats = new
            {
                TotalMaps = records.Count,
                TotalItems = records.Sum(r => r.PickRecord?.Count ?? 0),
                TotalQuantity = records.Select(r => r.PickRecord?.Sum(p => p.Value.Total) ?? 0).Sum(),
                TotalPlayTime = TimeSpan.FromSeconds(records.Sum(r => (r.EndTime - r.StartTime).TotalSeconds)).ToString(@"hh\:mm\:ss"),
                MostPickedItems = records.SelectMany(r => r.PickRecord?.Values ?? Enumerable.Empty<PickedItemDataModel>())
                                         .GroupBy(p => p.BaseId)
                                         .Select(g => new
                                         {
                                             BaseId = g.Key,
                                             g.First().Name,
                                             TotalQuantity = g.Sum(p => p.Total)
                                         }).OrderByDescending(i => i.TotalQuantity)
                                         .Take(10).ToArray()
            };

            return JsonSerializer.Serialize(stats, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取統計資料失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 清除所有記錄
    /// </summary>
    public string ClearAllRecords()
    {
        try
        {
            _mapPickRecordManager.Reset();
            Log.Information("已清除所有地圖記錄");
            return JsonSerializer.Serialize(new { success = true, message = "已清除所有記錄" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "清除記錄失敗");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 匯出記錄為 JSON
    /// </summary>
    public string ExportRecordsJson()
    {
        try
        {
            var records = _mapPickRecordManager.MapRecords;
            return JsonSerializer.Serialize(records, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "匯出記錄失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 最小化視窗
    /// </summary>
    public void MinimizeWindow()
    {
        _mainWindow.Invoke(() => _mainWindow.WindowState = FormWindowState.Minimized);
    }

    /// <summary>
    /// 關閉應用程式
    /// </summary>
    public void CloseApplication()
    {
        _mainWindow.Invoke(() => _mainWindow.Close());
    }

    /// <summary>
    /// 獲取所有地圖設定（按地圖類型分類）
    /// </summary>
    public string GetMapConfigs()
    {
        try
        {
            var configsByType = MapInfoMapper.GetAllMapConfigsByType();
            return JsonSerializer.Serialize(configsByType, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取地圖設定失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 新增或更新地圖設定
    /// </summary>
    public string SaveMapConfig(string mapId, string mapName, string mapType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mapId) || string.IsNullOrWhiteSpace(mapName))
            {
                return JsonSerializer.Serialize(new { success = false, message = "地圖ID和名稱不能為空" });
            }

            MapType type = mapType switch
            {
                "Hideout" => MapType.Hideout,
                "Netherrealm" => MapType.Netherrealm,
                "SecretRealm" => MapType.SecretRealm,
                "Boss" => MapType.Boss,
                _ => MapType.Unknown
            };

            var success = MapInfoMapper.AddOrUpdateMapMapping(mapId, mapName, type);

            return JsonSerializer.Serialize(new
            {
                success,
                message = success ? "地圖設定已儲存" : "儲存失敗"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "儲存地圖設定失敗");
            return JsonSerializer.Serialize(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 刪除地圖設定
    /// </summary>
    public string DeleteMapConfig(string mapId)
    {
        try
        {
            var success = MapInfoMapper.DeleteMapMapping(mapId);

            return JsonSerializer.Serialize(new
            {
                success,
                message = success ? "地圖設定已刪除" : "刪除失敗"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "刪除地圖設定失敗");
            return JsonSerializer.Serialize(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 獲取所有可用的地圖類型
    /// </summary>
    public string GetMapTypes()
    {
        try
        {
            var mapTypes = Enum.GetValues<MapType>()
                .Where(t => t != MapType.Unknown) // 排除 Unknown
                .Select(t => new
                {
                    Value = t.ToString(),
                    Name = t switch
                    {
                        MapType.Hideout => "🏠 藏身處",
                        MapType.Netherrealm => "🌌 異界",
                        MapType.SecretRealm => "🔮 秘境",
                        MapType.Boss => "👑 首領",
                        _ => t.ToString()
                    },
                    Description = t switch
                    {
                        MapType.Hideout => "玩家的安全區域",
                        MapType.Netherrealm => "可統計拾取的地圖",
                        MapType.SecretRealm => "特殊秘境地圖",
                        MapType.Boss => "首領地圖",
                        _ => ""
                    }
                }).ToArray();

            return JsonSerializer.Serialize(mapTypes, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取地圖類型失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 獲取所有 PageId 類型
    /// </summary>
    public string GetPageIdTypes()
    {
        try
        {
            var pageIdTypes = Enum.GetValues<PageIdType>()
                   .Select(t => new
                   {
                       Value = (int)t,
                       Name = t switch
                       {
                           PageIdType.Equipment => "⚔️ 裝備",
                           PageIdType.Skill => "✨ 技能",
                           PageIdType.Currency => "💰 通貨",
                           PageIdType.Other => "📦 其他",
                           _ => t.ToString()
                       },
                       Description = t switch
                       {
                           PageIdType.Equipment => "裝備類物品",
                           PageIdType.Skill => "技能類物品",
                           PageIdType.Currency => "通貨類物品",
                           PageIdType.Other => "其他類物品",
                           _ => ""
                       }
                   }).ToArray();

            return JsonSerializer.Serialize(pageIdTypes, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取 PageId 類型失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 獲取所有物品類型（ItemType）
    /// </summary>
    public string GetItemTypes()
    {
        try
        {
            var itemTypes = Enum.GetValues<ItemType>().Where(t => t != ItemType.Unknown)
                .Select(t => new
                {
                    Value = t.ToString(),
                    Name = t switch
                    {
                        ItemType.Currency => "💰 通貨",
                        ItemType.EquipmentMaterial => "⚙️ 裝備材料",
                        ItemType.MemoryMaterial => "🧩 追憶材料",
                        ItemType.CubeMaterial => "🎲 魔方材料",
                        ItemType.TowerMaterial => "🗼 高塔材料",
                        ItemType.DreamMaterial => "💭 夢語材料",
                        ItemType.CorrosionMaterial => "☠️ 侵蝕材料",
                        ItemType.OverlayMaterial => "🔷 疊界材料",
                        ItemType.SpecialItem => "✨ 特殊道具",
                        ItemType.DivinityContract => "📜 神格契約",
                        ItemType.GameplayTicket => "🎫 玩法門票",
                        ItemType.MapTicket => "🗺️ 地圖門票",
                        ItemType.BossTicket => "👑 BOSS門票",
                        ItemType.MemoryFirefly => "🔥 記憶螢光",
                        ItemType.DivinitySlate => "📖 神格石板",
                        ItemType.SkillItem => "⚡ 技能道具",
                        ItemType.Compass => "🧭 羅盤",
                        ItemType.Probe => "🛰️ 探針",
                        ItemType.DivineCrest => "🛡️ 神威紋章",
                        _ => t.ToString()
                    },
                    Description = t switch
                    {
                        ItemType.Currency => "基礎通貨類",
                        ItemType.EquipmentMaterial => "用於強化裝備的材料",
                        ItemType.MemoryMaterial => "追憶系統相關材料",
                        ItemType.CubeMaterial => "魔方系統相關材料",
                        ItemType.TowerMaterial => "高塔玩法相關材料",
                        ItemType.DreamMaterial => "夢語系統相關材料",
                        ItemType.CorrosionMaterial => "侵蝕系統相關材料",
                        ItemType.OverlayMaterial => "疊界系統相關材料",
                        ItemType.SpecialItem => "特殊功能道具",
                        ItemType.DivinityContract => "神格契約類物品",
                        ItemType.GameplayTicket => "各類玩法門票",
                        ItemType.MapTicket => "進入地圖的門票",
                        ItemType.BossTicket => "挑戰 BOSS 的門票",
                        ItemType.MemoryFirefly => "記憶螢光類物品",
                        ItemType.DivinitySlate => "神格石板類物品",
                        ItemType.SkillItem => "技能相關物品",
                        ItemType.Compass => "羅盤類物品",
                        ItemType.Probe => "探針類物品",
                        ItemType.DivineCrest => "神威紋章類物品",
                        _ => ""
                    }
                }).ToArray();

            return JsonSerializer.Serialize(itemTypes, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取物品類型失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 獲取 PageId 和 ItemType 的對應關係
    /// </summary>
    public string GetPageIdItemTypeMapping()
    {
        try
        {
            // 定義 PageIdType 和 ItemType 的對應關係
            var mapping = new Dictionary<int, List<string>>
            {
                // Equipment (100) - 裝備類
                [(int)PageIdType.Equipment] = [nameof(ItemType.DivinitySlate)],
                // Skill (101) - 技能類
                [(int)PageIdType.Skill] = [nameof(ItemType.SkillItem)],
                // Currency (102) - 通貨類
                [(int)PageIdType.Currency] =
                [
                    nameof(ItemType.Currency),
                    nameof(ItemType.EquipmentMaterial),
                    nameof(ItemType.MemoryMaterial),
                    nameof(ItemType.CubeMaterial),
                    nameof(ItemType.TowerMaterial),
                    nameof(ItemType.DreamMaterial),
                    nameof(ItemType.CorrosionMaterial),
                    nameof(ItemType.OverlayMaterial),
                    nameof(ItemType.SpecialItem),
                    nameof(ItemType.DivinityContract)
                ],
                // Other (103) - 其他類
                [(int)PageIdType.Other] =
                [
                    nameof(ItemType.GameplayTicket),
                    nameof(ItemType.MapTicket),
                    nameof(ItemType.BossTicket),
                    nameof(ItemType.MemoryFirefly),
                    nameof(ItemType.Compass),
                    nameof(ItemType.Probe),
                    nameof(ItemType.DivineCrest)
                ]
            };

            return JsonSerializer.Serialize(mapping, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取 PageId 和 ItemType 對應關係失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 獲取拾取統計設定（按 PageId 分類的物品列表）
    /// </summary>
    public string GetPickupStatisticsConfigs()
    {
        try
        {
            var allItems = ItemInfoMapper.GetAllItemConfigs();

            // 按 PageIdType 和 ItemType 雙層分組
            var configsByPageId = new Dictionary<int, Dictionary<string, List<object>>>();

            foreach (var item in allItems)
            {
                var pageId = (int)item.PageIdType;
                var itemType = item.Type.ToString();

                if (!configsByPageId.TryGetValue(pageId, out Dictionary<string, List<object>> value))
                {
                    value = [];
                    configsByPageId[pageId] = value;
                }

                if (!value.TryGetValue(itemType, out List<object> value1))
                {
                    value1 = [];
                    value[itemType] = value1;
                }

                value1.Add(new
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    PageId = pageId,
                    ItemType = itemType,
                    Enabled = item.Enable
                });
            }

            return JsonSerializer.Serialize(configsByPageId, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "獲取拾取統計設定失敗");
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 保存拾取統計項目（更新到 ItemInfo.json）
    /// </summary>
    public string SavePickupStatisticsItem(int itemId, string itemName, int pageId, bool enabled, string itemType)
    {
        try
        {
            // 從 ItemInfoMapper 獲取現有配置
            var allItems = ItemInfoMapper.GetAllItemConfigs();
            var existingItem = allItems.FirstOrDefault(i => i.Id == itemId);

            if (existingItem != null)
            {
                // 更新現有項目
                existingItem.Name = itemName;
                existingItem.PageIdType = (PageIdType)pageId;
                existingItem.Enable = enabled;

                // 更新物品類型
                if (!string.IsNullOrWhiteSpace(itemType) && Enum.TryParse<ItemType>(itemType, out var parsedItemType))
                {
                    existingItem.Type = parsedItemType;
                }

                // 儲存回 ItemInfo.json
                var success = ItemInfoMapper.SaveToJson();

                return JsonSerializer.Serialize(new
                {
                    success,
                    message = success ? "拾取統計項目已儲存" : "儲存失敗"
                }, _ops);
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "找不到指定的物品ID"
                }, _ops);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存拾取統計項目失敗");
            return JsonSerializer.Serialize(new { success = false, message = ex.Message }, _ops);
        }
    }

    /// <summary>
    /// 刪除拾取統計項目（將 Enable 設為 false）
    /// </summary>
    public string DeletePickupStatisticsItem(int itemId)
    {
        try
        {
            // 從 ItemInfoMapper 獲取現有配置
            var allItems = ItemInfoMapper.GetAllItemConfigs();
            var existingItem = allItems.FirstOrDefault(i => i.Id == itemId);

            if (existingItem != null)
            {
                // 將 Enable 設為 false
                existingItem.Enable = false;

                // 儲存回 ItemInfo.json
                var success = ItemInfoMapper.SaveToJson();

                return JsonSerializer.Serialize(new
                {
                    success,
                    message = success ? "拾取統計項目已停用" : "停用失敗"
                }, _ops);
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "找不到指定的物品ID"
                }, _ops);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "刪除拾取統計項目失敗");
            return JsonSerializer.Serialize(new { success = false, message = ex.Message }, _ops);
        }
    }

    /// <summary>
    /// 手動結算當前地圖（立即結束當前地圖記錄）
    /// </summary>
    public string SettleCurrentMap()
    {
        try
        {
            if (!_mapPickRecordManager.IsInMap)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "當前未在異界地圖中"
                }, _ops);
            }

            var currentMapName = _mapPickRecordManager.CurrentMapName;
            var endTime = DateTime.Now;

            // 結束當前地圖記錄
            _mapPickRecordManager.EndMapRecord(endTime);

            Log.Information("手動結算地圖: {MapName} 於 {Time}", currentMapName, endTime.ToString("yyyy/MM/dd HH:mm:ss"));

            // 通知前端更新當前地圖資訊
            _mainWindow.Invoke(async () =>
            {
                await _mainWindow.NotifyBagSyncAsync();
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = $"地圖「{currentMapName}」已結算完成",
                mapName = currentMapName,
                endTime
            }, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "手動結算地圖失敗");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = $"結算失敗: {ex.Message}"
            }, _ops);
        }
    }

    /// <summary>
    /// 🆕 顯示浮動統計窗體
    /// </summary>
    public string ShowFloatingStatsWindow()
    {
        try
        {
            _mainWindow.Invoke(() =>
            {
                _mainWindow.ShowFloatingWindow();
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "浮動窗體已顯示"
            }, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "顯示浮動窗體失敗");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = $"顯示失敗: {ex.Message}"
            }, _ops);
        }
    }

    /// <summary>
    /// 🆕 隱藏浮動統計窗體
    /// </summary>
    public string HideFloatingStatsWindow()
    {
        try
        {
            _mainWindow.Invoke(() =>
            {
                _mainWindow.HideFloatingWindow();
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "浮動窗體已隱藏"
            }, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "隱藏浮動窗體失敗");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = $"隱藏失敗: {ex.Message}"
            }, _ops);
        }
    }

    /// <summary>
    /// 🆕 切換浮動統計窗體顯示狀態
    /// </summary>
    public string ToggleFloatingStatsWindow()
    {
        try
        {
            bool isVisible = false;
            _mainWindow.Invoke(() =>
            {
                isVisible = _mainWindow.ToggleFloatingWindow();
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = isVisible ? "浮動窗體已顯示" : "浮動窗體已隱藏",
                isVisible
            }, _ops);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "切換浮動窗體失敗");
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = $"切換失敗: {ex.Message}"
            }, _ops);
        }
    }

    private class MapRecordDetail
    {
        public string RecordId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string MapTicket { get; set; }
        public string[] Compass { get; set; }
        public string Probe { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string UseTime { get; set; }
        public object[] Items { get; set; }
        public int ItemCount { get; set; }
        public int TotalQuantity { get; set; }
    }
}
