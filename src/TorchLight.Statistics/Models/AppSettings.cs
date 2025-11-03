using System.Drawing;

namespace TorchLight.Statistics.Models;

/// <summary>
/// 應用程式設定模型
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 斬殺線設定
    /// </summary>
    public ExecuteLineSettings ExecuteLine { get; set; } = new();
}

/// <summary>
/// 斬殺線設定
/// </summary>
public class ExecuteLineSettings
{
    /// <summary>
    /// 第一階段百分比（斬殺）
    /// </summary>
    public int Stage1Percentage { get; set; } = 20;

    /// <summary>
    /// 第一階段顏色（十六進位格式，例如：#FF0000）
    /// </summary>
    public string Stage1Color { get; set; } = "#FF0000";

    /// <summary>
    /// 第二階段百分比（危險）
    /// </summary>
    public int Stage2Percentage { get; set; } = 15;

    /// <summary>
    /// 第二階段顏色（十六進位格式，例如：#FFA500）
    /// </summary>
    public string Stage2Color { get; set; } = "#FFA500";

    /// <summary>
    /// 第三階段百分比（安全）
    /// </summary>
    public int Stage3Percentage { get; set; } = 15;

    /// <summary>
    /// 第三階段顏色（十六進位格式，例如：#FFFF00）
    /// </summary>
    public string Stage3Color { get; set; } = "#FFFF00";

    /// <summary>
    /// 預設區域顏色（十六進位格式，例如：#00FF00）
    /// </summary>
    public string DefaultColor { get; set; } = "#00FF00";

    /// <summary>
    /// 透明度（0.0-1.0）
    /// </summary>
    public double Opacity { get; set; } = 0.95;

    /// <summary>
    /// 視窗位置 X
    /// </summary>
    public int LocationX { get; set; } = 100;

    /// <summary>
    /// 視窗位置 Y
    /// </summary>
    public int LocationY { get; set; } = 200;

    /// <summary>
    /// 視窗寬度
    /// </summary>
    public int Width { get; set; } = 1000;

    /// <summary>
    /// 視窗高度
    /// </summary>
    public int Height { get; set; } = 30;

    /// <summary>
    /// 是否顯示（視窗狀態）
    /// </summary>
    public bool IsVisible { get; set; } = false;

    /// <summary>
    /// 驗證百分比總和是否有效
    /// </summary>
    public bool IsValid()
    {
        return Stage1Percentage + Stage2Percentage + Stage3Percentage <= 100;
    }

    /// <summary>
    /// 獲取剩餘百分比
    /// </summary>
    public int GetRemainingPercentage()
    {
        return Math.Max(0, 100 - Stage1Percentage - Stage2Percentage - Stage3Percentage);
    }

    /// <summary>
    /// 將十六進位顏色字串轉換為 Color 物件
    /// </summary>
    public Color GetStage1Color() => GetColorFromHex(Stage1Color, Color.Red);
    public Color GetStage2Color() => GetColorFromHex(Stage2Color, Color.Orange);
    public Color GetStage3Color() => GetColorFromHex(Stage3Color, Color.Yellow);
    public Color GetDefaultColor() => GetColorFromHex(DefaultColor, Color.Green);

    private Color GetColorFromHex(string hex, Color fallback)
    {
        try
        {
            return ColorTranslator.FromHtml(hex);
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// 從 Color 物件設定顏色
    /// </summary>
    public void SetStage1Color(Color color) => Stage1Color = ColorTranslator.ToHtml(color);
    public void SetStage2Color(Color color) => Stage2Color = ColorTranslator.ToHtml(color);
    public void SetStage3Color(Color color) => Stage3Color = ColorTranslator.ToHtml(color);
    public void SetDefaultColor(Color color) => DefaultColor = ColorTranslator.ToHtml(color);
}
