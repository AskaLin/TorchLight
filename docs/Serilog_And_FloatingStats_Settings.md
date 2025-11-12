# Serilog 與 FloatingStats 設定優化

## 📝 變更摘要

### 1. ✅ Serilog 設定改由 appsettings.json 管理

**修改目的**：讓日誌等級可由設定檔動態調整，無需重新編譯程式。

#### 新增設定

**檔案：** `src/TorchLight.Statistics/Models/AppSettings.cs`

```csharp
/// <summary>
/// Serilog 日誌設定
/// </summary>
public class SerilogSettings
{
    /// <summary>
    /// 最小日誌等級 (Verbose, Debug, Information, Warning, Error, Fatal)
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// 是否輸出到控制台
    /// </summary>
    public bool WriteToConsole { get; set; } = true;

    /// <summary>
    /// 是否輸出到檔案
    /// </summary>
    public bool WriteToFile { get; set; } = true;

    /// <summary>
    /// 日誌檔案路徑模板
    /// </summary>
    public string FilePathTemplate { get; set; } = "logs/torchlight-.txt";

    /// <summary>
    /// 日誌滾動間隔 (Infinite, Year, Month, Day, Hour, Minute)
    /// </summary>
    public string RollingInterval { get; set; } = "Day";
}
```

#### Program.cs 初始化變更

**修改前：**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/torchlight-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**修改後：**
```csharp
// 🆕 先載入應用程式設定（用於初始化 Serilog）
Services.AppSettingsManager.LoadSettings();
var settings = Services.AppSettingsManager.GetSettings();

// 🆕 根據 appsettings.json 初始化 Serilog
var loggerConfig = new LoggerConfiguration();

// 設定最小日誌等級
switch (settings.Serilog.MinimumLevel.ToLower())
{
    case "verbose":
        loggerConfig.MinimumLevel.Verbose();
        break;
    case "debug":
        loggerConfig.MinimumLevel.Debug();
        break;
    case "information":
    case "info":
        loggerConfig.MinimumLevel.Information();
        break;
    // ... 其他等級
}

// 設定輸出目標
if (settings.Serilog.WriteToConsole)
{
    loggerConfig.WriteTo.Console();
}

if (settings.Serilog.WriteToFile)
{
    var rollingInterval = // ... 解析 RollingInterval
    loggerConfig.WriteTo.File(
        settings.Serilog.FilePathTemplate,
        rollingInterval: rollingInterval
    );
}

Log.Logger = loggerConfig.CreateLogger();
```

#### appsettings.json 設定範例

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteToConsole": true,
    "WriteToFile": true,
    "FilePathTemplate": "logs/torchlight-.txt",
    "RollingInterval": "Day"
  }
}
```

#### 支援的日誌等級

| 等級 | 說明 | 用途 |
|------|------|------|
| `Verbose` | 最詳細的日誌 | 除錯深層問題 |
| `Debug` | 除錯資訊 | 開發階段除錯 |
| `Information` | 一般資訊 | **預設值**，正常執行狀態 |
| `Warning` | 警告訊息 | 潛在問題 |
| `Error` | 錯誤訊息 | 執行錯誤但程式可繼續 |
| `Fatal` | 嚴重錯誤 | 程式無法繼續執行 |

#### 支援的滾動間隔

| 間隔 | 說明 |
|------|------|
| `Infinite` | 不滾動（單一檔案） |
| `Year` | 每年一個檔案 |
| `Month` | 每月一個檔案 |
| `Day` | 每天一個檔案（**預設**） |
| `Hour` | 每小時一個檔案 |
| `Minute` | 每分鐘一個檔案 |

---

### 2. ✅ FloatingStatsWindow 新增座標記錄功能

**修改目的**：類似 ExecuteLineWindow，記住浮動統計窗體的位置、大小和顯示模式。

#### 新增設定

**檔案：** `src/TorchLight.Statistics/Models/AppSettings.cs`

```csharp
/// <summary>
/// 浮動統計窗體設定
/// </summary>
public class FloatingStatsSettings
{
    /// <summary>
    /// 視窗位置 X
    /// </summary>
    public int LocationX { get; set; } = 100;

