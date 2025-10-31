using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TorchLight.Statistics.UI;

/// <summary>
/// 懸浮統計窗體 - 無標題欄、細長型、永遠置頂
/// </summary>
public class FloatingStatsWindow : Form
{
    private readonly List<StatItem> _statItems = new();
    private Point _dragStartPoint;
    private bool _isDragging = false;

    // 調整大小相關
    private bool _isResizing = false;
    private ResizeDirection _resizeDirection = ResizeDirection.None;
    private Point _resizeStartPoint;
    private Size _resizeStartSize;
    private const int ResizeBorderWidth = 8;
    private const int MinWidth = 80;
    private const int MinHeight = 200;
    private const int MaxWidth = 800;
    private const int MaxHeight = 800;

    // 🆕 顯示模式
    private DisplayMode _displayMode = DisplayMode.Vertical;
    private const int VerticalWidth = 100;
    private const int VerticalHeight = 400;
    private const int HorizontalWidth = 600;
    private const int HorizontalHeight = 100;

    // 顏色配置
    private readonly Color _backgroundColor = Color.FromArgb(20, 20, 30);
    private readonly Color _borderColor = Color.FromArgb(100, 120, 200);
    private readonly Color _labelColor = Color.FromArgb(180, 180, 180);
    private readonly Color _valueColor = Color.FromArgb(255, 215, 0); // 金色

    // Windows API 宣告
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public FloatingStatsWindow()
    {
        InitializeWindow();
        InitializeStatItems();
    }

    private void InitializeWindow()
    {
        // 窗體基本設定
        FormBorderStyle = FormBorderStyle.None;  // 無邊框
        StartPosition = FormStartPosition.Manual;
        TopMost = true;  // 永遠置頂
        ShowInTaskbar = false;  // 不顯示在工作列

        // 設定窗體大小和位置（細長型）
        Width = VerticalWidth;
        Height = VerticalHeight;
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - Width - 20, 100);

        // 啟用雙緩衝以避免閃爍
        DoubleBuffered = true;

        // 設定透明度和背景色
        BackColor = _backgroundColor;
        Opacity = 0.9;

        // 註冊滑鼠事件以支援拖曳和調整大小
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;

        // 🆕 雙擊事件
        DoubleClick += OnDoubleClick;

        // 當窗體顯示時，強制置頂
        Shown += OnShown;

