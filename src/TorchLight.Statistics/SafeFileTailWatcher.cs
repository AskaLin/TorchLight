using System.Text;
using System.Threading;
using Timer = System.Threading.Timer;

namespace TorchLight.Statistics;
public sealed class SafeFileTailWatcher : IDisposable
{
    public event Action<string> OnNewText;         // 原始追加文字（可能包含多行）
    public event Action<string> OnNewLine;         // 逐行回傳（已去除結尾換行）

    private readonly string _filePath;
    private readonly string _dir;
    private readonly string _fileName;
    private readonly Encoding _encoding;
    private readonly TimeSpan _debounce;
    private readonly TimeSpan _pollInterval;
    private readonly bool _startFromEnd;

    private FileSystemWatcher _watcher;
    private long _lastPosition;
    private DateTime _lastHandleTime = DateTime.MinValue;
    private Timer _debounceTimer;
    private CancellationTokenSource _cts;
    private Task _pollTask;
    private readonly object _lock = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="encoding"></param>
    /// <param name="debounce">防止短時間多次觸發, 預設間隔 200ms</param>
    /// <param name="pollInterval">輪詢補漏, 預設間隔 2s</param>
    /// <param name="startFromEnd">啟動後從檔尾開始追（只看新加的內容）</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public SafeFileTailWatcher(
        string filePath, Encoding encoding = null,
        TimeSpan? debounce = null, TimeSpan? pollInterval = null,
        bool startFromEnd = true)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _dir = Path.GetDirectoryName(_filePath) ?? throw new ArgumentException("Invalid path.", nameof(filePath));
        _fileName = Path.GetFileName(_filePath);
        _encoding = encoding ?? Encoding.UTF8;
        _debounce = debounce ?? TimeSpan.FromMilliseconds(200);
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
        _startFromEnd = startFromEnd;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();

            // 初始化 lastPosition
            if (File.Exists(_filePath))
            {
                var len = new FileInfo(_filePath).Length;
                _lastPosition = _startFromEnd ? len : 0;
            }
            else
            {
                _lastPosition = 0;
            }

            // FileSystemWatcher
            _watcher = new FileSystemWatcher(_dir)
            {
                Filter = _fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                InternalBufferSize = 64 * 1024 // 放大到上限，降低 overflow 機率
            };

            _watcher.Changed += OnFsChanged;
            _watcher.Created += OnFsChanged;
            _watcher.Renamed += OnFsRenamed;
            _watcher.Deleted += OnFsDeleted;
            _watcher.Error += OnFsError;

            // Debounce 計時器
            _debounceTimer = new Timer(_ => ReadNewDataSafe(), null, Timeout.Infinite, Timeout.Infinite);

            // 輪詢補漏
            _pollTask = Task.Run(() => PollLoopAsync(_cts.Token));
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _cts?.Cancel();
            _pollTask = null;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnFsChanged;
                _watcher.Created -= OnFsChanged;
                _watcher.Renamed -= OnFsRenamed;
                _watcher.Deleted -= OnFsDeleted;
                _watcher.Error -= OnFsError;
                _watcher.Dispose();
                _watcher = null;
            }

            _debounceTimer?.Dispose();
            _debounceTimer = null;

            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                ReadNewDataSafe();
            }
            catch { /* 忽略單次讀取失敗 */ }

            try
            {
                await Task.Delay(_pollInterval, ct);
            }
            catch (TaskCanceledException) { }
        }
    }

    private void OnFsChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce：短時間多次變動合併成一次讀取
        lock (_lock)
        {
            if (_debounceTimer == null) return;
            _debounceTimer.Change(_debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnFsRenamed(object sender, RenamedEventArgs e)
    {
        // 常見的 log 轮转：舊檔被改名，新的同名檔案出現
        // 這裡直接重置位置，等下一次 Changed/Created 讀新檔
        lock (_lock)
        {
            _lastPosition = 0;
            if (_debounceTimer == null) return;
            _debounceTimer.Change(_debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnFsDeleted(object sender, FileSystemEventArgs e)
    {
        // 檔案被刪除：等再出現時 (Created) 重新開始
        lock (_lock)
        {
            _lastPosition = 0;
        }
    }

    private void OnFsError(object sender, ErrorEventArgs e)
    {
        // 例如 InternalBufferOverflowException
        // 發生時做一次保險讀取
        ReadNewDataSafe();
    }

    private void ReadNewDataSafe()
    {
        lock (_lock)
        {
            // 節流：避免同時被 FS 事件與 Poll 連續觸發造成重複讀
            if ((DateTime.UtcNow - _lastHandleTime) < TimeSpan.FromMilliseconds(50))
                return;
            _lastHandleTime = DateTime.UtcNow;
        }

        try
        {
            if (!File.Exists(_filePath)) return;

            long fileLen = new FileInfo(_filePath).Length;

            // 檔案被截斷或輪轉（長度變小）
            if (fileLen < _lastPosition)
            {
                _lastPosition = 0;
            }

            if (fileLen == _lastPosition) return;
            long toRead = fileLen - _lastPosition;
            if (toRead <= 0) return;

            using var fs = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            fs.Seek(_lastPosition, SeekOrigin.Begin);

            // 以 buffer 讀取，避免一次 ReadToEnd 造成大檔大量字串配置
            using var ms = new MemoryStream(capacity: (int)Math.Min(toRead, 1024 * 1024));
            var buffer = new byte[81920];
            long remaining = toRead;

            while (remaining > 0)
            {
                int take = (int)Math.Min(buffer.Length, remaining);
                int read = fs.Read(buffer, 0, take);
                if (read <= 0) break;
                ms.Write(buffer, 0, read);
                remaining -= read;
            }

            _lastPosition = fs.Position;

            if (ms.Length == 0) return;

            string text = _encoding.GetString(ms.ToArray());

            // 事件回呼
            OnNewText?.Invoke(text);

            // 逐行拆分（保留跨批次的行尾）
            EmitLines(text);
        }
        catch (IOException)
        {
            // 檔案可能仍在寫入中：本次略過，交給下次觸發/輪詢
        }
        catch (UnauthorizedAccessException)
        {
            // 權限或被獨占寫入：略過
        }
    }

    // —— 逐行輸出 —— //
    private string _lineCarry = string.Empty;

    private void EmitLines(string appended)
    {
        var combined = _lineCarry + appended;
        _lineCarry = string.Empty;

        // 同時支援 \r\n / \n / \r
        var lines = combined.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

        // 若最後一段非完整換行，暫存至下次
        for (int i = 0; i < lines.Length; i++)
        {
            bool isLast = (i == lines.Length - 1);
            if (isLast && !combined.EndsWith('\n') && !combined.EndsWith('\r'))
            {
                _lineCarry = lines[i];
            }
            else
            {
                OnNewLine?.Invoke(lines[i]);
            }
        }
    }

    public void Dispose() => Stop();
}

