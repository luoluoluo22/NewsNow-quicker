// 引用必要的命名空间
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

// 缓存文件路径
private static readonly string CacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QuickerCache", "newsData.json");

// Quicker将会调用的函数
public static void Exec(Quicker.Public.IStepContext context)
{
    try
    {
        // 确保缓存目录存在
        string cacheDirectory = Path.GetDirectoryName(CacheFilePath);
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

        // 尝试从缓存加载数据
        string cachedData = LoadCachedData();
        if (!string.IsNullOrEmpty(cachedData))
        {
            // 立即将缓存数据设置到Quicker变量
            context.SetVarValue("newsData", cachedData);
        }
        
        // 在后台线程中更新数据
        Task.Run(() => {
            try
            {
                // 创建新闻数据字典
                var newsDictionary = new Dictionary<string, List<NewsItem>>();
                
                // 获取各类新闻数据并添加到字典中 - 使用更有意义的栏目名称，增加获取条数到9条左右
                newsDictionary.Add("V2EX热门", GetV2exNews(9));
                newsDictionary.Add("微博热搜", GetWeiboNews(9));
                newsDictionary.Add("IT之家", GetITHomeNews(9));
                newsDictionary.Add("知乎热榜", GetZhihuNews(9));
                newsDictionary.Add("GitHub热门", GetGitHubTrending(9));
                newsDictionary.Add("小红书推荐", GetXiaohongshuNotes(9));
                
                // 使用Newtonsoft.Json序列化
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(newsDictionary, Newtonsoft.Json.Formatting.Indented);
                
                // 将数据保存到Quicker变量
                context.SetVarValue("newsData", jsonString);
                
                // 保存到缓存文件
                SaveCachedData(jsonString);
            }
            catch (Exception)
            {
                // 不显示错误消息，静默处理错误
            }
        });
    }
    catch (Exception)
    {
        // 不显示错误消息，静默处理错误
    }
}

// 从缓存文件加载数据
private static string LoadCachedData()
{
    try
    {
        if (File.Exists(CacheFilePath))
        {
            // 检查文件修改时间，如果超过12小时则认为缓存过期
            var fileInfo = new FileInfo(CacheFilePath);
            if ((DateTime.Now - fileInfo.LastWriteTime).TotalHours > 12)
            {
                return null;
            }
            
            return File.ReadAllText(CacheFilePath, Encoding.UTF8);
        }
    }
    catch
    {
        // 读取缓存失败，忽略错误
    }
    
    return null;
}

// 保存数据到缓存文件
private static void SaveCachedData(string data)
{
    try
    {
        File.WriteAllText(CacheFilePath, data, Encoding.UTF8);
    }
    catch
    {
        // 保存缓存失败，忽略错误
    }
}

// 新闻项模型 - 与词典.json中的格式完全一致
public class NewsItem
{
    public string Title { get; set; }
    public string Time { get; set; }
    public string Url { get; set; }
}

// 获取V2EX热门话题
private static List<NewsItem> GetV2exNews(int count)
{
    var result = new List<NewsItem>();
    try
    {
        // 创建请求并设置User-Agent和其他头信息
        var request = (HttpWebRequest)WebRequest.Create("https://www.v2ex.com/?tab=hot");
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.Add("Cache-Control", "max-age=0");
        request.Headers.Add("Upgrade-Insecure-Requests", "1");
        request.KeepAlive = true;
        request.Timeout = 15000; // 增加超时时间到15秒
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        
        // 获取响应内容
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
        {
            string html = reader.ReadToEnd();
            
            // 使用正则表达式提取话题信息 - 更新正则表达式
            var titleRegex = new Regex(@"<span class=""item_title"">.*?<a href=""(.*?)"".*?>(.*?)</a></span>", RegexOptions.Compiled | RegexOptions.Singleline);
            var matches = titleRegex.Matches(html);
            
            if (matches.Count == 0)
            {
                // 尝试其他V2EX的正则表达式
                titleRegex = new Regex(@"<span\s+class=""topic-link"">.*?<a\s+href=""(.*?)"".*?>(.*?)</a></span>", RegexOptions.Compiled | RegexOptions.Singleline);
                matches = titleRegex.Matches(html);
            }
            
            // 如果还是没有匹配结果，尝试更通用的正则表达式
            if (matches.Count == 0)
            {
                titleRegex = new Regex(@"<a href=""(/t/\d+.*?)"".*?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.Singleline);
                matches = titleRegex.Matches(html);
            }
            
            int itemCount = 0;
            foreach (Match match in matches)
            {
                if (itemCount >= count)
                    break;
                
                string title = match.Groups[2].Value.Trim();
                
                // 过滤掉HTML标签
                title = Regex.Replace(title, "<.*?>", string.Empty);
                
                if (string.IsNullOrEmpty(title) || title.Length < 3)
                    continue;
                    
                var newsItem = new NewsItem
                {
                    Title = title,
                    Url = "https://www.v2ex.com" + match.Groups[1].Value,
                    Time = DateTime.Now.ToString("HH:mm")
                };
                
                result.Add(newsItem);
                itemCount++;
            }
        }
    }
    catch (Exception)
    {
        // 如果获取失败，使用v2ex的官方API尝试获取
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.v2ex.com/api/topics/hot.json");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            request.Timeout = 15000;
            
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                var topics = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(json);
                
                int itemCount = 0;
                foreach (var topic in topics)
                {
                    if (itemCount >= count)
                        break;
                    
                    var newsItem = new NewsItem
                    {
                        Title = (string)topic.title,
                        Url = (string)topic.url,
                        Time = DateTime.Now.ToString("HH:mm")
                    };
                    
                    result.Add(newsItem);
                    itemCount++;
                }
            }
        }
        catch
        {
            // 如果API也失败，返回空列表，不添加备用数据
        }
    }
    
    return result;
}

