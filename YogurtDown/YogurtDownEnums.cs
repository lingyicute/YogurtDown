using System;
using System.Linq;

namespace YogurtDown;

public enum YogurtDownDanmakuFormat
{
    Xml,
    Ass,
}

public static class YogurtDownDanmakuFormatInfo
{
    // 默认
    public static YogurtDownDanmakuFormat[] DefaultFormats = [YogurtDownDanmakuFormat.Xml, YogurtDownDanmakuFormat.Ass];
    public static string[] DefaultFormatsNames = DefaultFormats.Select(f => f.ToString().ToLower()).ToArray();
    // 可选项
    public static string[] AllFormatNames = Enum.GetNames(typeof(YogurtDownDanmakuFormat)).Select(f => f.ToLower()).ToArray();

    public static YogurtDownDanmakuFormat FromFormatName(string formatName)
    {
        return formatName switch
        {
            "xml" => YogurtDownDanmakuFormat.Xml,
            "ass" => YogurtDownDanmakuFormat.Ass,
            _ => YogurtDownDanmakuFormat.Xml,
        };
    }
}
