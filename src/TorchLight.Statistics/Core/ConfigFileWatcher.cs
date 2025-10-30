using Serilog;

namespace TorchLight.Statistics.Core;

/// <summary>
/// 設定檔案監控器 - 提供檔案變更監控和自動重載功能
/// </summary>
/// <typeparam name="T">設定項目類型</typeparam>
public class ConfigFileWatcher<T> : IDisposable where T : class
{
    private readonly object _lock = new();
    private FileSystemWatcher? _fileWatcher;
    private DateTime _lastReloadTime = DateTime.MinValue;
    private readonly TimeSpan _reloadDebounceTime = TimeSpan.FromSeconds(1);
    private readonly string _configFilePath;
    private readonly Func<string, List<T>> _loadConfigFunc;
    private readonly Action<bool, string> _onConfigUpdated;
    private List<T> _configs = [];

    /// <summary>
    /// 當前設定列表
    /// </summary>
    public List<T> Configs
    {
        get
        {
            lock (_lock)
            {
                return new List<T>(_configs);
            }
        }
    }

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="configFilePath">設定檔路徑</param>
    /// <param name="loadConfigFunc">載入設定的函式</param>
    /// <param name="onConfigUpdated">設定更新時的回調</param>
    public ConfigFileWatcher(
        string configFilePath,
        Func<string, List<T>> loadConfigFunc,
        Action<bool, string> onConfigUpdated)
    {
        _configFilePath = configFilePath;
        _loadConfigFunc = loadConfigFunc;
        _onConfigUpdated = onConfigUpdated;
    }

    /// <summary>
    /// 初始化監控
    /// </summary>
    public void Initialize(List<T> initialConfigs)
    {
        lock (_lock)
        {
            _configs = new List<T>(initialConfigs);
        }

        StartFileWatcher();
    }

    /// <summary>
    /// 更新設定列表
    /// </summary>
    public void UpdateConfigs(List<T> configs)
    {
        lock (_lock)
        {
            _configs = new List<T>(configs);
        }
    }

    /// <summary>
    /// 啟動檔案監控
    /// </summary>
    private void StartFileWatcher()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            var fileName = Path.GetFileName(_configFilePath);

            if (string.IsNullOrEmpty(directory))
                return;

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnConfigFileChanged;
            Log.Information("已啟動設定檔監控: {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "啟動檔案監控失敗: {Path}", _configFilePath);
        }
    }

    /// <summary>
    /// 檔案變更事件處理
    /// </summary>
    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // 防抖動：避免短時間內重複載入
        var now = DateTime.Now;
        if ((now - _lastReloadTime) < _reloadDebounceTime)
            return;

        _lastReloadTime = now;

        // 延遲一小段時間，確保檔案寫入完成
        Task.Delay(500).ContinueWith(_ =>
        {
            Log.Information("偵測到設定檔變更，重新載入: {Path}", _configFilePath);

            // 備份當前設定
            var backupConfigs = new List<T>(_configs);

            try
            {
                var newConfigs = _loadConfigFunc(_configFilePath);

                lock (_lock)
                {
                    _configs = newConfigs;
                }

                _onConfigUpdated?.Invoke(true, "設定檔已成功重新載入");
                Log.Information("設定檔重新載入成功: {Path}", _configFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "重新載入設定檔失敗，恢復原設定: {Path}", _configFilePath);

                // 恢復備份
                lock (_lock)
                {
                    _configs = backupConfigs;
                }

                _onConfigUpdated?.Invoke(false, $"設定檔更新失敗: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// 暫時停止監控
    /// </summary>
    public void PauseWatching()
    {
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
        }
    }

    /// <summary>
    /// 恢復監控
    /// </summary>
    public void ResumeWatching()
    {
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = true;
        }
    }

    /// <summary>
    /// 停止檔案監控
    /// </summary>
    public void Stop()
    {
        _fileWatcher?.Dispose();
        _fileWatcher = null;
        Log.Information("已停止設定檔監控: {Path}", _configFilePath);
    }

    /// <summary>
    /// 釋放資源
    /// </summary>
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
