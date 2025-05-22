using QRCoder;
using System;
using System.IO;
using System.Threading.Tasks;
using static YogurtDown.YogurtDownUtil;
using static YogurtDown.Core.Logger;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using YogurtDown.Core.Util;

namespace YogurtDown;

internal static class YogurtDownLoginUtil
{
    public static async Task<string> GetLoginStatusAsync(string qrcodeKey)
    {
        string queryUrl = $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcodeKey}&source=main-fe-header";
        return await HTTPUtil.GetWebSourceAsync(queryUrl);
    }

    public static async Task LoginWEB()
    {
        try
        {
            Log("请求鉴权...");
            string loginUrl = "https://passport.bilibili.com/x/passport-login/web/qrcode/generate?source=main-fe-header";
            string url = JsonDocument.Parse(await HTTPUtil.GetWebSourceAsync(loginUrl)).RootElement.GetProperty("data").GetProperty("url").ToString();
            string qrcodeKey = GetQueryString("qrcode_key", url);
            //Log(oauthKey);
            //Log(url);
            bool flag = false;
            Log("生成二维码...");
            Log("  ");
            QRCodeGenerator qrGenerator = new();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode pngByteCode = new(qrCodeData);
            await File.WriteAllBytesAsync("qrcode.png", pngByteCode.GetGraphic(7));
            var consoleQRCode = new ConsoleQRCode(qrCodeData);
            consoleQRCode.GetGraphic();
            Log("  ");
            Log("生成二维码成功！请把刚刚生成的 qrcode.png 发给梨，让她完成鉴权相关的技术处理。");
            Log("  ");
            Log("或者，如果暂时不想麻烦梨，你也可以扫描屏幕上的二维码用自己的账号登录 YogurtDown。但需要注意的是，此时 YogurtDown 将只能下载你的账号有权播放的内容 / 清晰度。");

            while (true)
            {
                await Task.Delay(1000);
                string w = await GetLoginStatusAsync(qrcodeKey);
                int code = JsonDocument.Parse(w).RootElement.GetProperty("data").GetProperty("code").GetInt32();
                if (code == 86038)
                {
                    Log("  ");
                    LogColor("二维码已过期, 请重新执行鉴权流程。");
                    break;
                }
                else if (code == 86101) //等待扫码
                {
                    continue;
                }
                else if (code == 86090) //等待确认
                {
                    if (!flag)
                    {
                        flag = !flag;
                    }
                }
                else
                {
                    string cc = JsonDocument.Parse(w).RootElement.GetProperty("data").GetProperty("url").ToString();
                    Log("  ");
                    Log("鉴权成功！本次获得的令牌如下：");
                    Log("SESSDATA=" + GetQueryString("SESSDATA", cc));
                    Log("  ");
                    await File.WriteAllTextAsync(Path.Combine(Program.APP_DIR, "YogurtDown.data"), cc[(cc.IndexOf('?') + 1)..].Replace("&", ";").Replace(",", "%2C"));
                    File.Delete("qrcode.png");
                    break;
                }
            }
        }
        catch (Exception e) { LogError(e.Message); }
    }

    public static async Task LoginTV()
    {
        try
        {
            string loginUrl = "https://passport.snm0516.aisee.tv/x/passport-tv-login/qrcode/auth_code";
            string pollUrl = "https://passport.bilibili.com/x/passport-tv-login/qrcode/poll";
            var parms = GetTVLoginParms();
            Log("请求鉴权...");
            byte[] responseArray = await (await HTTPUtil.AppHttpClient.PostAsync(loginUrl, new FormUrlEncodedContent(parms.ToDictionary()))).Content.ReadAsByteArrayAsync();
            string web = Encoding.UTF8.GetString(responseArray);
            string url = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("url").ToString();
            string authCode = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("auth_code").ToString();
            Log("生成二维码...");
            QRCodeGenerator qrGenerator = new();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode pngByteCode = new(qrCodeData);
            await File.WriteAllBytesAsync("qrcode.png", pngByteCode.GetGraphic(7));
            var consoleQRCode = new ConsoleQRCode(qrCodeData);
            consoleQRCode.GetGraphic();
            Log("  ");
            Log("生成二维码成功！请把刚刚生成的 qrcode.png 发给梨，让她完成鉴权相关的技术处理。");
            Log("  ");
            Log("或者，如果暂时不想麻烦梨，你也可以扫描屏幕上的二维码用自己的账号登录 YogurtDown。此时 YogurtDown 将只能下载你的账号有权播放的内容 / 清晰度。");
            parms.Set("auth_code", authCode);
            parms.Set("ts", GetTimeStamp(true));
            parms.Remove("sign");
            parms.Add("sign", GetSign(ToQueryString(parms)));
            while (true)
            {
                await Task.Delay(1000);
                responseArray = await (await HTTPUtil.AppHttpClient.PostAsync(pollUrl, new FormUrlEncodedContent(parms.ToDictionary()))).Content.ReadAsByteArrayAsync();
                web = Encoding.UTF8.GetString(responseArray);
                string code = JsonDocument.Parse(web).RootElement.GetProperty("code").ToString();
                if (code == "86038")
                {
                    Log("  ");
                    LogColor("二维码已过期, 请重新执行鉴权流程。");
                    break;
                }
                else if (code == "86039") //等待扫码
                {
                    continue;
                }
                else
                {
                    string cc = JsonDocument.Parse(web).RootElement.GetProperty("data").GetProperty("access_token").ToString();
                    Log("  ");
                    Log("鉴权成功: AccessToken=" + cc);
                    Log("  ");
                    await File.WriteAllTextAsync(Path.Combine(Program.APP_DIR, "YogurtDownTV.data"), "access_token=" + cc);
                    File.Delete("qrcode.png");
                    break;
                }
            }
        }
        catch (Exception e) { LogError(e.Message); }
    }
}