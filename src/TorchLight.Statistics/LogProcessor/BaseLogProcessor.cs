using Serilog;

namespace TorchLight.Statistics.LogProcessor;

/// <summary>
/// 日誌處理器基類 - 提供統一的區塊處理邏輯
/// </summary>
public abstract class BaseLogProcessor
{
    /// <summary>
    /// 是否正在處理區塊
    /// </summary>
    protected bool IsInBlock { get; set; }

    /// <summary>
    /// 處理單行日誌（模板方法模式）
    /// </summary>
    /// <param name="line">日誌行內容</param>
    /// <returns>是否由此處理器處理（true = 已處理，不需要其他處理器處理）</returns>
    public bool HandleLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        // 檢查是否為區塊開始
        if (!IsInBlock && IsBlockStart(line))
        {
            IsInBlock = true;
            OnBlockStart(line);
            return true;
        }

        // 處理區塊內的行
        if (IsInBlock)
        {
            // 檢查是否為區塊結束
            if (IsBlockEnd(line))
            {
                OnBlockEnd(line);
                IsInBlock = false;
                return true;
            }

            // 處理區塊內的數據行
            ProcessBlockLine(line);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判斷是否為區塊開始
    /// </summary>
    protected abstract bool IsBlockStart(string line);

    /// <summary>
    /// 判斷是否為區塊結束
    /// </summary>
    protected abstract bool IsBlockEnd(string line);

    /// <summary>
    /// 區塊開始時的處理
    /// </summary>
    protected abstract void OnBlockStart(string line);

    /// <summary>
    /// 區塊結束時的處理
    /// </summary>
    protected abstract void OnBlockEnd(string line);

    /// <summary>
    /// 處理區塊內的每一行
    /// </summary>
    protected abstract void ProcessBlockLine(string line);

    /// <summary>
    /// 重置處理器狀態
    /// </summary>
    public virtual void Reset()
    {
        IsInBlock = false;
        Log.Debug("{ProcessorName} 已重置", GetType().Name);
    }
}
