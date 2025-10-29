using Serilog;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics;

/// <summary>
/// ¥D¿ן³ז÷Þ²z¾¹
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
    /// ֵד¥Ü¥D¿ן³ז
    /// </summary>
public void ShowMainMenu()
    {
        Console.WriteLine("\nשÝשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששß");
  Console.WriteLine("שר     ¥D¿ן³ז Main Menu שר");
   Console.WriteLine("שאשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששג");
    Console.WriteLine("שר 1. ¦C¥X¦a¹ֿ¬B¨ת°O¿‎                    שר");
        Console.WriteLine("שר 2. ₪ֱ´«₪י»x¿י¥X [{0}]       שר", _consoleLogEnabled ? "¶}±ׂ" : "ֳצ³¬");
  Console.WriteLine("שר 3. ²M°£µe­±   שר");
   Console.WriteLine("שר 0. ×נ¦^÷Êֵ¥ / µ²§פµ{¦¡  שר");
        Console.WriteLine("שדשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששו");
     Console.Write("\n½׀¿ן¾Ü¥\¯א (0-3): ");
    }

    /// <summary>
    /// ³B²z¿ן³ז¿י₪J
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
      Console.WriteLine("µL®ִ×÷¿ן¶µ¡A½׀­«·s¿י₪J");
                break;
        }
    }

    /// <summary>
    /// ¦C¥X©ׂ¦³¦a¹ֿ°O¿‎
    /// </summary>
    private void ShowMapRecordList()
    {
        var records = _mapPickRecordManager.MapRecords;
    
        if (records.Count == 0)
      {
            Console.WriteLine("\n¥״«e¨S¦³¦a¹ֿ°O¿‎");
            Console.WriteLine("«צ Enter ×נ¦^¥D¿ן³ז...");
         Console.ReadLine();
            return;
        }

        Console.WriteLine("\nשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששש");
        Console.WriteLine("         ¦a¹ֿ¬B¨ת°O¿‎¦C×ם");
        Console.WriteLine("שששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששש");

        for (int i = 0; i < records.Count; i++)
        {
       var record = records[i];
            var ticketInfo = string.IsNullOrEmpty(record.MapTicket) ? "µL×ש²¼" : record.MapTicket;
   Console.WriteLine($"{i + 1}. [{record.StartTime:yyyy/MM/dd HH:mm:ss}] - {ticketInfo} - {record.Name}");
        }

   Console.WriteLine("שששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששש");
        Console.Write("\n¿י₪J½s¸¹¬d¬Ý¸װ²׃₪÷®e (0 ×נ¦^): ");
    
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
 Console.WriteLine("µL®ִ×÷½s¸¹");
       }
        }
        
        Console.WriteLine("\n«צ Enter ×נ¦^¥D¿ן³ז...");
        Console.ReadLine();
    }

    /// <summary>
    /// ֵד¥Ü¦a¹ֿ°O¿‎¸װ²׃₪÷®e
    /// </summary>
    private void ShowMapRecordDetail(Models.MapRecordModel record)
    {
     Console.WriteLine("\nשÝשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששß");
        Console.WriteLine("שר           ¦a¹ֿ°O¿‎¸װ²׃¸ך°T         שר");
      Console.WriteLine("שדשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששו");
        Console.WriteLine($"\n°O¿‎ID: {record.RecordId}");
        Console.WriteLine($"¦a¹ֿ¦W÷Ù: {record.Name}");
        Console.WriteLine($"¦a¹ֿID: {record.Id}");
        Console.WriteLine($"¶}©l®ֹ¶¡: {record.StartTime:yyyy/MM/dd HH:mm:ss}");
        Console.WriteLine($"µ²§פ®ֹ¶¡: {record.EndTime:yyyy/MM/dd HH:mm:ss}");
        Console.WriteLine($"¨ֿ¥־®ֹ¶¡: {record.UseTime}");
        
        if (!string.IsNullOrEmpty(record.MapTicket))
        {
            Console.WriteLine($"\n¨ֿ¥־×ש²¼: {record.MapTicket}");
      }
    
        if (record.Compass.Any(c => !string.IsNullOrEmpty(c)))
     {
            Console.WriteLine("\n¨ֿ¥־ֳ¹½L:");
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
       Console.WriteLine($"\n¨ֿ¥־±´°w: {record.Probe}");
        }

      if (record.PickRecord != null && record.PickRecord.Count > 0)
        {
            Console.WriteLine("\n¬B¨ת×««~²־­p:");
            Console.WriteLine("¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w");
      
  foreach (var item in record.PickRecord.OrderByDescending(x => x.Value.Total))
        {
        Console.WriteLine($"  {item.Value.Name,-30} x {item.Value.Total,5}");
            }
            
  Console.WriteLine("¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w¢w");
    Console.WriteLine($"¦@ {record.PickRecord.Count} ÷״×««~");
        }
        else
    {
    Console.WriteLine("\n¥»¦¸¦a¹ֿ¥¼¬B¨ת¥פ¦ף×««~");
     }
    }

    /// <summary>
    /// ₪ֱ´«±±¨מ¥x₪י»x¿י¥X
    /// </summary>
    private void ToggleConsoleLog()
    {
        _consoleLogEnabled = !_consoleLogEnabled;
        
        if (_consoleLogEnabled)
  {
            // ­«·s±ׂ¥־ Console sink
       Log.Logger = new LoggerConfiguration()
      .MinimumLevel.Debug()
       .WriteTo.Console()
    .WriteTo.File("logs/torchlight-.txt", rollingInterval: Serilog.RollingInterval.Day)
      .CreateLogger();
        Console.WriteLine("\n₪י»x¿י¥X₪w¶}±ׂ");
        }
  else
        {
          // ¥u«O¯d File sink
    Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/torchlight-.txt", rollingInterval: Serilog.RollingInterval.Day)
          .CreateLogger();
        Console.WriteLine("\n₪י»x¿י¥X₪wֳצ³¬¡]¶ָ¼g₪Jְֹ®׳¡^");
        }
        
     Console.WriteLine("«צ Enter ×נ¦^¥D¿ן³ז...");
        Console.ReadLine();
    }

    /// <summary>
    /// ²M°£µe­±¨ֳֵד¥Ü¿ן³ז
    /// </summary>
    private void ClearScreen()
    {
        Console.Clear();
     Console.WriteLine("שÝשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששß");
        Console.WriteLine("שר  ₪ץ¬²₪§¥תµL­­ - ¬B¨ת×««~²־­p₪u¨ד       שר");
        Console.WriteLine("שר  Torchlight Infinite Item Tracker      שר");
      Console.WriteLine("שדשששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששששו");
        Console.WriteLine("\nµe­±₪w²M°£");
     Console.WriteLine("«צ Enter ×נ¦^¥D¿ן³ז...");
   Console.ReadLine();
    }
}
