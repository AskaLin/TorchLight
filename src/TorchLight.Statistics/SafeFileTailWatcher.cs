using System.Text;
using Timer = System.Threading.Timer;

namespace TorchLight.Statistics;

public sealed class SafeFileTailWatcher : IDisposable
{
    public event Action<string> OnNewText;// 原始追加文字（可能包含多行）
    public event Action<string> OnNewLine;   // 逐行回傳（已去除結尾換行）
    // 檔案大小變更事件
    public event Action<long> OnFileSizeChanged;   // 檔案大小（bytes）

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

    // 記錄上次檔案大小，避免重複通知
    private long _lastFileSize = -1;

    // 🆕 日誌監控狀態（只有在檢測到"已開啟日誌"後才為 true）
    private bool _isLogMonitoringActive = false;

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

    // 🆕 啟用日誌監控（當檢測到"已開啟日誌"訊息後調用）
    public void EnableLogMonitoring()
    {
        lock (_lock)
        {
            _isLogMonitoringActive = true;
        }
    }

    // 🆕 停用日誌監控（重新登入時調用）
    public void DisableLogMonitoring()
    {
        lock (_lock)
        {
            _isLogMonitoringActive = false;
            // 通知前端大小為 0（待機中）
            NotifyFileSizeChanged(0);
        }
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
                // 初始化時不通知檔案大小（等待檢測到"已開啟日誌"）
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
                InternalBufferSize = 64 * 1024
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
        lock (_lock)
        {
            if (_debounceTimer == null) return;
            _debounceTimer.Change(_debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnFsRenamed(object sender, RenamedEventArgs e)
    {
        lock (_lock)
        {
            _lastPosition = 0;
            if (_debounceTimer == null) return;
            _debounceTimer.Change(_debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnFsDeleted(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            _lastPosition = 0;
            // 檔案刪除時，如果監控已啟用才通知大小為 0
            if (_isLogMonitoringActive)
            {
                NotifyFileSizeChanged(0);
            }
        }
    }

    private void OnFsError(object sender, ErrorEventArgs e)
    {
        ReadNewDataSafe();
    }

    // 🆕 通知檔案大小變更（只有在日誌監控啟用後才通知）
    private void NotifyFileSizeChanged(long fileSize)
    {
        if (!_isLogMonitoringActive)
            return;

        if (_lastFileSize != fileSize)
        {
            _lastFileSize = fileSize;
            OnFileSizeChanged?.Invoke(fileSize);
        }
    }

    private void ReadNewDataSafe()
    {
        lock (_lock)
        {
            if ((DateTime.UtcNow - _lastHandleTime) < TimeSpan.FromMilliseconds(50))
                return;
            _lastHandleTime = DateTime.UtcNow;
        }

        try
        {
            if (!File.Exists(_filePath))
            {
                // 檔案不存在時，如果監控已啟用才通知大小為 0
                if (_isLogMonitoringActive)
                {
                    NotifyFileSizeChanged(0);
                }
                return;
            }

            long fileLen = new FileInfo(_filePath).Length;

            // 🆕 只有在日誌監控啟用後才通知檔案大小變更
            NotifyFileSizeChanged(fileLen);

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

            OnNewText?.Invoke(text);
            EmitLines(text);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private string _lineCarry = string.Empty;

    private void EmitLines(string appended)
    {
        var combined = _lineCarry + appended;
        _lineCarry = string.Empty;

        var lines = combined.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

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

