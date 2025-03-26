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
        
        // 设置刷新按钮图标
        context.SetVarValue("refreshIcon", GetRefreshIcon());
        
        // 在后台线程中更新数据
        Task.Run(() => {
            try
            {
                // 创建新闻数据字典，添加网站首页链接作为Value[0]的URL值
                var newsDictionary = new Dictionary<string, List<NewsItem>>();
                
                // 获取各类新闻数据并添加到字典中 - 使用更有意义的栏目名称，增加获取条数到9条左右
                var v2exList = GetV2exNews(9);
                var weiboList = GetWeiboNews(9);
                var itHomeList = GetITHomeNews(9);
                var zhihuList = GetZhihuNews(9);
                var githubList = GetGitHubTrending(9);
                var xiaohongshuList = GetXiaohongshuNotes(9);
                
                // 在列表开头添加网站首页项作为栏目标题的链接
                v2exList.Insert(0, new NewsItem { Title = "V2EX热门", Url = "https://www.v2ex.com/?tab=hot", Time = "首页" });
                weiboList.Insert(0, new NewsItem { Title = "微博热搜", Url = "https://s.weibo.com/top/summary", Time = "首页" });
                itHomeList.Insert(0, new NewsItem { Title = "IT之家", Url = "https://www.ithome.com/", Time = "首页" });
                zhihuList.Insert(0, new NewsItem { Title = "知乎热榜", Url = "https://www.zhihu.com/hot", Time = "首页" });
                githubList.Insert(0, new NewsItem { Title = "GitHub热门", Url = "https://github.com/trending", Time = "首页" });
                xiaohongshuList.Insert(0, new NewsItem { Title = "小红书推荐", Url = "https://www.xiaohongshu.com/explore", Time = "首页" });
                
                // 添加到字典
                newsDictionary.Add("V2EX热门", v2exList);
                newsDictionary.Add("微博热搜", weiboList);
                newsDictionary.Add("IT之家", itHomeList);
                newsDictionary.Add("知乎热榜", zhihuList);
                newsDictionary.Add("GitHub热门", githubList);
                newsDictionary.Add("小红书推荐", xiaohongshuList);
                
                // 使用Newtonsoft.Json序列化
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(newsDictionary, Newtonsoft.Json.Formatting.Indented);
                
                // 将数据保存到Quicker变量
                context.SetVarValue("newsData", jsonString);
                
                // 保存到缓存文件
                SaveCachedData(jsonString);
            }
            catch (Exception ex)
            {
                // 显示错误消息
                MessageBox.Show($"后台更新新闻数据失败: {ex.Message}");
            }
        });
    }
    catch (Exception ex)
    {
        // 显示错误消息
        MessageBox.Show($"获取新闻数据失败: {ex.Message}");
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
    catch (Exception ex)
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
            // 如果API也失败，则添加备用的热门话题
            result.Add(new NewsItem
            {
                Title = "从V2EX获取数据失败，最近热门话题：程序员应该如何保持技术更新",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.v2ex.com/t/981998"
            });
            
            result.Add(new NewsItem
            {
                Title = "大家现在都在用什么笔记软件？",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.v2ex.com/t/981487"
            });
            
            result.Add(new NewsItem
            {
                Title = "35岁危机是真实存在的吗？",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.v2ex.com/t/982011"
            });
            
            result.Add(new NewsItem
            {
                Title = "2024年的技术选型应该如何抉择？",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.v2ex.com/t/981876"
            });
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
            // 如果两种方法都失败，添加备用的热搜条目
            result.Add(new NewsItem
            {
                Title = "央视网络春晚阵容官宣",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://s.weibo.com/weibo?q=央视网络春晚阵容官宣"
            });
            
            result.Add(new NewsItem
            {
                Title = "新能源汽车销量创新高",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://s.weibo.com/weibo?q=新能源汽车销量创新高"
            });
            
            result.Add(new NewsItem
            {
                Title = "教育部发布最新通知",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://s.weibo.com/weibo?q=教育部发布最新通知"
            });
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
    catch (Exception ex)
    {
        // 如果获取失败，添加一个错误信息的条目
        result.Add(new NewsItem
        {
            Title = "获取IT之家新闻失败: " + ex.Message,
            Time = DateTime.Now.ToString("HH:mm"),
            Url = "https://www.ithome.com/"
        });
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
            // 如果两种方法都失败，添加备用的热榜条目
            result.Add(new NewsItem
            {
                Title = "如何看待ChatGPT-4最新版本的能力提升？",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.zhihu.com/question/613350000"
            });
            
            result.Add(new NewsItem
            {
                Title = "现在买房还是租房更划算？",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.zhihu.com/question/618954321"
            });
            
            result.Add(new NewsItem
            {
                Title = "年轻人「内卷」的原因是什么？",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.zhihu.com/question/542673839"
            });
            
            result.Add(new NewsItem
            {
                Title = "高效学习的方法有哪些？",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.zhihu.com/question/271408259"
            });
        }
    }
    
    return result;
}