// 获取微博热搜
private static List<NewsItem> GetWeiboNews(int count)
{
    var result = new List<NewsItem>();
    try
    {
        // 创建请求并设置User-Agent
        var request = (HttpWebRequest)WebRequest.Create("https://weibo.com/ajax/side/hotSearch");
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Accept = "application/json, text/plain, */*";
        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Referer = "https://weibo.com/";
        request.Timeout = 15000;
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        
        // 获取响应内容
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
        {
            string json = reader.ReadToEnd();
            
            // 使用Newtonsoft.Json解析微博热搜API返回的JSON
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            
            if (data != null && data.data != null && data.data.realtime != null)
            {
                int itemCount = 0;
                foreach (var item in data.data.realtime)
                {
                    if (itemCount >= count)
                        break;
                    
                    string title = (string)item.word;
                    string url = "https://s.weibo.com/weibo?q=" + WebUtility.UrlEncode(title);
                    
                    var newsItem = new NewsItem
                    {
                        Title = title,
                        Url = url,
                        Time = DateTime.Now.ToString("HH:mm")
                    };
                    
                    result.Add(newsItem);
                    itemCount++;
                }
            }
        }
    }
    catch (Exception)
    {
        // 如果API失败，尝试另一个获取微博热搜的方法
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://weibo.com/hot/search");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            request.Timeout = 15000;
            
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                
                // 使用正则表达式提取热搜信息
                var topicRegex = new Regex(@"<a\s+href=""(.*?)"".*?class=""HotTopic_tit.*?"">(.*?)</a>", RegexOptions.Compiled | RegexOptions.Singleline);
                var matches = topicRegex.Matches(html);
                
                int itemCount = 0;
                foreach (Match match in matches)
                {
                    if (itemCount >= count)
                        break;
                    
                    string title = match.Groups[2].Value.Trim();
                    title = Regex.Replace(title, "<.*?>", string.Empty);
                    
                    if (string.IsNullOrEmpty(title) || title.Length < 2)
                        continue;
                    
                    var newsItem = new NewsItem
                    {
                        Title = title,
                        Url = "https://weibo.com" + match.Groups[1].Value,
                        Time = DateTime.Now.ToString("HH:mm")
                    };
                    
                    result.Add(newsItem);
                    itemCount++;
                }
            }
        }
        catch
        {
            // 如果两种方法都失败，返回空列表，不添加备用数据
        }
    }
    
    return result;
}

