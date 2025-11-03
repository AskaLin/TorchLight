using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TorchLight.Statistics.UI;

/// <summary>
/// 斬殺線窗體 - 無標題欄、可調整大小、永遠置頂、工具尺樣式
/// </summary>
public class ExecuteLineWindow : Form
{
    private Point _dragStartPoint;
    private bool _isDragging = false;

    // 調整大小相關
    private bool _isResizing = false;
    private ResizeDirection _resizeDirection = ResizeDirection.None;
    private Point _resizeStartPoint;
    private Size _resizeStartSize;
    private const int ResizeBorderWidth = 8;
    private const int MinHeight = 10;
    private const int MaxHeight = 50;
    private const int MinWidth = 100;
    private const int MaxWidth = 1600;

    // 斬殺線設定 - 三階段
    private int _stage1Percentage = 20; // 第一階段（斬殺）
    private Color _stage1Color = Color.Red;
    private int _stage2Percentage = 15; // 第二階段（危險）
    private Color _stage2Color = Color.Orange;
    private int _stage3Percentage = 15; // 第三階段（安全）
    private Color _stage3Color = Color.Yellow;
    private Color _defaultColor = Color.Green; // 預設區域顏色

    // 工具尺樣式設定
    private const int CornerRadius = 8; // 圓角半徑

    // Windows API 宣告
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public ExecuteLineWindow()
    {
      InitializeWindow();
    }

    private void InitializeWindow()
    {
        // 窗體基本設定
        FormBorderStyle = FormBorderStyle.None;  // 無邊框
        StartPosition = FormStartPosition.Manual;
   TopMost = true;  // 永遠置頂
        ShowInTaskbar = false;  // 不顯示在工作列

      // 設定窗體大小和位置
  Width = 1000;
      Height = 30;
        Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - Width - 20, 200);

    // 啟用雙緩衝以避免閃爍
DoubleBuffered = true;

        // 設定透明度
  BackColor = Color.FromArgb(240, 240, 240);
   Opacity = 0.95;

        // 註冊滑鼠事件以支援拖曳和調整大小
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
     MouseUp += OnMouseUp;

        // 當窗體顯示時，強制置頂
 Shown += OnShown;

        // 當窗體失去焦點時，確保仍然置頂
        Deactivate += OnDeactivate;

