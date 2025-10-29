using TorchLight.Statistics.Services;

namespace TorchLight.Statistics;

/// <summary>
/// 控制台日誌輸出器
/// </summary>
public class ConsoleLogger
{
    /// <summary>
    /// 記錄背包物品修改
    /// </summary>
    public void LogBagModification(BagModEvent ev, ItemChangeResult result)
    {
        Console.WriteLine($"{ev.Time:yyyy/MM/dd HH:mm:ss.fff}\t{ev.ProtoName} {ev.Action} - {result.ItemName}({result.ConfigBaseId}) 在 slot:{ev.SlotId} 有 {result.NewSlotCount} 個");

        if (result.IsNewItem)
        {
            Console.WriteLine($"\t\t\t這是新物品，撿到 {result.QuantityChange} 個\r\n");
            return;
        }

        Console.WriteLine($"\t\t\t背包之前有 {result.ItemName} {result.PreviousTotalCount} 個");

        if (result.IsNewSlot)
        {
            Console.WriteLine($"\t\t\t這是新欄位，撿到 {result.QuantityChange} 個，現在全部有 {result.NewTotalCount} 個\r\n");
        }
        else
        {
            if (ev.ProtoName == "Spv3Open")
            {
                Console.WriteLine($"\t\t\t原本在 slot:{ev.SlotId}, 有 {result.PreviousSlotCount} 個, 所以是使用 {Math.Abs(result.QuantityChange)} 個, 現在剩下 {result.NewTotalCount} 個\r\n");
            }
            else
            {
                Console.WriteLine($"\t\t\t其中在 slot:{ev.SlotId}, 有 {result.PreviousSlotCount} 個, 所以是撿到 {result.QuantityChange} 個, 現在全部有 {result.NewTotalCount} 個\r\n");
            }
        }
    }

    /// <summary>
    /// 記錄地圖拾取物品
    /// </summary>
    public void LogMapPickItem(string mapName, MapPickResult result)
    {
        Console.Write($"在 {mapName} 地圖");

        if (result.IsFirstTimeInMap)
        {
            Console.WriteLine($"\t第一次在這張地圖撿到 {result.ItemName} {result.NewTotalCount} 個 (slot:{result.SlotId} = {result.NewSlotCount})\r\n");
        }
        else if (result.IsNewSlot)
        {
            Console.WriteLine($"\t該物品在此欄位之前沒有數量，這次新增 {result.QuantityChange} 個 (slot:{result.SlotId})");
            Console.WriteLine($"\t目前地圖上 {result.ItemName} 總數量: {result.NewTotalCount}\r\n");
        }
        else if (result.IsExistingSlot)
        {
            Console.WriteLine($"\t該物品在此欄位之前有 {result.PreviousSlotCount} 個，這次增加 {result.QuantityChange} 個，現在該欄位有 {result.NewSlotCount} 個");
            Console.WriteLine($"\t\t\t目前地圖上 {result.ItemName} 總數量: {result.NewTotalCount}\r\n");
        }
    }
}