    /// <summary>
    /// 視窗位置 Y
    /// </summary>
    public int LocationY { get; set; } = 100;

    /// <summary>
    /// 視窗寬度
    /// </summary>
    public int Width { get; set; } = 900;  // 預設橫列寬度

    /// <summary>
    /// 視窗高度
    /// </summary>
    public int Height { get; set; } = 50;  // 預設橫列高度

    /// <summary>
    /// 是否顯示
    /// </summary>
    public bool IsVisible { get; set; } = false;

    /// <summary>
    /// 透明度（0.0-1.0）
    /// </summary>
    public double Opacity { get; set; } = 0.9;

    /// <summary>
    /// 顯示模式（Vertical 或 Horizontal）
    /// </summary>
    public string DisplayMode { get; set; } = "Horizontal";  // 預設橫列
}
```

#### FloatingStatsWindow 新增方法

**檔案：** `src/TorchLight.Statistics/UI/FloatingStatsWindow.cs`

```csharp
/// <summary>
/// 🆕 獲取當前設定（用於儲存）
/// </summary>
public (Point location, Size size, double opacity, DisplayModePublic displayMode) GetSettings()
{
    return (Location, Size, Opacity, (DisplayModePublic)_displayMode);
}

/// <summary>
/// 🆕 套用設定（從 appsettings.json 載入）
/// </summary>
public void ApplySettings(Point location, Size size, double opacity, DisplayModePublic displayMode)
{
    Location = location;
    Size = size;
    Opacity = opacity;
    _displayMode = (DisplayMode)displayMode;

    RecalculateItemPositions();
    Invalidate();
}

/// <summary>
/// 🆕 公開 DisplayMode 列舉（用於序列化）
/// </summary>
public enum DisplayModePublic
{
    Vertical,
    Horizontal
}
```

#### MainWindow 載入和儲存設定

**載入設定（InitializeFloatingStatsWindow）：**
```csharp
// 🆕 載入設定
var settings = Services.AppSettingsManager.GetSettings();
var floatingSettings = settings.FloatingStats;

// 🆕 解析 DisplayMode
var displayMode = floatingSettings.DisplayMode.ToLower() == "vertical"
    ? FloatingStatsWindow.DisplayModePublic.Vertical
    : FloatingStatsWindow.DisplayModePublic.Horizontal;

// 🆕 套用設定
_floatingStatsWindow.ApplySettings(
    new Point(floatingSettings.LocationX, floatingSettings.LocationY),
    new Size(floatingSettings.Width, floatingSettings.Height),
    floatingSettings.Opacity,
    displayMode
);

// 🆕 根據設定決定是否顯示
if (floatingSettings.IsVisible)
{
    _floatingStatsWindow.Show();
}
else
{
    _floatingStatsWindow.Hide();
}
```

**儲存設定（Dispose）：**
```csharp
// 🆕 關閉前儲存浮動統計窗體的位置和大小
if (_floatingStatsWindow != null && !_floatingStatsWindow.IsDisposed)
{
    var (location, size, opacity, displayMode) = _floatingStatsWindow.GetSettings();

    var settings = Services.AppSettingsManager.GetSettings();
    settings.FloatingStats.LocationX = location.X;
    settings.FloatingStats.LocationY = location.Y;
    settings.FloatingStats.Width = size.Width;
    settings.FloatingStats.Height = size.Height;
    settings.FloatingStats.Opacity = opacity;
    settings.FloatingStats.DisplayMode = displayMode.ToString();
    settings.FloatingStats.IsVisible = _floatingStatsWindow.Visible;
    Services.AppSettingsManager.SaveSettings(settings);
}
```

#### appsettings.json 設定範例

```json
{
  "FloatingStats": {
    "LocationX": 100,
    "LocationY": 100,
    "Width": 900,
    "Height": 50,
    "IsVisible": false,
    "Opacity": 0.9,
    "DisplayMode": "Horizontal"
  }
}
```

#### 顯示模式

| 模式 | 說明 | 預設尺寸 |
|------|------|---------|
| `Horizontal` | 橫列模式（**預設**） | 900 x 50 |
| `Vertical` | 直列模式 | 100 x 400 |

---

## 📊 完整的 appsettings.json 範本

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteToConsole": true,
    "WriteToFile": true,
    "FilePathTemplate": "logs/torchlight-.txt",
    "RollingInterval": "Day"
  },
  "ExecuteLine": {
    "Stage1Percentage": 20,
    "Stage1Color": "#FF0000",
    "Stage2Percentage": 15,
    "Stage2Color": "#FFA500",
    "Stage3Percentage": 15,
    "Stage3Color": "#FFFF00",
    "DefaultColor": "#00FF00",
    "Opacity": 0.95,
    "LocationX": 100,
    "LocationY": 200,
    "Width": 1000,
    "Height": 30,
    "IsVisible": false
  },
  "FloatingStats": {
    "LocationX": 100,
    "LocationY": 100,
    "Width": 900,
    "Height": 50,
    "IsVisible": false,
    "Opacity": 0.9,
    "DisplayMode": "Horizontal"
  },
  "Environment": {
    "GameLogPath": ""
  }
}
```

