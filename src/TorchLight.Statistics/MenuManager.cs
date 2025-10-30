using Serilog;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics;

/// <summary>
/// 主選單管理器
/// </summary>
public class MenuManager
{
    private readonly MapPickRecordManager _mapPickRecordManager;
    private bool _isRunning = true;
    private bool _consoleLogEnabled = true;

    public MenuManager(MapPickRecordManager mapPickRecordManager)
    {
        _mapPickRecordManager = mapPickRecordManager;
    }

    public bool IsRunning => _isRunning;

    /// <summary>
    /// 顯示主選單
    /// </summary>
public void ShowMainMenu()
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
  Console.WriteLine("║     主選單 Main Menu ║");
   Console.WriteLine("╠════════════════════════════════════════╣");
    Console.WriteLine("║ 1. 列出地圖拾取記錄                    ║");
        Console.WriteLine("║ 2. 切換日誌輸出 [{0}]       ║", _consoleLogEnabled ? "開啟" : "關閉");
  Console.WriteLine("║ 3. 清除畫面   ║");
   Console.WriteLine("║ 0. 返回監聽 / 結束程式  ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
     Console.Write("\n請選擇功能 (0-3): ");
    }

    /// <summary>
    /// 處理選單輸入
    /// </summary>
    public void HandleInput(string input)
    {
    switch (input?.Trim())
        {
            case "1":
      ShowMapRecordList();
           break;
     case "2":
       ToggleConsoleLog();
    break;
        case "3":
          ClearScreen();
          break;
            case "0":
     _isRunning = false;
     break;
  default:
      Console.WriteLine("無效的選項，請重新輸入");
                break;
        }
    }

    /// <summary>
    /// 列出所有地圖記錄
    /// </summary>
    private void ShowMapRecordList()
    {
        var records = _mapPickRecordManager.MapRecords;
    
        if (records.Count == 0)
      {
            Console.WriteLine("\n目前沒有地圖記錄");
            Console.WriteLine("按 Enter 返回主選單...");
         Console.ReadLine();
            return;
        }

        Console.WriteLine("\n════════════════════════════════════════");
        Console.WriteLine("         地圖拾取記錄列表");
        Console.WriteLine("════════════════════════════════════════");

        for (int i = 0; i < records.Count; i++)
        {
       var record = records[i];
            var ticketInfo = string.IsNullOrEmpty(record.MapTicket) ? "無門票" : record.MapTicket;
   Console.WriteLine($"{i + 1}. [{record.StartTime:yyyy/MM/dd HH:mm:ss}] - {ticketInfo} - {record.Name}");
        }

   Console.WriteLine("════════════════════════════════════════");
        Console.Write("\n輸入編號查看詳細內容 (0 返回): ");
    
        if (int.TryParse(Console.ReadLine(), out int selection))
        {
  if (selection == 0)
   {
         return;
       }
            else if (selection > 0 && selection <= records.Count)
       {
           ShowMapRecordDetail(records[selection - 1]);
          }
       else
            {
 Console.WriteLine("無效的編號");
       }
        }
        
        Console.WriteLine("\n按 Enter 返回主選單...");
        Console.ReadLine();
    }

    /// <summary>
    /// 顯示地圖記錄詳細內容
    /// </summary>
    private void ShowMapRecordDetail(Models.MapRecordModel record)
    {
     Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║           地圖記錄詳細資訊         ║");
      Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine($"\n記錄ID: {record.RecordId}");
        Console.WriteLine($"地圖名稱: {record.Name}");
        Console.WriteLine($"地圖ID: {record.Id}");
        Console.WriteLine($"開始時間: {record.StartTime:yyyy/MM/dd HH:mm:ss}");
        Console.WriteLine($"結束時間: {record.EndTime:yyyy/MM/dd HH:mm:ss}");
        Console.WriteLine($"使用時間: {record.UseTime}");
        
        if (!string.IsNullOrEmpty(record.MapTicket))
        {
            Console.WriteLine($"\n使用門票: {record.MapTicket}");
      }
    
        if (record.Compass.Any(c => !string.IsNullOrEmpty(c)))
     {
            Console.WriteLine("\n使用羅盤:");
         for (int i = 0; i < record.Compass.Length; i++)
      {
   if (!string.IsNullOrEmpty(record.Compass[i]))
 {
      Console.WriteLine($"  {i + 1}. {record.Compass[i]}");
     }
      }
        }
      
        if (!string.IsNullOrEmpty(record.Probe))
        {
       Console.WriteLine($"\n使用探針: {record.Probe}");
        }

      if (record.PickRecord != null && record.PickRecord.Count > 0)
        {
            Console.WriteLine("\n拾取物品統計:");
            Console.WriteLine("────────────────────────────────────────");
      
  foreach (var item in record.PickRecord.OrderByDescending(x => x.Value.Total))
        {
        Console.WriteLine($"  {item.Value.Name,-30} x {item.Value.Total,5}");
            }
            
  Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine($"共 {record.PickRecord.Count} 種物品");
        }
        else
    {
    Console.WriteLine("\n本次地圖未拾取任何物品");
     }
    }

    /// <summary>
    /// 切換控制台日誌輸出
    /// </summary>
    private void ToggleConsoleLog()
    {
        _consoleLogEnabled = !_consoleLogEnabled;
        
        if (_consoleLogEnabled)
  {
            // 重新啟用 Console sink
       Log.Logger = new LoggerConfiguration()
      .MinimumLevel.Debug()
       .WriteTo.Console()
    .WriteTo.File("logs/torchlight-.txt", rollingInterval: Serilog.RollingInterval.Day)
      .CreateLogger();
        Console.WriteLine("\n日誌輸出已開啟");
        }
  else
        {
          // 只保留 File sink
    Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/torchlight-.txt", rollingInterval: Serilog.RollingInterval.Day)
          .CreateLogger();
        Console.WriteLine("\n日誌輸出已關閉（僅寫入檔案）");
        }
        
     Console.WriteLine("按 Enter 返回主選單...");
        Console.ReadLine();
    }

    /// <summary>
    /// 清除畫面並顯示選單
    /// </summary>
    private void ClearScreen()
    {
        Console.Clear();
     Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║  火炬之光無限 - 拾取物品統計工具       ║");
        Console.WriteLine("║  Torchlight Infinite Item Tracker      ║");
      Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine("\n畫面已清除");
     Console.WriteLine("按 Enter 返回主選單...");
   Console.ReadLine();
    }
}
