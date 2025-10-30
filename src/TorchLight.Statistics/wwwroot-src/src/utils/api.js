/**
 * API 呼叫工具 - 與 C# WebView API 通訊
 */

export async function apiCall(methodName, ...args) {
  try {
    // 檢查是否在 WebView2 環境中
    if (window.chrome && window.chrome.webview) {
 // 透過 C# API 呼叫
    const result = await window.chrome.webview.hostObjects.csharpApi[methodName](...args)
      
   // 解析 JSON 結果
      if (typeof result === 'string') {
        const data = JSON.parse(result)
        if (data.error) {
          throw new Error(data.error)
        }
        return data
      }
      
      return result
    } else {
      // 開發模式：使用模擬資料
      console.warn(`API call in dev mode: ${methodName}`, args)
      return getMockData(methodName, args)
    }
  } catch (error) {
    console.error(`API call failed: ${methodName}`, error)
    throw error
  }
}

/**
 * 開發模式的模擬資料
 */
function getMockData(methodName, args) {
  switch (methodName) {
    case 'GetMapRecords':
      return [
        {
       recordId: '123e4567-e89b-12d3-a456-426614174000',
          id: 'GeBuLinCunLuo01',
          name: '隔壁林村落01',
          mapTicket: '悲鳴礦區門票',
          compass: ['羅盤1', '羅盤2'],
          probe: '探針A',
        startTime: '2025-10-29T03:31:16',
      endTime: '2025-10-29T03:45:20',
     useTime: '00:14:04',
          itemCount: 15,
        totalQuantity: 245
 }
      ]
    
    case 'GetCurrentMapInfo':
      return {
        isInMap: false,
      mapName: ''
      }
    
    case 'GetMapRecordDetail':
    return {
        recordId: args[0],
        id: 'GeBuLinCunLuo01',
        name: '隔壁林村落01',
        mapTicket: '悲鳴礦區門票',
        compass: ['羅盤1', '羅盤2'],
        probe: '探針A',
        startTime: '2025-10-29T03:31:16',
        endTime: '2025-10-29T03:45:20',
        useTime: '00:14:04',
        items: [
     { baseId: 1001, name: '命運卡片', total: 50, slots: { 1: 50 } },
          { baseId: 1002, name: '通貨', total: 100, slots: { 2: 100 } },
   { baseId: 1003, name: '裝備', total: 95, slots: { 3: 45, 4: 50 } }
    ]
  }
    
  case 'GetStatistics':
      return {
        totalMaps: 10,
   totalItems: 150,
        totalQuantity: 2450,
   totalPlayTime: '02:30:45',
        mostPickedItems: [
       { baseId: 1001, name: '命運卡片', totalQuantity: 500 },
      { baseId: 1002, name: '通貨', totalQuantity: 1000 }
        ]
   }
    
    default:
      return {}
  }
}
