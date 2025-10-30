using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorchLight.Statistics.Enums;

/// <summary>
/// 地圖類型枚舉
/// </summary>
public enum MapType
{
    /// <summary>
    /// 未知地圖
    /// </summary>
    Unknown,

    /// <summary>
    /// 藏身處
    /// </summary>
    Hideout,

    /// <summary>
    /// 異界地圖（可統計拾取）
    /// </summary>
    Netherrealm,

    /// <summary>
    /// 秘境
    /// </summary>
    SecretRealm,
}
