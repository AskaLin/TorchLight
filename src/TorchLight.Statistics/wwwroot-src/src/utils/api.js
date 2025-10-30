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

    case 'GetPageIdTypes':
      return [
  { value: 100, name: '⚔️ 裝備', description: '裝備類物品' },
   { value: 101, name: '✨ 技能', description: '技能類物品' },
     { value: 102, name: '💰 通貨', description: '通貨類物品' },
        { value: 103, name: '📦 其他', description: '其他類物品' }
      ]

    case 'GetItemTypes':
   return [
        { value: 'Currency', name: '💰 通貨', description: '基礎通貨類' },
        { value: 'EquipmentMaterial', name: '⚙️ 裝備材料', description: '用於強化裝備的材料' },
        { value: 'Compass', name: '🧭 羅盤', description: '羅盤類物品' },
        { value: 'DivinitySlate', name: '📖 神格石板', description: '神格石板類物品' }
      ]

    case 'GetPageIdItemTypeMapping':
      return {
        100: ['DivinitySlate'],
  101: ['SkillItem'],
        102: ['Currency', 'EquipmentMaterial', 'MemoryMaterial', 'CubeMaterial'],
        103: ['Compass', 'Probe', 'MemoryFirefly', 'GameplayTicket']
      }

    case 'GetPickupStatisticsConfigs':
      return {
        102: { // Currency
          Currency: [
            { itemId: 5011, itemName: '遺忘之水', pageId: 102, itemType: 'Currency', enabled: true },
      { itemId: 5028, itemName: '異界迴響', pageId: 102, itemType: 'Currency', enabled: true }
          ],
          EquipmentMaterial: [
            { itemId: 5080, itemName: '能量核心', pageId: 102, itemType: 'EquipmentMaterial', enabled: true },
            { itemId: 200003, itemName: '優質灰燼', pageId: 102, itemType: 'EquipmentMaterial', enabled: false }
          ]
        },
        103: { // Other
      Compass: [
        { itemId: 10001, itemName: '罪孽之劫掠羅盤', pageId: 103, itemType: 'Compass', enabled: true }
 ],
   MemoryFirefly: [
    { itemId: 6002, itemName: '寒淵的秘密', pageId: 103, itemType: 'MemoryFirefly', enabled: true }
        ]
        }
   }

    case 'SavePickupStatisticsItem':
      return { success: true, message: '拾取統計項目已儲存' }

    case 'DeletePickupStatisticsItem':
      return { success: true, message: '拾取統計項目已停用' }
    
    default:
      return {}
  }
}