// 获取IT之家新闻
private static List<NewsItem> GetITHomeNews(int count)
{
    var result = new List<NewsItem>();
    try
    {
        // 创建请求并设置User-Agent
        var request = (HttpWebRequest)WebRequest.Create("https://www.ithome.com/");
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Timeout = 15000;
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        
        // 获取响应内容
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
        {
            string html = reader.ReadToEnd();
            
            // 使用正则表达式提取新闻信息
            var newsRegex = new Regex(@"<a\s+href=""(https://www\.ithome\.com/\d+/.*?)"".*?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.Singleline);
            var matches = newsRegex.Matches(html);
            
            int itemCount = 0;
            foreach (Match match in matches)
            {
                if (itemCount >= count)
                    break;
                    
                var title = match.Groups[2].Value.Trim();
                title = Regex.Replace(title, "<.*?>", string.Empty);
                
                if (string.IsNullOrEmpty(title) || title.Length < 5)
                    continue;
                    
                var newsItem = new NewsItem
                {
                    Title = title,
                    Url = match.Groups[1].Value,
                    Time = DateTime.Now.ToString("HH:mm")
                };
                
                result.Add(newsItem);
                itemCount++;
            }
        }
    }
    catch (Exception)
    {
        // 如果获取失败，返回空列表，不添加备用数据
    }
    
    return result;
}

// 获取知乎热榜
private static List<NewsItem> GetZhihuNews(int count)
{
    var result = new List<NewsItem>();
    try
    {
        // 使用知乎API获取热榜
        var request = (HttpWebRequest)WebRequest.Create("https://www.zhihu.com/api/v3/feed/topstory/hot-lists/total?limit=50&desktop=true");
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Accept = "application/json, text/plain, */*";
        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Referer = "https://www.zhihu.com/hot";
        request.Headers.Add("x-requested-with", "fetch");
        request.Timeout = 15000;
        
        // 获取响应内容
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
        {
            string json = reader.ReadToEnd();
            
            // 解析JSON获取热榜数据
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            
            if (data != null && data.data != null)
            {
                int itemCount = 0;
                foreach (var item in data.data)
                {
                    if (itemCount >= count)
                        break;
                    
                    string title = (string)item.target.title;
                    string url = $"https://www.zhihu.com/question/{item.target.id}";
                    
                    var newsItem = new NewsItem
                    {
                        Title = title,
                        Url = url,
                        Time = DateTime.Now.ToString("HH:mm")
                    };
                    
                    result.Add(newsItem);
                    itemCount++;
                }
            }
        }
    }
    catch (Exception)
    {
        // 如果API失败，尝试使用网页解析获取热榜
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.zhihu.com/hot");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            request.Timeout = 15000;
            
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                
                // 使用正则表达式提取热榜信息
                var titleRegex = new Regex(@"<h2\s+class=""HotItem-title"">(.*?)</h2>.*?<a\s+class=""HotItem-content""\s+href=""(.*?)""", RegexOptions.Compiled | RegexOptions.Singleline);
                var matches = titleRegex.Matches(html);
                
                int itemCount = 0;
                foreach (Match match in matches)
                {
                    if (itemCount >= count)
                        break;
                    
                    string title = match.Groups[1].Value.Trim();
                    string link = match.Groups[2].Value;
                    
                    if (!link.StartsWith("http"))
                    {
                        link = "https://www.zhihu.com" + link;
                    }
                    
                    var newsItem = new NewsItem
                    {
                        Title = title,
                        Url = link,
                        Time = DateTime.Now.ToString("HH:mm")
                    };
                    
                    result.Add(newsItem);
                    itemCount++;
                }
            }
        }
        catch
        {
            // 如果两种方法都失败，返回空列表，不添加备用数据
        }
    }
    
    return result;
}