---

## 🎯 使用方式

### 調整日誌等級

1. 開啟 `appsettings.json`
2. 修改 `Serilog.MinimumLevel` 的值
3. 重新啟動程式

**範例：開啟除錯模式**
```json
{
  "Serilog": {
    "MinimumLevel": "Debug"
  }
}
```

### 調整浮動統計窗體

1. 啟動程式並顯示浮動統計窗體
2. 拖曳、調整大小、切換顯示模式
3. 關閉程式時會自動儲存設定
4. 下次啟動會自動載入上次的位置和設定

### 雙擊切換顯示模式

- 在浮動統計窗體上**雙擊**可切換 `Horizontal` ↔ `Vertical`
- 切換後的模式會在關閉程式時自動儲存

---

## ✅ 已修改的檔案清單

| 檔案 | 變更內容 |
|------|---------|
| `Models/AppSettings.cs` | 新增 `SerilogSettings` 和 `FloatingStatsSettings` |
| `Program.cs` | 從 `appsettings.json` 初始化 Serilog |
| `UI/FloatingStatsWindow.cs` | 新增 `GetSettings()` 和 `ApplySettings()` 方法 |
| `UI/MainWindow.cs` | 載入和儲存 FloatingStatsWindow 設定 |
| `appsettings.json` | **新增檔案**，包含所有設定範本 |

---

## 🔄 測試建議

### Serilog 測試

1. **測試預設等級（Information）**
   - 啟動程式
   - 檢查 console 和 log 檔案
   - 應該看到 Information 以上的日誌

2. **測試 Debug 等級**
   - 修改 `appsettings.json`：`"MinimumLevel": "Debug"`
   - 重新啟動程式
   - 應該看到更多詳細的 Debug 日誌

3. **測試 Warning 等級**
   - 修改為 `"MinimumLevel": "Warning"`
   - 應該只看到 Warning、Error 和 Fatal

### FloatingStats 測試

1. **測試位置記憶**
   - 啟動程式並顯示浮動統計窗體
   - 拖曳到特定位置
   - 關閉程式並重新啟動
   - 檢查窗體是否出現在相同位置

2. **測試顯示模式記憶**
   - 雙擊切換為直列模式
   - 關閉程式並重新啟動
   - 檢查是否保持直列模式

3. **測試可見性記憶**
   - 顯示浮動統計窗體
   - 關閉程式並重新啟動
   - 檢查窗體是否自動顯示

---

## 📚 參考資料

- [Serilog Configuration](https://github.com/serilog/serilog/wiki/Configuration-Basics)
- [Serilog Level Control](https://github.com/serilog/serilog/wiki/Configuration-Basics#minimum-level)

---

**文件版本**: 1.0  
**最後更新**: 2025/01/XX  
**維護者**: TorchLight Statistics Team
