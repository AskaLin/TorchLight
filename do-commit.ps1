$commitMessage = @"
feat: 地圖設定管理系統與前端介面優化

## 主要功能

### 地圖設定管理系統
- 新增動態地圖設定載入機制 (mapInfo.json)
- 前端設定管理頁面 (MapSettings.vue)
- 檔案監控與自動重新載入
- 錯誤處理與備份恢復機制

### 當前地圖資訊顯示
- 新增 CurrentMapInfo 元件
- 即時顯示當前地圖狀態與拾取統計

### 前端介面優化
- 地圖詳情: 網格佈局 (每行4列)
- 物品名稱與數量不換行
- 統計頁面: Top 10 兩列佈局
- 總地圖數可點擊跳轉

### 地圖設定同步修復
- 前端自動響應設定更新
- API 即時查詢最新地圖名稱

## 技術改進
- 重構 MapMapper 為動態載入
- WebView2 雙向訊息通知
- 優化前端狀態管理

## 變更檔案
- 新增: 4個程式碼檔案 + 7個文檔
- 修改: 13個檔案
- 刪除: IdTable.conf
"@

# 執行 git add
Write-Host "Adding files..." -ForegroundColor Yellow
git add .

# 執行 commit
Write-Host "Creating commit..." -ForegroundColor Yellow
git commit -m $commitMessage

# 顯示狀態
Write-Host "`nCommit completed!" -ForegroundColor Green
Write-Host "`nLatest commit:" -ForegroundColor Cyan
git log --oneline -1

Write-Host "`nGit status:" -ForegroundColor Cyan
git status