// 获取GitHub热门项目
private static List<NewsItem> GetGitHubTrending(int count)
{
    var result = new List<NewsItem>();
    
    // 定义获取GitHub数据的函数，用于调用后立即处理响应
    Func<string, List<NewsItem>> FetchAndParse = (url) => {
        var items = new List<NewsItem>();
        try
        {
            // 创建请求并设置User-Agent和其他头信息
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            request.Headers.Add("Cache-Control", "max-age=0");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            request.Headers.Add("Sec-Fetch-User", "?1");
            request.KeepAlive = true;
            request.Timeout = 15000; // 增加超时时间到15秒
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            
            // 获取响应内容
            string html = "";
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
                
                // 使用更多种类的正则表达式尝试匹配
                var regexPatterns = new List<Tuple<string, string>> {
                    // 仓库路径和名称的正则表达式
                    Tuple.Create(@"<h2 class=""h3 lh-condensed"">\s*<a href=""([^""]+)""[^>]*>(.*?)</a>\s*</h2>", 
                                @"<p class=""col-9 color-fg-muted my-1 pr-4"">\s*(.*?)\s*</p>"),
                    
                    Tuple.Create(@"<h2 class=""f3 color-fg-muted text-normal lh-condensed"">\s*<a [^>]*href=""([^""]+)""[^>]*>(.*?)</a>", 
                                @"<p class=""color-fg-muted mb-0 pr-4"">\s*(.*?)\s*</p>"),
                    
                    Tuple.Create(@"<h1 class=""h3 lh-condensed"">\s*<a href=""([^""]+)""[^>]*>(.*?)</a>\s*</h1>", 
                                @"<p class=""col-9 color-fg-muted my-1 pr-4"">\s*(.*?)\s*</p>"),
                    
                    Tuple.Create(@"<h2 class=""[^""]*"">\s*<a\s+href=""(/[^""]+)""[^>]*>\s*(.*?)\s*</a>\s*</h2>", 
                                @"<p[^>]*class=""[^""]*color-fg-muted[^""]*""[^>]*>\s*(.*?)\s*</p>"),
                                
                    Tuple.Create(@"<article.*?>\s*<h2.*?>\s*<a\s+href=""([^""]+)"".*?>(.*?)</a>.*?</h2>.*?(?:<p.*?>(.*?)</p>)?", "")
                };
                
                // 尝试所有正则表达式，直到获得足够的结果
                foreach (var pattern in regexPatterns)
                {
                    var repoRegex = new Regex(pattern.Item1, RegexOptions.Compiled | RegexOptions.Singleline);
                    var repoMatches = repoRegex.Matches(html);
                    
                    if (repoMatches.Count > 0)
                    {
                        Regex descRegex = null;
                        MatchCollection descMatches = null;
                        
                        if (!string.IsNullOrEmpty(pattern.Item2))
                        {
                            descRegex = new Regex(pattern.Item2, RegexOptions.Compiled | RegexOptions.Singleline);
                            descMatches = descRegex.Matches(html);
                        }
                        
                        int itemCount = 0;
                        for (int i = 0; i < repoMatches.Count && itemCount < count; i++)
                        {
                            string repoPath = repoMatches[i].Groups[1].Value.Trim();
                            string repoName = repoMatches[i].Groups[2].Value.Trim();
                            repoName = Regex.Replace(repoName, @"\s+", " ").Trim();
                            repoName = Regex.Replace(repoName, "<.*?>", string.Empty);
                            
                            string description = "";
                            if (descRegex != null && i < descMatches.Count)
                            {
                                description = descMatches[i].Groups[1].Value.Trim();
                                description = Regex.Replace(description, @"\s+", " ").Trim();
                                description = Regex.Replace(description, "<.*?>", string.Empty);
                            }
                            else if (repoMatches[i].Groups.Count > 3)
                            {
                                description = repoMatches[i].Groups[3].Value.Trim();
                                description = Regex.Replace(description, @"\s+", " ").Trim();
                                description = Regex.Replace(description, "<.*?>", string.Empty);
                            }
                            
                            if (string.IsNullOrEmpty(repoName))
                                continue;
                            
                            string title = repoName;
                            if (!string.IsNullOrEmpty(description))
                            {
                                title = $"{repoName}: {description}";
                            }
                            
                            var newsItem = new NewsItem
                            {
                                Title = title,
                                Url = "https://github.com" + repoPath,
                                Time = DateTime.Now.ToString("HH:mm")
                            };
                            
                            items.Add(newsItem);
                            itemCount++;
                        }
                        
                        if (items.Count >= 3) // 至少找到几个项目就认为成功了
                        {
                            break;
                        }
                    }
                }
                
                // 尝试通用的文章匹配（基于article元素）
                if (items.Count < 3)
                {
                    var articleRegex = new Regex(@"<article[^>]*class=""Box-row""[^>]*>(.*?)</article>", RegexOptions.Compiled | RegexOptions.Singleline);
                    var articleMatches = articleRegex.Matches(html);
                    
                    if (articleMatches.Count > 0)
                    {
                        items.Clear(); // 清空之前的结果
                        
                        var hrefRegex = new Regex(@"<a\s+href=""(/[^""]+)""[^>]*>([^<]+)</a>", RegexOptions.Compiled | RegexOptions.Singleline);
                        var descRegex = new Regex(@"<p[^>]*class=""[^""]*color-fg-muted[^""]*""[^>]*>([^<]+)</p>", RegexOptions.Compiled | RegexOptions.Singleline);
                        
                        int itemCount = 0;
                        foreach (Match article in articleMatches)
                        {
                            if (itemCount >= count)
                                break;
                                
                            string articleContent = article.Groups[1].Value;
                            var hrefMatch = hrefRegex.Match(articleContent);
                            var descMatch = descRegex.Match(articleContent);
                            
                            if (hrefMatch.Success)
                            {
                                string repoPath = hrefMatch.Groups[1].Value.Trim();
                                string repoName = hrefMatch.Groups[2].Value.Trim();
                                repoName = Regex.Replace(repoName, @"\s+", " ").Trim();
                                
                                string description = "";
                                if (descMatch.Success)
                                {
                                    description = descMatch.Groups[1].Value.Trim();
                                    description = Regex.Replace(description, @"\s+", " ").Trim();
                                }
                                
                                if (string.IsNullOrEmpty(repoName))
                                    continue;
                                    
                                string title = repoName;
                                if (!string.IsNullOrEmpty(description))
                                {
                                    title = $"{repoName}: {description}";
                                }
                                
                                var newsItem = new NewsItem
                                {
                                    Title = title,
                                    Url = "https://github.com" + repoPath,
                                    Time = DateTime.Now.ToString("HH:mm")
                                };
                                
                                items.Add(newsItem);
                                itemCount++;
                            }
                        }
                    }
                }
            }
            
            // 如果尝试所有方法后仍无结果，返回空列表（不读取本地JSON文件）
        }
        catch
        {
            // 捕获请求异常，不处理，返回空列表
        }
        
        return items;
    };
    
    // 首先尝试获取GitHub Trending数据
    result = FetchAndParse("https://github.com/trending");
    
    // 如果第一次请求失败，尝试中文页面
    if (result.Count == 0)
    {
        result = FetchAndParse("https://github.com/trending?spoken_language_code=zh");
    }
    
    // 如果所有方法都失败，返回空列表
    return result.Take(count).ToList();
}