        // 當窗體失去焦點時，確保仍然置頂
        Deactivate += OnDeactivate;
    }

    // 🆕 雙擊切換顯示模式
    private void OnDoubleClick(object sender, EventArgs e)
    {
        ToggleDisplayMode();
    }

    // 🆕 切換顯示模式
    private void ToggleDisplayMode()
    {
        _displayMode = _displayMode == DisplayMode.Vertical ? DisplayMode.Horizontal : DisplayMode.Vertical;

        // 根據模式調整窗體大小
        if (_displayMode == DisplayMode.Vertical)
        {
            Width = VerticalWidth;
            Height = VerticalHeight;
        }
        else
        {
            Width = HorizontalWidth;
            Height = HorizontalHeight;
        }

        // 重新計算統計項目位置
        RecalculateItemPositions();

        Invalidate();
    }

    // 🆕 根據顯示模式重新計算項目位置
    private void RecalculateItemPositions()
    {
        if (_displayMode == DisplayMode.Vertical)
        {
            // 直排模式：垂直排列
            int startY = 40;
            int spacing = 60;
            for (int i = 0; i < _statItems.Count; i++)
            {
                _statItems[i].Y = startY + (i * spacing);
            }
        }
        else
        {
            // 橫列模式：水平排列
            int startX = 10;
            int itemWidth = (Width - 20) / _statItems.Count;
            for (int i = 0; i < _statItems.Count; i++)
            {
                _statItems[i].X = startX + (i * itemWidth);
                _statItems[i].Y = 40;
            }
        }
    }

    // 窗體顯示時強制置頂
    private void OnShown(object sender, EventArgs e)
    {
        EnsureTopMost();
    }

    // 窗體失去焦點時確保仍然置頂
    private void OnDeactivate(object sender, EventArgs e)
    {
        EnsureTopMost();
    }

    // 強制窗體置頂
    private void EnsureTopMost()
    {
        if (IsHandleCreated)
        {
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }

    // 覆寫 CreateParams 設定 WS_EX_TOPMOST 和 WS_EX_TOOLWINDOW
    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOPMOST = 0x00000008;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOPMOST;      // 永遠置頂
            cp.ExStyle |= WS_EX_TOOLWINDOW;   // 工具窗體（不顯示在工作列）
            cp.ExStyle |= WS_EX_NOACTIVATE;   // 不搶奪焦點
            return cp;
        }
    }

    private void InitializeStatItems()
    {
        _statItems.Add(new StatItem("地圖數", "0", 40, 0));
        _statItems.Add(new StatItem("物品種類", "0", 100, 0));
        _statItems.Add(new StatItem("總數量", "0", 160, 0));
        _statItems.Add(new StatItem("遊戲時間", "00:00:00", 220, 0));
        _statItems.Add(new StatItem("當前地圖", "待機中", 280, 0));
        _statItems.Add(new StatItem("拾取數", "0", 340, 0));
    }

    /// <summary>
    /// 更新統計數據
    /// </summary>
    public void UpdateStat(string label, string value)
    {
        var item = _statItems.FirstOrDefault(s => s.Label == label);
        if (item != null)
        {
            item.Value = value;
            Invalidate();
        }
    }

    /// <summary>
    /// 批次更新多個統計數據
    /// </summary>
    public void UpdateStats(Dictionary<string, string> stats)
    {
        foreach (var kvp in stats)
        {
            UpdateStat(kvp.Key, kvp.Value);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        DrawBackground(g);
        DrawTitle(g);
        DrawStatItems(g);
    }

    private void DrawBackground(Graphics g)
    {
        using var backgroundBrush = new SolidBrush(_backgroundColor);
        using var borderPen = new Pen(_borderColor, 2);

        var rect = new Rectangle(1, 1, Width - 2, Height - 2);
        var radius = 10;

        using var path = GetRoundedRectPath(rect, radius);
        g.FillPath(backgroundBrush, path);
        g.DrawPath(borderPen, path);
    }

    private void DrawTitle(Graphics g)
    {
        using var titleFont = new Font("微軟正黑體", 10, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Color.FromArgb(100, 150, 255));

        // 🆕 根據模式調整標題顯示
        var titleText = _displayMode == DisplayMode.Vertical ? "🔥 統計面板" : "🔥 統計面板 (雙擊切換模式)";
        var titleRect = new RectangleF(0, 5, Width, 25);
        var titleFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(titleText, titleFont, titleBrush, titleRect, titleFormat);

        using var linePen = new Pen(Color.FromArgb(60, 60, 80), 1);
        g.DrawLine(linePen, 20, 30, Width - 20, 30);
    }

    private void DrawStatItems(Graphics g)
    {
        using var labelFont = new Font("微軟正黑體", 8);
        using var valueFont = new Font("Consolas", 10, FontStyle.Bold);
        using var labelBrush = new SolidBrush(_labelColor);
        using var valueBrush = new SolidBrush(_valueColor);

        if (_displayMode == DisplayMode.Vertical)
        {
            // 直排模式：垂直排列
            foreach (var item in _statItems)
            {
                var labelRect = new RectangleF(15, item.Y, Width - 30, 15);
                g.DrawString(item.Label, labelFont, labelBrush, labelRect);

                var valueRect = new RectangleF(15, item.Y + 18, Width - 30, 20);
                var valueFormat = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(item.Value, valueFont, valueBrush, valueRect, valueFormat);
            }
        }
        else
        {
            // 橫列模式：水平排列
            int itemWidth = (Width - 20) / _statItems.Count;
            for (int i = 0; i < _statItems.Count; i++)
            {
                var item = _statItems[i];
                float x = 10 + (i * itemWidth);

                var labelRect = new RectangleF(x, 40, itemWidth - 5, 15);
                var labelFormat = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(item.Label, labelFont, labelBrush, labelRect, labelFormat);

                var valueRect = new RectangleF(x, 58, itemWidth - 5, 20);
                var valueFormat = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(item.Value, valueFont, valueBrush, valueRect, valueFormat);
            }
        }
    }

    private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    #region 拖曳和調整大小功能

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var direction = GetResizeDirection(e.Location);

            if (direction != ResizeDirection.None)
            {
                _isResizing = true;
                _resizeDirection = direction;
                _resizeStartPoint = e.Location;
                _resizeStartSize = Size;
            }
            else
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
            }
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isResizing)
        {
            HandleResize(e.Location);
        }
        else if (_isDragging)
        {
            var newLocation = new Point(
                      Location.X + e.X - _dragStartPoint.X,
            Location.Y + e.Y - _dragStartPoint.Y
                      );
            Location = newLocation;
        }
        else
        {
            UpdateCursor(e.Location);
        }
    }

    private void OnMouseUp(object sender, MouseEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        _resizeDirection = ResizeDirection.None;
        Cursor = Cursors.Default;
    }

    private ResizeDirection GetResizeDirection(Point location)
    {
        bool onLeft = location.X <= ResizeBorderWidth;
        bool onRight = location.X >= Width - ResizeBorderWidth;
        bool onTop = location.Y <= ResizeBorderWidth;
        bool onBottom = location.Y >= Height - ResizeBorderWidth;

        if (onLeft && onTop) return ResizeDirection.TopLeft;
        if (onRight && onTop) return ResizeDirection.TopRight;
        if (onLeft && onBottom) return ResizeDirection.BottomLeft;
        if (onRight && onBottom) return ResizeDirection.BottomRight;
        if (onLeft) return ResizeDirection.Left;
        if (onRight) return ResizeDirection.Right;
        if (onTop) return ResizeDirection.Top;
        if (onBottom) return ResizeDirection.Bottom;

        return ResizeDirection.None;
    }

    private void UpdateCursor(Point location)
    {
        var direction = GetResizeDirection(location);

        Cursor = direction switch
        {
            ResizeDirection.Left or ResizeDirection.Right => Cursors.SizeWE,
            ResizeDirection.Top or ResizeDirection.Bottom => Cursors.SizeNS,
            ResizeDirection.TopLeft or ResizeDirection.BottomRight => Cursors.SizeNWSE,
            ResizeDirection.TopRight or ResizeDirection.BottomLeft => Cursors.SizeNESW,
            _ => Cursors.Default
        };
    }

    private void HandleResize(Point currentLocation)
    {
        int deltaX = currentLocation.X - _resizeStartPoint.X;
        int deltaY = currentLocation.Y - _resizeStartPoint.Y;

        int newWidth = Width;
        int newHeight = Height;
        int newX = Location.X;
        int newY = Location.Y;

        switch (_resizeDirection)
        {
            case ResizeDirection.Right:
                newWidth = _resizeStartSize.Width + deltaX;
                break;

            case ResizeDirection.Left:
                newWidth = _resizeStartSize.Width - deltaX;
                newX = Location.X + deltaX;
                break;

            case ResizeDirection.Bottom:
                newHeight = _resizeStartSize.Height + deltaY;
                break;

            case ResizeDirection.Top:
                newHeight = _resizeStartSize.Height - deltaY;
                newY = Location.Y + deltaY;
                break;

            case ResizeDirection.TopLeft:
                newWidth = _resizeStartSize.Width - deltaX;
                newHeight = _resizeStartSize.Height - deltaY;
                newX = Location.X + deltaX;
                newY = Location.Y + deltaY;
                break;

            case ResizeDirection.TopRight:
                newWidth = _resizeStartSize.Width + deltaX;
                newHeight = _resizeStartSize.Height - deltaY;
                newY = Location.Y + deltaY;
                break;

            case ResizeDirection.BottomLeft:
                newWidth = _resizeStartSize.Width - deltaX;
                newHeight = _resizeStartSize.Height + deltaY;
                newX = Location.X + deltaX;
                break;

            case ResizeDirection.BottomRight:
                newWidth = _resizeStartSize.Width + deltaX;
                newHeight = _resizeStartSize.Height + deltaY;
                break;
        }

        newWidth = Math.Max(MinWidth, Math.Min(MaxWidth, newWidth));
        newHeight = Math.Max(MinHeight, Math.Min(MaxHeight, newHeight));

        if (_resizeDirection == ResizeDirection.Left ||
        _resizeDirection == ResizeDirection.TopLeft ||
         _resizeDirection == ResizeDirection.BottomLeft)
        {
            int actualWidthChange = newWidth - Width;
            newX = Location.X - actualWidthChange;
        }

        if (_resizeDirection == ResizeDirection.Top ||
            _resizeDirection == ResizeDirection.TopLeft ||
          _resizeDirection == ResizeDirection.TopRight)
        {
            int actualHeightChange = newHeight - Height;
            newY = Location.Y - actualHeightChange;
        }

        Location = new Point(newX, newY);
        Size = new Size(newWidth, newHeight);

        // 🆕 調整大小後重新計算項目位置
        RecalculateItemPositions();
    }

    private enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    #endregion

    // 🆕 顯示模式列舉
    private enum DisplayMode
    {
        Vertical,    // 直排
        Horizontal   // 橫列
    }

    private class StatItem
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public int Y { get; set; }
        public int X { get; set; }  // 🆕 新增 X 座標

        public StatItem(string label, string value, int y, int x)
        {
            Label = label;
            Value = value;
            Y = y;
            X = x;
        }
    }
}
