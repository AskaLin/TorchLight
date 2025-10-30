# mapInfo.json 檔案位置修正

## 問題說明

原本 `mapInfo.json` 被放置在方案根目錄，但正確的位置應該是專案根目錄。

## 修正內容

### 1. 檔案位置變更

**原位置（錯誤）：**
```
E:\SideProjects\TorchLight\mapInfo.json
```

**新位置（正確）：**
```
E:\SideProjects\TorchLight\src\TorchLight.Statistics\mapInfo.json
```

### 2. 檔案複製流程

建置時，`mapInfo.json` 會從專案目錄自動複製到輸出目錄：

```
專案目錄:
src/TorchLight.Statistics/mapInfo.json

↓ [建置]

輸出目錄 (Debug):
src/TorchLight.Statistics/bin/Debug/net8.0-windows/mapInfo.json

或

輸出目錄 (Release):
src/TorchLight.Statistics/bin/Release/net8.0-windows/mapInfo.json
```

### 3. 執行時路徑

應用程式使用 `AppContext.BaseDirectory` 來定位檔案：

```csharp
private static string ConfigFilePath => 
    Path.Combine(AppContext.BaseDirectory, "mapInfo.json");
```

- `AppContext.BaseDirectory` 指向執行檔所在目錄
- 開發模式：`src/TorchLight.Statistics/bin/Debug/net8.0-windows/`
- 發布模式：發布目錄

## 檔案讀寫流程

### 開發階段

1. **初始建置**
   ```
   src/TorchLight.Statistics/mapInfo.json
   ↓ [複製]
   bin/Debug/net8.0-windows/mapInfo.json
   ```

2. **應用程式啟動**
   - 從 `bin/Debug/net8.0-windows/mapInfo.json` 讀取
   - 啟動 FileSystemWatcher 監控此檔案

3. **前端修改設定**
   - 寫入 `bin/Debug/net8.0-windows/mapInfo.json`
   - **不會**影響專案目錄中的原始檔案

4. **手動編輯原始檔案**
   - 編輯 `src/TorchLight.Statistics/mapInfo.json`
   - **需要重新建置**才會複製到輸出目錄
   - **需要重啟應用程式**才會載入新設定

5. **手動編輯輸出檔案**
   - 編輯 `bin/Debug/net8.0-windows/mapInfo.json`
   - FileSystemWatcher **自動偵測**
   - **立即重新載入**，無需重啟

### 發布階段

1. **發布應用程式**
   ```
   src/TorchLight.Statistics/mapInfo.json
   ↓ [發布]
   PublishFolder/mapInfo.json
   ```

2. **使用者執行應用程式**
   - 從 `PublishFolder/mapInfo.json` 讀取
   - 所有修改都寫入此檔案
   - 檔案監控持續運作

## 重要提示

### ⚠️ 開發時的注意事項

1. **前端修改的設定不會回寫到專案目錄**
   - 前端修改只會寫入輸出目錄
   - 如果需要保存到原始碼，必須手動複製

2. **重新建置會覆蓋輸出目錄的檔案**
   - Clean 或 Rebuild 會用專案目錄的檔案覆蓋輸出目錄
   - 開發時的修改可能會遺失

3. **推薦的開發流程**
   - **方法一（測試用）：** 直接修改輸出目錄的 JSON，透過 FileSystemWatcher 即時生效
 - **方法二（正式用）：** 修改專案目錄的 JSON，重新建置並提交到版控

### ✅ 最佳實踐

1. **版本控制**
   ```
   # 將專案目錄的 mapInfo.json 加入版控
   git add src/TorchLight.Statistics/mapInfo.json
git commit -m "Update map configuration"
   ```

2. **同步開發修改**
 ```bash
   # 如果在輸出目錄測試了新設定，需要手動複製回專案目錄
 copy bin\Debug\net8.0-windows\mapInfo.json src\TorchLight.Statistics\mapInfo.json
   ```

3. **忽略輸出檔案**
   ```gitignore
   # .gitignore 中應該已經包含
   bin/
   obj/
   ```

## .csproj 設定說明

在 `TorchLight.Statistics.csproj` 中：

```xml
<ItemGroup>
  <None Update="mapInfo.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- `Update="mapInfo.json"` - 會尋找專案目錄下的檔案
- `CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` - 只在檔案較新時複製
- 每次建置時會檢查並複製到輸出目錄

## 檔案監控機制

```csharp
// MapMapper.cs
private static void StartFileWatcher()
{
    var directory = Path.GetDirectoryName(ConfigFilePath);
    var fileName = Path.GetFileName(ConfigFilePath);
    
 // ConfigFilePath = AppContext.BaseDirectory + "mapInfo.json"
    // 監控的是輸出目錄中的檔案
    _fileWatcher = new FileSystemWatcher(directory, fileName)
    {
      NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
        EnableRaisingEvents = true
    };
    
    _fileWatcher.Changed += OnConfigFileChanged;
}
```

**監控的檔案：** `bin/Debug/net8.0-windows/mapInfo.json`  
**不監控：** `src/TorchLight.Statistics/mapInfo.json`

## 測試驗證

### 驗證檔案位置

1. 建置專案
2. 檢查檔案是否存在：
   ```bash
   # 原始檔案
   dir src\TorchLight.Statistics\mapInfo.json
   
   # 輸出檔案
   dir src\TorchLight.Statistics\bin\Debug\net8.0-windows\mapInfo.json
   ```

### 驗證自動重新載入

1. 啟動應用程式
2. 用編輯器開啟 `bin\Debug\net8.0-windows\mapInfo.json`
3. 修改並儲存
4. 觀察應用程式日誌：
 ```
   [INF] 偵測到地圖設定檔變更，重新載入...
   [INF] 已載入地圖設定: X 個地圖
   ```
5. 前端會顯示通知訊息

### 驗證前端修改

1. 在前端設定頁面新增地圖
2. 檢查 `bin\Debug\net8.0-windows\mapInfo.json` 是否更新
3. 檢查 `src\TorchLight.Statistics\mapInfo.json` **不應該**變更

## 總結

- ✅ `mapInfo.json` 現在位於正確位置：專案根目錄
- ✅ 建置時自動複製到輸出目錄
- ✅ 執行時從輸出目錄讀取和寫入
- ✅ FileSystemWatcher 監控輸出目錄的檔案
- ✅ 所有功能正常運作

**記住：** 開發時如果需要保存設定到原始碼，必須手動將輸出目錄的 JSON 複製回專案目錄並提交版控。