// 获取小红书推荐笔记
private static List<NewsItem> GetXiaohongshuNotes(int count)
{
    var result = new List<NewsItem>();
    try
    {
        // 创建请求并设置User-Agent和其他头信息
        var request = (HttpWebRequest)WebRequest.Create("https://www.xiaohongshu.com/explore");
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.Add("Cache-Control", "max-age=0");
        request.Headers.Add("Upgrade-Insecure-Requests", "1");
        request.Headers.Add("Referer", "https://www.xiaohongshu.com/");
        request.Headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        request.Headers.Add("Sec-Fetch-User", "?1");
        request.KeepAlive = true;
        request.Timeout = 15000;
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        
        // 获取响应内容
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
        {
            string html = reader.ReadToEnd();
            
            // 使用正则表达式提取笔记信息
            var noteRegex = new Regex(@"<a\s+href=""(/explore/[^""]+)""[^>]*>\s*<div[^>]*>\s*<h3[^>]*>(.*?)</h3>\s*<div[^>]*>(.*?)</div>", RegexOptions.Compiled | RegexOptions.Singleline);
            var matches = noteRegex.Matches(html);
            
            int itemCount = 0;
            foreach (Match match in matches)
            {
                if (itemCount >= count)
                    break;
                
                string path = match.Groups[1].Value.Trim();
                string title = match.Groups[2].Value.Trim();
                string author = match.Groups[3].Value.Trim();
                
                // 清理HTML标签
                title = Regex.Replace(title, "<.*?>", string.Empty);
                author = Regex.Replace(author, "<.*?>", string.Empty);
                
                if (string.IsNullOrEmpty(title))
                    continue;
                
                var newsItem = new NewsItem
                {
                    Title = $"{title} - {author}",
                    Url = "https://www.xiaohongshu.com" + path,
                    Time = DateTime.Now.ToString("HH:mm")
                };
                
                result.Add(newsItem);
                itemCount++;
            }
        }
    }
    catch (Exception)
    {
        // 如果上述方法失败，尝试使用API获取
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://edith.xiaohongshu.com/api/sns/web/v1/homefeed");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            request.Headers.Add("Origin", "https://www.xiaohongshu.com");
            request.Headers.Add("Referer", "https://www.xiaohongshu.com/");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            
            // 构建请求体
            var requestBody = "{\"cursor_score\":\"\",\"num\":12,\"refresh_type\":1,\"note_index\":0,\"unread_begin_note_id\":\"\",\"unread_end_note_id\":\"\",\"unread_note_count\":0,\"category\":\"homefeed_recommend\"}";
            var data = Encoding.UTF8.GetBytes(requestBody);
            request.ContentLength = data.Length;
            
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                dynamic feedData = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                
                if (feedData != null && feedData.data != null && feedData.data.items != null)
                {
                    int itemCount = 0;
                    foreach (var item in feedData.data.items)
                    {
                        if (itemCount >= count)
                            break;
                        
                        string title = item.note.display_title;
                        string noteId = item.note.id;
                        string userId = item.note.user.nickname;
                        
                        if (string.IsNullOrEmpty(title))
                            continue;
                        
                        var newsItem = new NewsItem
                        {
                            Title = $"{title} - {userId}",
                            Url = $"https://www.xiaohongshu.com/explore/{noteId}",
                            Time = DateTime.Now.ToString("HH:mm")
                        };
                        
                        result.Add(newsItem);
                        itemCount++;
                    }
                }
            }
        }
        catch
        {
            // 如果两种方法都失败，返回空列表，不添加备用数据
        }
    }
    
    return result;
}
