using System.Text.RegularExpressions;

namespace TorchLight.Statistics;

public abstract record LogEvent(DateTime Time, int ThreadId);

public record BagModEvent(DateTime Time, int ThreadId, int PageId, int SlotId, int ConfigBaseId, int Num)
    : LogEvent(Time, ThreadId);

public record BlockStarted(DateTime Time, int ThreadId, string ProtoName) : LogEvent(Time, ThreadId);
public record BlockEnded(DateTime Time, int ThreadId, string ProtoName) : LogEvent(Time, ThreadId);

public sealed class ItemChangeBlockContext
{
    public bool InBlock { get; set; }
    public string ProtoName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public List<BagModEvent> Buffer { get; } = [];
}

public static partial class LineParsers
{
    private const string DtFormat = "yyyy.MM.dd-HH.mm.ss:fff";

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
}

public sealed class ItemChangeBlockProcessor
{
    // 依 ThreadId 紀錄各自的區塊
    private readonly Dictionary<int, ItemChangeBlockContext> _ctx = [];

    // 回呼：你可以依需求只用其中幾個
    public event Action<BlockStarted> OnBlockStarted;                   // 偵測到 start
    public event Action<BagModEvent> OnBagModInsideBlock;               // 區塊內 BagMod（即時模式）
    public event Action<BlockEnded, IReadOnlyList<BagModEvent>> OnBlockEndedWithBatch; // 彙整模式

    // 只處理 ProtoName=PickItems 的區塊（其餘忽略）
    private const string TargetProto = "PickItems";

    public void HandleLine(string line)
    {
        // 1) start
        var mStart = LineParsers.StartLine().Match(line);
        if (mStart.Success)
        {
            var t = DateTime.ParseExact(mStart.Groups["time"].Value, "yyyy.MM.dd-HH.mm.ss:fff", null);
            var tid = int.Parse(mStart.Groups["tid"].Value);
            var proto = mStart.Groups["proto"].Value;

            var ctx = GetOrCreate(tid);

            // 只針對 PickItems；其他 ProtoName 可視需求擴充
            if (string.Equals(proto, TargetProto, StringComparison.OrdinalIgnoreCase))
            {
                ctx.InBlock = true;
                ctx.ProtoName = proto;
                ctx.StartTime = t;
                ctx.Buffer.Clear();

                OnBlockStarted?.Invoke(new BlockStarted(t, tid, proto));
            }
            return;
        }

        // 2) end
        var mEnd = LineParsers.EndLine().Match(line);
        if (mEnd.Success)
        {
            var t = DateTime.ParseExact(mEnd.Groups["time"].Value, "yyyy.MM.dd-HH.mm.ss:fff", null).AddHours(8);
            var tid = int.Parse(mEnd.Groups["tid"].Value);
            var proto = mEnd.Groups["proto"].Value;

            if (_ctx.TryGetValue(tid, out var ctx) && ctx.InBlock &&
                string.Equals(ctx.ProtoName, TargetProto, StringComparison.OrdinalIgnoreCase))
            {
                ctx.InBlock = false;
                var ended = new BlockEnded(t, tid, proto);
                OnBlockEndedWithBatch?.Invoke(ended, ctx.Buffer.AsReadOnly());
                ctx.Buffer.Clear(); // 清掉，等下一個區塊
            }
            return;
        }

        // 3) 區塊內的 BagMgr 修改
        var mBag = LineParsers.BagModLine().Match(line);
        if (mBag.Success)
        {
            var t = DateTime.ParseExact(mBag.Groups["time"].Value, "yyyy.MM.dd-HH.mm.ss:fff", null);
            var tid = int.Parse(mBag.Groups["tid"].Value);

            if (_ctx.TryGetValue(tid, out var ctx) && ctx.InBlock &&
                string.Equals(ctx.ProtoName, TargetProto, StringComparison.OrdinalIgnoreCase))
            {
                var ev = new BagModEvent(
                    t, tid,
                    PageId: int.Parse(mBag.Groups["page"].Value),
                    SlotId: int.Parse(mBag.Groups["slot"].Value),
                    ConfigBaseId: int.Parse(mBag.Groups["config"].Value),
                    Num: int.Parse(mBag.Groups["num"].Value)
                );

                // ✅ 即時模式：立刻通知
                OnBagModInsideBlock?.Invoke(ev);

                // ✅ 彙整模式：緩存到區塊，等 end 時一次吐
                ctx.Buffer.Add(ev);
            }
        }
    }

    private ItemChangeBlockContext GetOrCreate(int tid)
    {
        if (!_ctx.TryGetValue(tid, out var ctx))
        {
            ctx = new ItemChangeBlockContext();
            _ctx[tid] = ctx;
        }
        return ctx;
    }

    // 保護性機制（可選）：逾時自動結束區塊，避免漏掉 end
    public void CloseStaleBlocks(TimeSpan timeout, DateTime nowUtc)
    {
        foreach (var kvp in _ctx)
        {
            var ctx = kvp.Value;
            if (ctx.InBlock && (nowUtc - ctx.StartTime.ToUniversalTime()) > timeout)
            {
                ctx.InBlock = false;
                OnBlockEndedWithBatch?.Invoke(new BlockEnded(DateTime.UtcNow, kvp.Key, ctx.ProtoName), ctx.Buffer.AsReadOnly());
                ctx.Buffer.Clear();
            }
        }
    }
}
