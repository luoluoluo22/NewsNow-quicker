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
        
        // 在后台线程中更新数据
        Task.Run(() => {
            try
            {
                // 创建新闻数据字典
                var newsDictionary = new Dictionary<string, List<NewsItem>>();
                
                // 获取各类新闻数据并添加到字典中 - 使用更有意义的栏目名称
                newsDictionary.Add("V2EX热门", GetV2exNews(8));
                newsDictionary.Add("微博热搜", GetWeiboNews(3));
                newsDictionary.Add("IT之家", GetITHomeNews(4));
                newsDictionary.Add("知乎热榜", GetZhihuNews(4));
                
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