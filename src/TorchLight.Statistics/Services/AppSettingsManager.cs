using Serilog;
using System.Text.Json;
using TorchLight.Statistics.Models;

namespace TorchLight.Statistics.Services;

/// <summary>
/// 應用程式設定管理器
/// </summary>
public class AppSettingsManager
{
    private static readonly string _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static AppSettings _settings;

    /// <summary>
    /// 載入設定
    /// </summary>
    public static AppSettings LoadSettings()
    {
if (_settings != null)
        {
            return _settings;
      }

   try
    {
   if (File.Exists(_settingsFilePath))
    {
     var jsonContent = File.ReadAllText(_settingsFilePath);
         _settings = JsonSerializer.Deserialize<AppSettings>(jsonContent, _jsonOptions) ?? new AppSettings();
       Log.Information("已載入應用程式設定: {Path}", _settingsFilePath);
      }
else
      {
 _settings = new AppSettings();
Log.Information("設定檔不存在，使用預設設定");
       }
        }
     catch (Exception ex)
        {
Log.Error(ex, "載入設定檔失敗，使用預設設定");
       _settings = new AppSettings();
        }

  return _settings;
    }

  /// <summary>
    /// 儲存設定
    /// </summary>
    public static bool SaveSettings(AppSettings settings)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_settingsFilePath, jsonContent);

     _settings = settings;

 Log.Information("已儲存應用程式設定: {Path}", _settingsFilePath);
         return true;
        }
        catch (Exception ex)
  {
         Log.Error(ex, "儲存設定檔失敗");
       return false;
        }
    }

    /// <summary>
    /// 取得當前設定
    /// </summary>
    public static AppSettings GetSettings()
    {
        return _settings ?? LoadSettings();
    }
}
