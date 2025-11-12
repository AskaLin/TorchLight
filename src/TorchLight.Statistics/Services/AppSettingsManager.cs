using Serilog;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private static JsonNode _originalSerilogConfig; // ✅ 儲存原始的 Serilog 設定

    /// <summary>
    /// 🆕 日誌路徑變更事件
    /// </summary>
    public static event Action<string> OnLogPathChanged;

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

                // ✅ 保存原始的 Serilog 設定
                try
                {
                    var jsonDoc = JsonNode.Parse(jsonContent);
                    if (jsonDoc != null && jsonDoc["Serilog"] != null)
                    {
                        _originalSerilogConfig = jsonDoc["Serilog"].DeepClone();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "無法解析原始 Serilog 設定");
                }

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
            // 🆕 檢查日誌路徑是否變更
            var oldPath = _settings?.Environment?.GameLogPath;
            var newPath = settings?.Environment?.GameLogPath;

            // ✅ 先序列化設定
            var jsonContent = JsonSerializer.Serialize(settings, _jsonOptions);

            // ✅ 如果有原始的 Serilog 設定，替換回去
            if (_originalSerilogConfig != null)
            {
                try
                {
                    var jsonDoc = JsonNode.Parse(jsonContent);
                    if (jsonDoc != null)
                    {
                        // 移除序列化產生的 serilog 屬性，替換為原始設定
                        jsonDoc["serilog"] = null;
                        jsonDoc["Serilog"] = _originalSerilogConfig.DeepClone();

                        // 重新序列化
                        jsonContent = jsonDoc.ToJsonString(new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "無法還原原始 Serilog 設定，將使用新的設定");
                }
            }

            File.WriteAllText(_settingsFilePath, jsonContent);

            _settings = settings;

            Log.Information("已儲存應用程式設定: {Path}", _settingsFilePath);

            // 🆕 如果日誌路徑變更，觸發事件
            if (oldPath != newPath && !string.IsNullOrWhiteSpace(newPath))
            {
                Log.Information("日誌路徑已變更：{OldPath} -> {NewPath}", oldPath ?? "(無)", newPath);
                OnLogPathChanged?.Invoke(newPath);
            }

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
