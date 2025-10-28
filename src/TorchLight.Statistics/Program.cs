using System.Text;
using TorchLight.Statistics;
using TorchLight.Statistics.Models;

Console.WriteLine("Hello, World!");

Dictionary<int, BagDataModel> tempBagData = [];
Dictionary<int, BagDataModel> mapPickItemData = [];
var idTable = IdTable.GetIdTable();
var logFormater = new LogFormater();

var proc = new ItemChangeBlockProcessor();
// 即時模式：每讀到一條 BagMgr（且在 PickItems 區塊內）就觸發
proc.OnBagModInsideBlock += ev =>
{
    var itemModel = new ItemModel
    {
        Name = idTable.TryGetValue(ev.ConfigBaseId, out string itemName) ? itemName : "未知的物品",
        Num = ev.Num
    };
    Console.WriteLine($"\t\t{ev.Time:yyyy/MM/dd HH:mm:ss.fff} {itemModel.Name} 在 slot:{ev.SlotId} 有 {itemModel.Num} 個");

    if (tempBagData.TryGetValue(ev.ConfigBaseId, out BagDataModel value))
    {
        Console.WriteLine($"\t\t背包之前有 {value.ItemName} {value.Total} 個");
        if (value.Slots.TryGetValue(ev.SlotId, out int value2))
        {
            int newNum = ev.Num - value2;            
            value.Slots[ev.SlotId] = ev.Num;
            value.Total += newNum;
            itemModel.Num = newNum;
            Console.WriteLine($"\t\t其中在 solt:{ev.SlotId}, 有 {value2} 個, 所以是撿到 {newNum} 個, 現在全部有 {value.Total} 個\r\n\r\n");
        }
        else
        {
            value.Slots[ev.SlotId] = ev.Num;
            value.Total += ev.Num;
        }
    }

    Console.WriteLine("在這張地圖");
    if (mapPickItemData.TryGetValue(ev.ConfigBaseId, out BagDataModel pickValue))
    {
        if (pickValue.Slots.TryGetValue(ev.SlotId, out int prevSlotCount))
        {
            // update existing slot
            pickValue.Slots[ev.SlotId] = prevSlotCount + itemModel.Num;
            pickValue.Total += itemModel.Num;

            Console.WriteLine($"\t該物品在此欄位之前有 {prevSlotCount} 個，這次增加 {itemModel.Num} 個，現在該欄位有 {pickValue.Slots[ev.SlotId]} 個");
            Console.WriteLine($"\t目前地圖上 {pickValue.ItemName} 總數量: {pickValue.Total}\r\n");
        }
        else
        {
            // new slot for this map
            pickValue.Slots[ev.SlotId] = itemModel.Num;
            pickValue.Total += itemModel.Num;

            Console.WriteLine($"\t該物品在此欄位之前沒有數量，這次新增 {itemModel.Num} 個 (slot:{ev.SlotId})");
            Console.WriteLine($"\t目前地圖上 {pickValue.ItemName} 總數量: {pickValue.Total}\r\n");
        }
    }
    else
    {
        var bagData = new BagDataModel
        {
            ItemName = itemModel.Name,
            Total = itemModel.Num
        };
        bagData.Slots[ev.SlotId] = itemModel.Num;
        mapPickItemData[ev.ConfigBaseId] = bagData;

        Console.WriteLine($"\t第一次在這張地圖撿到 {bagData.ItemName} {bagData.Total} 個 (slot:{ev.SlotId} = {itemModel.Num})\r\n");
    }
    // Console.WriteLine($"{ev.Time:yyyy/MM/dd HH:mm:ss.fff} 撿到 {itemModel.Name} {itemModel.Num} 個");
};


//var filePath = "D:\\Torchlight Infinite Game\\UE_game\\TorchLight\\Saved\\Logs\\UE_game.log";
//var tail = new SafeFileTailWatcher(
//    filePath,
//    Encoding.UTF8,
//    TimeSpan.FromMilliseconds(500),
//    TimeSpan.FromSeconds(2),
//    startFromEnd: true);

//tail.OnNewLine += ProcessLineData;
//tail.Start();

using var fs = new FileStream("UE_game.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
using var streamReader = new StreamReader(fs, Encoding.UTF8);
string line;
while (!streamReader.EndOfStream)
{
    line = streamReader.ReadLine();
    ProcessLineData(line);
}


Console.WriteLine("監聽中，按下 Enter 離開...");
Console.ReadLine();


void ProcessLineData(string line)
{
    if (logFormater.IsInitBagData(line))
    {
        InitBagItem(logFormater.GetItemData(line));
        return;
    }

    if (line.Contains("LuaLoading@ NetData _LoadFunctionNetData Progress = 1.0"))
    {
        Console.WriteLine("初始化背包完成:");
        foreach (var bagItem in tempBagData)
        {
            Console.WriteLine($"物品名稱: {bagItem.Value.ItemName}, 總數量: {bagItem.Value.Total}");
            //foreach (var slot in bagItem.Value.Slots)
            //{
            //    Console.WriteLine($"\t欄位: {slot.Key}, 數量: {slot.Value}");
            //}
        }
        return;
    }

    if (line.Contains("LuaLoading@ LoadUILogic STT!"))
    {
        Console.WriteLine("重新登入, 重置背包資料");
        tempBagData = [];
        mapPickItemData = []; // 切換地圖重置
        return;
    }

    proc.HandleLine(line);
}

void InitBagItem(ItemModel item)
{
    if (tempBagData.TryGetValue(item.ConfigBaseId, out BagDataModel value))
    {
        value.Total += item.Num;
        value.Slots[item.SoltId] = item.Num;
        return;
    }

    var bagData = new BagDataModel
    {
        ItemName = item.Name,
        Total = item.Num
    };

    bagData.Slots[item.SoltId] = item.Num;
    tempBagData[item.ConfigBaseId] = bagData;
}