        // 監聽 Resize 事件以重新繪製
        Resize += OnResize;
    }

    // 窗體大小改變時重新繪製
    private void OnResize(object sender, EventArgs e)
    {
        // 使用 Region 實現圓角
        UpdateRegion();
        Invalidate();
    }

    // 更新視窗區域（圓角）
    private void UpdateRegion()
    {
        var path = GetRoundedRectPath(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region = new Region(path);
    }

    // 窗體顯示時強制置頂
    private void OnShown(object sender, EventArgs e)
    {
        UpdateRegion();
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
 cp.ExStyle |= WS_EX_NOACTIVATE; // 不搶奪焦點
  return cp;
     }
    }

    /// <summary>
    /// 更新斬殺線設定 - 三階段
    /// </summary>
    public void UpdateSettings(
    int stage1Percentage, Color stage1Color,
        int stage2Percentage, Color stage2Color,
        int stage3Percentage, Color stage3Color,
        Color defaultColor, double opacity)
    {
      _stage1Percentage = Math.Max(0, Math.Min(100, stage1Percentage));
        _stage1Color = stage1Color;
        _stage2Percentage = Math.Max(0, Math.Min(100, stage2Percentage));
    _stage2Color = stage2Color;
  _stage3Percentage = Math.Max(0, Math.Min(100, stage3Percentage));
        _stage3Color = stage3Color;
        _defaultColor = defaultColor;
      Opacity = Math.Max(0.0, Math.Min(1.0, opacity));
        Invalidate(); // 重新繪製
    }

    /// <summary>
 /// 獲取當前設定
    /// </summary>
    public (int stage1Percentage, Color stage1Color, 
         int stage2Percentage, Color stage2Color,
       int stage3Percentage, Color stage3Color,
    Color defaultColor, double opacity, 
   Point location, Size size) GetSettings()
    {
    return (_stage1Percentage, _stage1Color,
    _stage2Percentage, _stage2Color,
       _stage3Percentage, _stage3Color,
   _defaultColor, Opacity, 
  Location, Size);
    }

    /// <summary>
    /// 套用設定（包含位置和大小）
    /// </summary>
    public void ApplySettings(
   int stage1Percentage, Color stage1Color,
        int stage2Percentage, Color stage2Color,
        int stage3Percentage, Color stage3Color,
  Color defaultColor, double opacity,
      Point location, Size size)
    {
        _stage1Percentage = Math.Max(0, Math.Min(100, stage1Percentage));
        _stage1Color = stage1Color;
        _stage2Percentage = Math.Max(0, Math.Min(100, stage2Percentage));
        _stage2Color = stage2Color;
        _stage3Percentage = Math.Max(0, Math.Min(100, stage3Percentage));
        _stage3Color = stage3Color;
   _defaultColor = defaultColor;
        Opacity = Math.Max(0.0, Math.Min(1.0, opacity));

        // 確保大小在範圍內
        int width = Math.Max(MinWidth, Math.Min(MaxWidth, size.Width));
     int height = Math.Max(MinHeight, Math.Min(MaxHeight, size.Height));

   Location = location;
   Size = new Size(width, height);

  UpdateRegion();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
  base.OnPaint(e);

        var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;

        // 計算各階段寬度（順序：階段3 → 階段2 → 階段1 → 預設）
        int stage3Width = (int)(Width * (_stage3Percentage / 100.0));
        int stage2Width = (int)(Width * (_stage2Percentage / 100.0));
  int stage1Width = (int)(Width * (_stage1Percentage / 100.0));
        int defaultWidth = Width - stage3Width - stage2Width - stage1Width;

        // 繪製背景（使用圓角矩形）
        var backgroundPath = GetRoundedRectPath(new Rectangle(0, 0, Width, Height), CornerRadius);

        int currentX = 0;

        // 繪製第三階段（最左邊，從0開始）
        if (stage3Width > 0)
        {
using var brush = new SolidBrush(_stage3Color);
            var rect = new Rectangle(currentX, 0, stage3Width, Height);
            var path = currentX == 0 && stage3Width == Width 
                ? GetRoundedRectPath(rect, CornerRadius)
        : GetPartialRoundedRectPath(rect, CornerRadius, currentX == 0, false);
            g.FillPath(brush, path);
    currentX += stage3Width;
        }

        // 繪製第二階段（中間）
  if (stage2Width > 0)
        {
 using var brush = new SolidBrush(_stage2Color);
       var rect = new Rectangle(currentX, 0, stage2Width, Height);
     var path = GetPartialRoundedRectPath(rect, CornerRadius, false, false);
            g.FillPath(brush, path);
         currentX += stage2Width;
        }

        // 繪製第一階段（預設區段右邊）
        if (stage1Width > 0)
        {
       using var brush = new SolidBrush(_stage1Color);
            var rect = new Rectangle(currentX, 0, stage1Width, Height);
        var path = GetPartialRoundedRectPath(rect, CornerRadius, false, false);
       g.FillPath(brush, path);
         currentX += stage1Width;
        }

        // 繪製預設區域（最右邊）
        if (defaultWidth > 0)
     {
     using var brush = new SolidBrush(_defaultColor);
            var rect = new Rectangle(currentX, 0, defaultWidth, Height);
   var path = currentX == 0
          ? GetRoundedRectPath(rect, CornerRadius)
    : GetPartialRoundedRectPath(rect, CornerRadius, false, true);
        g.FillPath(brush, path);
        }

 // 繪製工具尺刻度
     DrawRulerMarks(g);

        // 繪製邊框
        using var borderPen = new Pen(Color.FromArgb(100, 100, 100), 2);
        g.DrawPath(borderPen, backgroundPath);
    }

    /// <summary>
    /// 繪製工具尺刻度
 /// </summary>
    private void DrawRulerMarks(Graphics g)
  {
        using var markPen = new Pen(Color.FromArgb(60, 60, 60), 1);

        // 根據寬度決定顯示哪些刻度
        bool show1Percent = Width >= 500;  // 寬度 >= 500 顯示 1% 刻度
 bool show5Percent = Width >= 300;  // 寬度 >= 300 顯示 5% 刻度
        bool show10Percent = true;      // 總是顯示 10% 刻度

        for (int i = 0; i <= 100; i++)
        {
    int x = (int)(Width * (i / 100.0));

// 跳過邊界（0 和 100）
   if (x <= CornerRadius || x >= Width - CornerRadius)
                continue;

  float markHeight = 0;

   // 10% 刻度（大刻度）
     if (i % 10 == 0 && show10Percent)
         {
     markHeight = Height * 0.4f;
       }
      // 5% 刻度（中刻度）
            else if (i % 5 == 0 && show5Percent)
     {
           markHeight = Height * 0.25f;
            }
   // 1% 刻度（小刻度）
            else if (show1Percent)
      {
              markHeight = Height * 0.15f;
       }

            // 繪製刻度線
     if (markHeight > 0)
            {
             // 上方刻度
           g.DrawLine(markPen, x, 0, x, markHeight);
                // 下方刻度
        g.DrawLine(markPen, x, Height - markHeight, x, Height);
      }
  }

      // 繪製百分比數字（只在 10% 刻度上）
        if (Width >= 500)
        {
            using var font = new Font("Arial", 7, FontStyle.Regular);
   using var textBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
            var format = new StringFormat
            {
    Alignment = StringAlignment.Center,
   LineAlignment = StringAlignment.Center
            };

            for (int i = 10; i < 100; i += 10)
       {
          int x = (int)(Width * (i / 100.0));
       if (x > CornerRadius && x < Width - CornerRadius)
       {
         g.DrawString($"{i}", font, textBrush, x, Height / 2, format);
              }
          }
        }
    }

    /// <summary>
    /// 獲取圓角矩形路徑
    /// </summary>
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

    /// <summary>
    /// 獲取部分圓角矩形路徑（支援只圓角左側或右側）
    /// </summary>
    private GraphicsPath GetPartialRoundedRectPath(Rectangle rect, int radius, bool roundLeft, bool roundRight)
  {
        var path = new GraphicsPath();
      int diameter = radius * 2;

    // 右上角
        if (roundRight)
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
      else
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Y);

        // 右側線
   path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);

      // 右下角
        if (roundRight)
    path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
    else
  path.AddLine(rect.Right, rect.Bottom, rect.Right, rect.Bottom);

        // 底部線
        path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);

        // 左下角
        if (roundLeft)
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
     else
        path.AddLine(rect.X, rect.Bottom, rect.X, rect.Bottom);

        // 左側線
        path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y);

        // 左上角
   if (roundLeft)
    path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
    else
            path.AddLine(rect.X, rect.Y, rect.X, rect.Y);

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

   // 限制寬度和高度
     newWidth = Math.Max(MinWidth, Math.Min(MaxWidth, newWidth));
      newHeight = Math.Max(MinHeight, Math.Min(MaxHeight, newHeight));

        // 調整位置（當從左側或頂部調整大小時）
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
}