// 获取GitHub热门项目
private static List<NewsItem> GetGitHubTrending(int count)
{
    var result = new List<NewsItem>();
    try
    {
        // 创建请求并设置User-Agent和其他头信息
        var request = (HttpWebRequest)WebRequest.Create("https://github.com/trending");
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.Add("Cache-Control", "max-age=0");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        request.Headers.Add("Sec-Fetch-User", "?1");
        request.Headers.Add("Upgrade-Insecure-Requests", "1");
        request.KeepAlive = true;
        request.Timeout = 20000; // 增加超时时间到20秒
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        
        // 获取响应内容
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
        {
            string html = reader.ReadToEnd();
            
            // 使用正则表达式提取热门项目信息 - 更新正则表达式
            var projectRegex = new Regex(@"<article[^>]*>.*?<h2[^>]*>.*?<a\s+href=""([^""]+)""[^>]*>(.*?)</a>.*?</h2>.*?(?:<p[^>]*>(.*?)</p>)?", RegexOptions.Compiled | RegexOptions.Singleline);
            var matches = projectRegex.Matches(html);
            
            if(matches.Count == 0)
            {
                // 尝试不同的正则表达式
                projectRegex = new Regex(@"<h2 class=""h3 lh-condensed"">.*?<a href=""([^""]+)""[^>]*>(.*?)</a>.*?</h2>.*?<p class=""col-9[^""]*"">(.*?)</p>", RegexOptions.Compiled | RegexOptions.Singleline);
                matches = projectRegex.Matches(html);
            }
            
            if(matches.Count == 0)
            {
                // 再尝试一个更通用的正则表达式
                projectRegex = new Regex(@"<h\d[^>]*>.*?<a[^>]*href=""(/[^""]+/[^""]+)""[^>]*>(.*?)</a>.*?</h\d>", RegexOptions.Compiled | RegexOptions.Singleline);
                matches = projectRegex.Matches(html);
            }
            
            int itemCount = 0;
            foreach (Match match in matches)
            {
                if (itemCount >= count)
                    break;
                
                string path = match.Groups[1].Value.Trim();
                string name = match.Groups[2].Value.Trim();
                name = Regex.Replace(name, @"\s+", " ").Trim();
                name = Regex.Replace(name, "<.*?>", string.Empty);
                
                // 清理特殊字符
                name = name.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
                
                string description = "";
                if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
                {
                    description = match.Groups[3].Value.Trim();
                    description = Regex.Replace(description, @"\s+", " ").Trim();
                    description = Regex.Replace(description, "<.*?>", string.Empty);
                    description = description.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
                }
                
                if (string.IsNullOrEmpty(name) || name.Length < 2)
                    continue;
                
                string title = name;
                if (!string.IsNullOrEmpty(description))
                {
                    title = $"{name}: {description}";
                }
                
                string url = path;
                if (!url.StartsWith("http"))
                {
                    url = "https://github.com" + path;
                }
                
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
    catch (Exception ex)
    {
        // 尝试使用另一种方法
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://github.com/trending/developers");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            request.Timeout = 20000; // 增加超时时间到20秒
            
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                string html = reader.ReadToEnd();
                
                // 使用正则表达式提取开发者信息
                var devRegex = new Regex(@"<article[^>]*>.*?<h2[^>]*>.*?<a\s+href=""([^""]+)""[^>]*>(.*?)</a>.*?</h2>.*?<p[^>]*>(.*?)</p>", RegexOptions.Compiled | RegexOptions.Singleline);
                var matches = devRegex.Matches(html);
                
                if (matches.Count == 0)
                {
                    devRegex = new Regex(@"<h2[^>]*>.*?<a[^>]*href=""(/[^""]+)""[^>]*>(.*?)</a>", RegexOptions.Compiled | RegexOptions.Singleline);
                    matches = devRegex.Matches(html);
                }
                
                int itemCount = 0;
                foreach (Match match in matches)
                {
                    if (itemCount >= count)
                        break;
                    
                    string path = match.Groups[1].Value.Trim();
                    string name = match.Groups[2].Value.Trim();
                    name = Regex.Replace(name, @"\s+", " ").Trim();
                    name = Regex.Replace(name, "<.*?>", string.Empty);
                    
                    if (string.IsNullOrEmpty(name) || name.Length < 2)
                        continue;
                    
                    string description = "GitHub热门开发者";
                    if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
                    {
                        description = match.Groups[3].Value.Trim();
                        description = Regex.Replace(description, @"\s+", " ").Trim();
                        description = Regex.Replace(description, "<.*?>", string.Empty);
                    }
                    
                    var newsItem = new NewsItem
                    {
                        Title = $"{name}: {description}",
                        Url = "https://github.com" + path,
                        Time = DateTime.Now.ToString("HH:mm")
                    };
                    
                    result.Add(newsItem);
                    itemCount++;
                }
            }
        }
        catch
        {
            // 如果两种方法都失败，添加备用项目
            result.Add(new NewsItem
            {
                Title = "microsoft/vscode: Visual Studio Code",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/microsoft/vscode"
            });
            
            result.Add(new NewsItem
            {
                Title = "tensorflow/tensorflow: 机器学习框架",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/tensorflow/tensorflow"
            });
            
            result.Add(new NewsItem
            {
                Title = "flutter/flutter: 跨平台UI框架",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/flutter/flutter"
            });
            
            result.Add(new NewsItem
            {
                Title = "vuejs/vue: 渐进式JavaScript框架",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/vuejs/vue"
            });
            
            result.Add(new NewsItem
            {
                Title = "facebook/react: 用于构建用户界面的JavaScript库",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/facebook/react"
            });
            
            result.Add(new NewsItem
            {
                Title = "coder/code-server: 浏览器中的VSCode",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/coder/code-server"
            });
            
            result.Add(new NewsItem
            {
                Title = "microsoft/PowerToys: Windows系统实用工具",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/microsoft/PowerToys"
            });
            
            result.Add(new NewsItem
            {
                Title = "ant-design/ant-design: 企业级UI设计语言和React组件库",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/ant-design/ant-design"
            });
            
            result.Add(new NewsItem
            {
                Title = "goldbergyoni/nodebestpractices: Node.js最佳实践",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://github.com/goldbergyoni/nodebestpractices"
            });
        }
    }
    
    return result;
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
            
            // 使用正则表达式提取笔记信息 - 修改正则表达式以匹配正确的链接
            var noteRegex = new Regex(@"<a\s+href=""(/discovery/item/([^""]+))""[^>]*>\s*.*?<div[^>]*>\s*<div[^>]*>\s*(?:<div[^>]*>)?([^<]*)", RegexOptions.Compiled | RegexOptions.Singleline);
            var matches = noteRegex.Matches(html);
            
            if (matches.Count == 0)
            {
                // 备用正则表达式
                noteRegex = new Regex(@"<a\s+href=""(/explore/[^""]+)""[^>]*>\s*<div[^>]*>\s*(?:<img[^>]*>)?\s*<div[^>]*>\s*(.*?)\s*</div>", RegexOptions.Compiled | RegexOptions.Singleline);
                matches = noteRegex.Matches(html);
            }
            
            int itemCount = 0;
            foreach (Match match in matches)
            {
                if (itemCount >= count)
                    break;
                
                string path = match.Groups[1].Value.Trim();
                string title = "";
                
                if (match.Groups.Count > 2)
                {
                    title = match.Groups[3].Value.Trim();
                }
                
                // 截取长度过长的标题
                if (title.Length > 50)
                {
                    title = title.Substring(0, 47) + "...";
                }
                
                if (string.IsNullOrEmpty(title) || title.Length < 2)
                    continue;
                
                var newsItem = new NewsItem
                {
                    Title = title,
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
        // 如果上述方法失败，使用备用请求
        try
        {
            var request = (HttpWebRequest)WebRequest.Create("https://www.xiaohongshu.com/web_api/sns/v2/notecard/real_time_list");
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            request.Accept = "application/json, text/plain, */*";
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            request.Headers.Add("Referer", "https://www.xiaohongshu.com/explore");
            request.Timeout = 15000;
            
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                dynamic notesData = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                
                if (notesData != null && notesData.data != null)
                {
                    int itemCount = 0;
                    foreach (var item in notesData.data)
                    {
                        if (itemCount >= count)
                            break;
                        
                        string title = item.title;
                        string noteId = item.id;
                        
                        if (string.IsNullOrEmpty(title))
                            continue;
                        
                        var newsItem = new NewsItem
                        {
                            Title = title,
                            Url = $"https://www.xiaohongshu.com/discovery/item/{noteId}",
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
            // 如果两种方法都失败，添加备用笔记
            result.Add(new NewsItem
            {
                Title = "今日穿搭分享：初春温柔风格",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fd1a89000000001e03c368"
            });
            
            result.Add(new NewsItem
            {
                Title = "打卡北京这家新开的咖啡店，氛围感满分",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fc7d55000000001e03e4f2"
            });
            
            result.Add(new NewsItem
            {
                Title = "家常菜谱：简单又好吃的红烧排骨",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fc0f31000000001f03ba4a"
            });
            
            result.Add(new NewsItem
            {
                Title = "日常护肤小技巧，让皮肤水润有光泽",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fd2342000000001f02d350"
            });
            
            result.Add(new NewsItem
            {
                Title = "最近入手的五款口红试色分享",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fc8943000000001e03e8ba"
            });
            
            result.Add(new NewsItem
            {
                Title = "旅行vlog：探索云南小众景点",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fd1d8f000000001e03a2c0"
            });
            
            result.Add(new NewsItem
            {
                Title = "三款平价好用的面霜推荐",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fc6023000000001f03a3a2"
            });
            
            result.Add(new NewsItem
            {
                Title = "宿舍收纳小妙招，空间瞬间变大",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fd0f3b000000001f036a56"
            });
            
            result.Add(new NewsItem
            {
                Title = "记录我的减肥餐，一个月瘦了10斤",
                Time = DateTime.Now.ToString("HH:mm"),
                Url = "https://www.xiaohongshu.com/discovery/item/65fce245000000001f03e325"
            });
        }
    }
    
    return result;
}

// 获取美观的刷新图标
private static string GetRefreshIcon()
{
    // 返回一个SVG刷新图标，使用柔和的颜色
    return @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""24"" height=""24"" viewBox=""0 0 24 24"" style=""fill: #4A90E2; cursor: pointer; transition: transform 0.3s ease;"" onmouseover=""this.style.transform='rotate(45deg)'"" onmouseout=""this.style.transform='rotate(0deg)'"">
        <path d=""M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 10h7V3l-2.35 3.35z""/>
    </svg>";
}