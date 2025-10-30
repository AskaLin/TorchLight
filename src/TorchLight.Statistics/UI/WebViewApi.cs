using System.Runtime.InteropServices;
using System.Text.Json;
using Serilog;
using TorchLight.Statistics.Models;
using TorchLight.Statistics.Services;
using TorchLight.Statistics.Core;

namespace TorchLight.Statistics.UI;

/// <summary>
/// WebView2 API 橋接層 - 提供給 JavaScript 呼叫的 C# 方法
/// </summary>
[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class WebViewApi
{
    private readonly MapPickRecordManager _mapPickRecordManager;
    private readonly GameLogProcessor _gameLogProcessor;
private readonly MainWindow _mainWindow;

    public WebViewApi(MapPickRecordManager mapPickRecordManager, GameLogProcessor gameLogProcessor, MainWindow mainWindow)
    {
        _mapPickRecordManager = mapPickRecordManager;
  _gameLogProcessor = gameLogProcessor;
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// 獲取所有地圖記錄
    /// </summary>
    public string GetMapRecords()
    {
  try
        {
       var records = _mapPickRecordManager.MapRecords
     .Select(r => new
  {
    r.RecordId,
          r.Id,
          Name = MapMapper.GetMapName(r.Id), // 即時從 MapMapper 取得最新名稱
     r.MapTicket,
        Compass = r.Compass.Where(c => !string.IsNullOrEmpty(c)).ToArray(),
          r.Probe,
     r.StartTime,
 r.EndTime,
          r.UseTime,
          ItemCount = r.PickRecord?.Count ?? 0,
     TotalQuantity = r.PickRecord?.Sum(p => p.Value.Total) ?? 0
})
    .ToList();

            return JsonSerializer.Serialize(records, new JsonSerializerOptions 
          {
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
   WriteIndented = true
 });
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
    public string GetMapRecordDetail(string recordIdStr)
    {
  try
     {
        if (!Guid.TryParse(recordIdStr, out var recordId))
    {
return JsonSerializer.Serialize(new { error = "無效的記錄ID" });
 }

            var record = _mapPickRecordManager.MapRecords
 .FirstOrDefault(r => r.RecordId == recordId);

   if (record == null)
     {
  return JsonSerializer.Serialize(new { error = "找不到指定的記錄" });
       }

   var detail = new
     {
     record.RecordId,
record.Id,
     Name = MapMapper.GetMapName(record.Id), // 即時從 MapMapper 取得最新名稱
            record.MapTicket,
   Compass = record.Compass.Where(c => !string.IsNullOrEmpty(c)).ToArray(),
     record.Probe,
 record.StartTime,
  record.EndTime,
    record.UseTime,
       Items = record.PickRecord?.Select(p => new
        {
         p.Value.BaseId,
    p.Value.Name,
       p.Value.Total,
   Slots = p.Value.Slots
      }).OrderByDescending(i => i.Total).ToArray() ?? Array.Empty<object>()
  };

   return JsonSerializer.Serialize(detail, new JsonSerializerOptions 
   {
   PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
     WriteIndented = true
   });
      }
        catch (Exception ex)
   {
     Log.Error(ex, "獲取地圖記錄詳情失敗");
    return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 獲取當前地圖資訊
    /// </summary>
    public string GetCurrentMapInfo()
    {
     try
        {
 var currentRecord = _mapPickRecordManager.GetCurrentMapRecord();
   
      if (!_mapPickRecordManager.IsInNetherrealmMap)
       {
  // 避難所地圖 - 只顯示地圖名稱
    return JsonSerializer.Serialize(new
    {
   IsInMap = false,
    MapType = "Hideout",
     MapName = _mapPickRecordManager.CurrentMapName
       }, new JsonSerializerOptions 
   {
PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });
     }
  else if (currentRecord != null)
  {
// 異界地圖 - 顯示完整資訊
  return JsonSerializer.Serialize(new
      {
   IsInMap = true,
     MapType = "Netherrealm",
   MapName = MapMapper.GetMapName(currentRecord.Id), // 即時取得最新名稱
         RecordId = currentRecord.RecordId,
           MapTicket = currentRecord.MapTicket,
   Compass = currentRecord.Compass.Where(c => !string.IsNullOrEmpty(c)).ToArray(),
       Probe = currentRecord.Probe,
        StartTime = currentRecord.StartTime,
     Items = currentRecord.PickRecord?.Select(p => new
         {
 p.Value.BaseId,
       p.Value.Name,
 p.Value.Total,
   Slots = p.Value.Slots
    }).OrderByDescending(i => i.Total).ToArray() ?? Array.Empty<object>()
      }, new JsonSerializerOptions 
    {
           PropertyNamingPolicy = JsonNamingPolicy.CamelCase
   });
       }
     else
{
      // 在異界地圖但沒有記錄
  return JsonSerializer.Serialize(new
     {
       IsInMap = true,
 MapType = "Netherrealm",
     MapName = _mapPickRecordManager.CurrentMapName
     }, new JsonSerializerOptions 
{
  PropertyNamingPolicy = JsonNamingPolicy.CamelCase
   });
  }
      }
      catch (Exception ex)
        {
    Log.Error(ex, "獲取當前地圖資訊失敗");
    return JsonSerializer.Serialize(new { error = ex.Message });
        }
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
   TotalQuantity = records.Sum(r => r.PickRecord?.Sum(p => p.Value.Total) ?? 0),
       TotalPlayTime = TimeSpan.FromSeconds(
         records.Sum(r => (r.EndTime - r.StartTime).TotalSeconds)
    ).ToString(@"hh\:mm\:ss"),
   MostPickedItems = records
         .SelectMany(r => r.PickRecord?.Values ?? Enumerable.Empty<PickedItemDataModel>())
    .GroupBy(p => p.BaseId)
   .Select(g => new
 {
    BaseId = g.Key,
     Name = g.First().Name,
    TotalQuantity = g.Sum(p => p.Total)
    })
.OrderByDescending(i => i.TotalQuantity)
  .Take(10)
  .ToArray()
  };

  return JsonSerializer.Serialize(stats, new JsonSerializerOptions 
   {
 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
     WriteIndented = true
  });
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
            return JsonSerializer.Serialize(records, new JsonSerializerOptions 
            {
 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
     WriteIndented = true
  });
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
    /// 獲取所有地圖設定
    /// </summary>
    public string GetMapConfigs()
    {
        try
 {
          var configs = MapMapper.GetAllMapConfigs();
  return JsonSerializer.Serialize(configs, new JsonSerializerOptions
            {
     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
       WriteIndented = true
   });
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
            _ => MapType.Unknown
      };

        var success = MapMapper.AddOrUpdateMapMapping(mapId, mapName, type);
       
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
var success = MapMapper.DeleteMapMapping(mapId);
         
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
}
