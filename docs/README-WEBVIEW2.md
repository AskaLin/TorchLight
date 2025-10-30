# 火炬之光無限 - 拾取物品統計工具

這是一個使用 WebView2 + Vue3 + Vite 構建的遊戲拾取物品統計工具。

## 功能特色 

- ?? 自動監控遊戲日誌檔案
- ?? 實時統計異界地圖拾取物品
- ?? 記錄門票、羅盤、探針等開圖材料
- ?? 提供詳細的統計報表
- ?? 現代化的 UI 介面

## 技術架構

### 後端
- .NET 8
- WebView2
- WinForms

### 前端
- Vue 3
- Vite
- Pinia (狀態管理)
- Vue Router

## 開發環境設置

### 1. 安裝 Node.js 依賴

```bash
cd src/TorchLight.Statistics/wwwroot-src
npm install
```

### 2. 開發模式

在一個終端中啟動 Vite 開發伺服器：

```bash
cd src/TorchLight.Statistics/wwwroot-src
npm run dev
```

在另一個終端中啟動 .NET 應用程式：

```bash
cd src/TorchLight.Statistics
dotnet run
```

應用程式會自動連接到 Vite 開發伺服器 (http://localhost:5173)。

### 3. 生產構建

首先構建前端資源：

```bash
cd src/TorchLight.Statistics/wwwroot-src
npm run build
```

這會將構建的文件輸出到 `src/TorchLight.Statistics/wwwroot` 目錄。

然後構建 .NET 應用程式：

```bash
cd src/TorchLight.Statistics
dotnet publish -c Release
```

## 專案結構

```
src/TorchLight.Statistics/
├── UI/           # WebView2 UI 層
│   ├── MainWindow.cs           # 主視窗
│ └── WebViewApi.cs           # JavaScript 橋接 API
├── Services/        # 業務邏輯服務
├── Models/                 # 資料模型
├── wwwroot/          # 構建後的前端資源 (自動生成)
└── wwwroot-src/         # Vue3 前端源碼
    ├── src/
    │   ├── components/      # Vue 元件
    │   ├── views/      # 頁面視圖
    │   ├── stores/       # Pinia 狀態管理
    │   ├── router/# 路由配置
    │   ├── utils/              # 工具函數
    │   └── styles/             # 樣式文件
    ├── package.json
    ├── vite.config.js
    └── index.html
```

## API 接口

前端可以通過 `window.chrome.webview.hostObjects.csharpApi` 調用以下 C# 方法：

### 地圖記錄相關
- `GetMapRecords()` - 獲取所有地圖記錄
- `GetMapRecordDetail(recordId)` - 獲取地圖詳情
- `GetCurrentMapInfo()` - 獲取當前地圖資訊

### 統計相關
- `GetStatistics()` - 獲取統計資料

### 操作相關
- `ClearAllRecords()` - 清除所有記錄
- `ExportRecordsJson()` - 匯出記錄為 JSON
- `MinimizeWindow()` - 最小化視窗
- `CloseApplication()` - 關閉應用程式

## C# 調用 JavaScript

從 C# 可以調用以下 JavaScript 函數：

- `window.onNewMapRecord()` - 當有新地圖記錄時觸發
- `window.onItemPicked(itemName, quantity)` - 當拾取物品時觸發

## 常見問題

### Q: WebView2 初始化失敗？
A: 請確保已安裝 Microsoft Edge WebView2 Runtime。可以從 [Microsoft 官網](https://developer.microsoft.com/microsoft-edge/webview2/) 下載。

### Q: 開發模式下無法連接到 Vite 伺服器？
A: 確保 Vite 開發伺服器正在運行 (http://localhost:5173)，並檢查防火牆設置。

### Q: 構建後找不到 wwwroot 資源？
A: 確保先執行 `npm run build` 構建前端資源。

## 授權

MIT License

## 貢獻

歡迎提交 Issue 和 Pull Request！
