﻿using YogurtDown.Core.Entity;
using System.Text.Json;
using static YogurtDown.Core.Util.HTTPUtil;
using static YogurtDown.Core.Logger;

namespace YogurtDown.Core.Fetcher;

public class SpaceVideoFetcher : IFetcher
{
    public async Task<VInfo> FetchAsync(string id)
    {
        id = id[4..];
        // using the live API can bypass w_rid
        string userInfoApi = $"https://api.live.bilibili.com/live_user/v1/Master/info?uid={id}";
        string userName = GetValidFileName(JsonDocument.Parse(await GetWebSourceAsync(userInfoApi)).RootElement.GetProperty("data").GetProperty("info").GetProperty("uname").ToString(), ".", true);
        List<string> urls = new();
        int pageSize = 50;
        int pageNumber = 1;
        var api = Parser.WbiSign($"mid={id}&order=pubdate&pn={pageNumber}&ps={pageSize}&tid=0&wts={DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}");
        api = $"https://api.bilibili.com/x/space/wbi/arc/search?{api}";
        string json = await GetWebSourceAsync(api);
        var infoJson = JsonDocument.Parse(json);
        var pages = infoJson.RootElement.GetProperty("data").GetProperty("list").GetProperty("vlist").EnumerateArray();
        foreach (var page in pages)
        {
            urls.Add($"https://www.bilibili.com/video/av{page.GetProperty("aid")}");
        }
        int totalCount = infoJson.RootElement.GetProperty("data").GetProperty("page").GetProperty("count").GetInt32();
        int totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
        while (pageNumber < totalPage)
        {
            pageNumber++;
            urls.AddRange(await GetVideosByPageAsync(pageNumber, pageSize, id));
        }
        await File.WriteAllTextAsync($"{userName}的投稿视频.txt", string.Join(Environment.NewLine, urls));
        Log("目前 YogurtDown 不支持下载用户的全部投稿视频，不过它已帮你获取了该用户的全部投稿视频地址，你可以自行使用批处理脚本等手段调用 YogurtDown 进行批量下载。例如，在 Windows 系统你可以使用如下代码：");
        Console.WriteLine();
        Console.WriteLine(@"@echo Off
For /F %%a in (urls.txt) Do (YogurtDown.exe ""%%a"")
pause");
        Console.WriteLine();
        throw new Exception("暂不支持该功能");
    }

    static async Task<List<string>> GetVideosByPageAsync(int pageNumber, int pageSize, string mid)
    {
        List<string> urls = new();
        var api = Parser.WbiSign($"mid={mid}&order=pubdate&pn={pageNumber}&ps={pageSize}&tid=0&wts={DateTimeOffset.Now.ToUnixTimeSeconds().ToString()}");
        api = $"https://api.bilibili.com/x/space/wbi/arc/search?{api}";
        string json = await GetWebSourceAsync(api);
        var infoJson = JsonDocument.Parse(json);
        var pages = infoJson.RootElement.GetProperty("data").GetProperty("list").GetProperty("vlist").EnumerateArray();
        foreach (var page in pages)
        {
            urls.Add($"https://www.bilibili.com/video/av{page.GetProperty("aid")}");
        }
        return urls;
    }

    private static string GetValidFileName(string input, string re = ".", bool filterSlash = false)
    {
        string title = input;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            title = title.Replace(invalidChar.ToString(), re);
        }
        if (filterSlash)
        {
            title = title.Replace("/", re);
            title = title.Replace("\\", re);
        }
        return title;
    }
}