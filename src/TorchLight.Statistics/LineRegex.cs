using System.Text.RegularExpressions;

namespace TorchLight.Statistics;

public static partial class LineRegex
{
    // 共同：時間 + ThreadId
    private const string UnrealTime = @"(?<time>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})";
    private const string ThreadId = @"\[\s*(?<tid>\d+)\]";

    // start/end
    [GeneratedRegex(@"\[" + UnrealTime + @"\]" + ThreadId + @".*?ItemChange@\s+ProtoName=(?<proto>\S+)\s+start", RegexOptions.Singleline)]
    public static partial Regex StartLine();

    [GeneratedRegex(@"\[" + UnrealTime + @"\]" + ThreadId + @".*?ItemChange@\s+ProtoName=(?<proto>\S+)\s+end", RegexOptions.Singleline)]
    public static partial Regex EndLine();

    // 區塊中要抓的 BagMgr 修改
    [GeneratedRegex(@"\[" + UnrealTime + @"\]" + ThreadId + @".*?BagMgr@:\s*Modfy\s+BagItem\s+PageId\s*=\s*(?<page>\d+)\s+SlotId\s*=\s*(?<slot>\d+)\s+ConfigBaseId\s*=\s*(?<config>\d+)\s+Num\s*=\s*(?<num>\d+)", RegexOptions.Singleline)]
    public static partial Regex BagModLine();

    [GeneratedRegex(@"\[" + UnrealTime + @"\]" + ThreadId + @".*?BagMgr@:\s*InitBagData\s+PageId\s*=\s*(?<page>\d+)\s+SlotId\s*=\s*(?<slot>\d+)\s+ConfigBaseId\s*=\s*(?<config>\d+)\s+Num\s*=\s*(?<num>\d+)", RegexOptions.Singleline)]
    public static partial Regex BagInitLine();

    [GeneratedRegex(@"\[" + UnrealTime + @"\]" + ThreadId + @".*BagMgr@:(?<action>Modfy\s+BagItem|InitBagData)\s+PageId\s*=\s*(?<page>\d+)\s+SlotId\s*=\s*(?<slot>\d+)(?:\s+ConfigBaseId\s*=\s*(?<config>\d+)\s+Num\s*=\s*(?<num>\d+))?", RegexOptions.Singleline)]
    public static partial Regex BagItemLine();

    [GeneratedRegex(@"\[" + UnrealTime + @"\]" + ThreadId + @".*?ItemChange@\s+Delete\s+Id=(?<config>\d+)_\S+\s+in\s+PageId=(?<page>\d+)\s+SlotId=(?<slot>\d+)")]
    public static partial Regex BagItemDeleteLine();

    [GeneratedRegex(@"\[" + UnrealTime + @"\]" + ThreadId + @".*?LastSceneName\s*=\s*World'[^']*/(?<from>[^/']+/[^/']+\.[^/']+)'\s+NextSceneName\s*=\s*World'[^']*/(?<to>[^/']+/[^/']+\.[^/']+)'$", RegexOptions.Singleline)]
    public static partial Regex MapLine();

    /// <summary>
    /// 只取最前面的時間
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"\[(\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}:\d{3})\]")]
    public static partial Regex GetDateTimeValue();

    /// <summary>
    /// 取得`[ ]` 內的值
    /// Ex: [12345] => 12345
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"\[(\d+)\]")]
    public static partial Regex GetCellValue();
}
