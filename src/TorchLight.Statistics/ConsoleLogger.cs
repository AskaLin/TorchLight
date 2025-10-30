using Serilog;
using TorchLight.Statistics.Services;

namespace TorchLight.Statistics;

/// <summary>
/// 控制台日誌輸出器
/// </summary>
public class ConsoleLogger
{
    /// <summary>
    /// 記錄背包物品修改事件
    /// </summary>
    public void LogBagModification(BagModEvent ev, ItemChangeResult result)
    {
        // Debug: 詳細資訊
        Log.Debug("[{Protocol}] {Action} - 物品: {ItemName}({ItemId}), Slot: {SlotId}, 數量: {Count}",
            ev.ProtoName, ev.Action, result.ItemName, result.ConfigBaseId, ev.SlotId, result.NewSlotCount);

        Log.Debug("  變化詳情: 前={PrevTotal}, 變化={Change}, 後={NewTotal}",
            result.PreviousTotalCount, result.QuantityChange, result.NewTotalCount);

        // Info: 簡單結果
        if (result.IsNewItem)
        {
            Log.Information("[背包] 新增 {ItemName} x{Count}", result.ItemName, result.QuantityChange);
            return;
        }

        string action = result.QuantityChange > 0 ? "增加" : "減少";
        Log.Information("[背包] {ItemName} {Action} {Change} (總計: {Total})",
            result.ItemName, action, Math.Abs(result.QuantityChange), result.NewTotalCount);
    }

    /// <summary>
    /// 記錄地圖拾取物品
    /// </summary>
    public void LogMapPickItem(string mapName, MapPickResult result)
    {
        // Debug: 詳細資訊
        Log.Debug("[地圖拾取] 地圖: {MapName}, 物品: {ItemName}, Slot: {SlotId}",
            mapName, result.ItemName, result.SlotId);
        Log.Debug("  拾取詳情: 前={PrevSlot}, 增加={Change}, 後={NewSlot}, 總計={Total}",
            result.PreviousSlotCount, result.QuantityChange, result.NewSlotCount, result.NewTotalCount);

        // Info: 簡單結果
        if (result.IsFirstTimeInMap)
        {
            Log.Information("[{MapName}] 拾取 {ItemName} x{Count}",
                mapName, result.ItemName, result.NewTotalCount);
        }
        else
        {
            Log.Information("[{MapName}] {ItemName} +{Change} (總計: {Total})",
                mapName, result.ItemName, result.QuantityChange, result.NewTotalCount);
        }
    }
}
