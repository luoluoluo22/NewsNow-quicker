using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quicker.Public;
using System.Text.RegularExpressions;
using System.Text;
using HtmlAgilityPack;
using System.IO;

// NewsNow Quicker版本 - C#代码文件

#region 数据模型
class NewsItem
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string Source { get; set; }
    public string Time { get; set; }
}

class NewsSource
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Category { get; set; }
    public string ApiEndpoint { get; set; }
    public Func<JObject, List<NewsItem>> DataParser { get; set; }
}
#endregion

public class NewsNow
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly string LogPath = @"F:\vscodeNotes\03-project\newsnow\newsnow_log.txt";
    
    public static void OnWindowCreated(Window win, IDictionary<string, object> dataContext, ICustomWindowContext winContext)
    {
        // 记录初始数据
        LogDataContextInfo(dataContext, "OnWindowCreated");
        
        // 设置分类按钮的样式
        UpdateCategoryButtonStyle(win, "hot");
    }

    public static void OnWindowLoaded(Window win, IDictionary<string, object> dataContext, ICustomWindowContext winContext)
    {
        // 记录加载后的数据
        LogDataContextInfo(dataContext, "OnWindowLoaded");
        
        // 尝试直接设置控件的ItemsSource
        TrySetItemsDirectly(win, dataContext);
        
        // 为刷新按钮添加事件处理
        var refreshButton = win.FindName("RefreshButton") as Button;
        if (refreshButton != null)
        {
            refreshButton.Click += (sender, args) =>
            {
                // 刷新时尝试再次设置数据
                TrySetItemsDirectly(win, dataContext);
                
                // 刷新时记录日志
                LogDataContextInfo(dataContext, "Refresh_Clicked");
                
                // 提示信息
                MessageBox.Show("已重新设置数据源并记录到日志文件：" + LogPath, "NewsNow");
            };
        }
    }

    public static bool OnButtonClicked(string controlName, object controlTag, Window win, IDictionary<string, object> dataContext, ICustomWindowContext winContext)
    {
        // 记录按钮点击事件
        LogDataContextInfo(dataContext, "OnButtonClicked_" + controlName);
        return false;
    }

    #region 辅助方法

    // 记录数据上下文信息到日志文件
    private static void LogDataContextInfo(IDictionary<string, object> dataContext, string eventName)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("============== " + eventName + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ==============");
            
            // 记录所有键值对
            foreach (var key in dataContext.Keys)
            {
                sb.AppendLine("Key: " + key);
                
                if (dataContext[key] == null)
                {
                    sb.AppendLine("Value: null");
                }
                else if (dataContext[key] is string)
                {
                    sb.AppendLine("Value (string): " + dataContext[key]);
                }
                else if (dataContext[key] is bool)
                {
                    sb.AppendLine("Value (bool): " + dataContext[key]);
                }
                else if (dataContext[key] is Dictionary<string, object>)
                {
                    var dict = dataContext[key] as Dictionary<string, object>;
                    sb.AppendLine("Value (Dictionary): Count = " + dict.Count);
                    
                    sb.AppendLine("  Dictionary Contents:");
                    foreach (var dictKey in dict.Keys)
                    {
                        sb.AppendLine("    Key: " + dictKey);
                        
                        if (dict[dictKey] == null)
                        {
                            sb.AppendLine("    Value: null");
                        }
                        else if (dict[dictKey] is IList<object>)
                        {
                            var list = dict[dictKey] as IList<object>;
                            sb.AppendLine("    Value (List): Count = " + list.Count);
                            
                            // 记录前3个项目的详细信息
                            for (int i = 0; i < Math.Min(list.Count, 3); i++)
                            {
                                sb.AppendLine("      Item " + i + ":");
                                if (list[i] is Dictionary<string, object>)
                                {
                                    var itemDict = list[i] as Dictionary<string, object>;
                                    foreach (var itemKey in itemDict.Keys)
                                    {
                                        sb.AppendLine("        " + itemKey + ": " + itemDict[itemKey]);
                                    }
                                }
                                else
                                {
                                    sb.AppendLine("        Value: " + list[i]);
                                }
                            }
                        }
                        else
                        {
                            sb.AppendLine("    Value: " + dict[dictKey]);
                        }
                    }
                }
                else if (dataContext[key] is IList<Dictionary<string, object>>)
                {
                    var list = dataContext[key] as IList<Dictionary<string, object>>;
                    sb.AppendLine("Value (List<Dictionary>): Count = " + list.Count);
                    
                    // 记录前3个项目的详细信息
                    for (int i = 0; i < Math.Min(list.Count, 3); i++)
                    {
                        sb.AppendLine("  Item " + i + ":");
                        foreach (var itemKey in list[i].Keys)
                        {
                            sb.AppendLine("    " + itemKey + ": " + list[i][itemKey]);
                        }
                    }
                }
                else
                {
                    sb.AppendLine("Value (other): Type = " + dataContext[key].GetType().Name + ", ToString = " + dataContext[key].ToString());
                    
                    // 尝试反射获取属性
                    if (dataContext[key] != null)
                    {
                        var properties = dataContext[key].GetType().GetProperties();
                        if (properties.Length > 0)
                        {
                            sb.AppendLine("  Properties:");
                            foreach (var prop in properties)
                            {
                                try
                                {
                                    var value = prop.GetValue(dataContext[key]);
                                    string valueText = "null";
                                    if (value != null)
                                    {
                                        valueText = value.ToString();
                                    }
                                    sb.AppendLine("    " + prop.Name + ": " + valueText);
                                }
                                catch
                                {
                                    sb.AppendLine("    " + prop.Name + ": <error reading value>");
                                }
                            }
                        }
                    }
                }
                
                sb.AppendLine();
            }
            
            sb.AppendLine("======================================================");
            sb.AppendLine();
            
            // 创建目录（如果不存在）
            string directory = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 追加到日志文件
            File.AppendAllText(LogPath, sb.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show("记录日志时出错: " + ex.Message, "NewsNow", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 更新分类按钮样式
    private static void UpdateCategoryButtonStyle(Window win, string selectedCategory)
    {
        // 获取所有分类按钮
        foreach (var button in FindAllCategoryButtons(win))
        {
            string category = button.Name.Substring("BtnCategory_".Length).ToLower();
            
            if (category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase))
            {
                // 选中的分类按钮样式
                button.FontWeight = FontWeights.Bold;
                button.BorderThickness = new Thickness(0, 0, 0, 2);
                SolidColorBrush blueBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#1976D2"));
                button.BorderBrush = blueBrush;
                button.Foreground = blueBrush;
            }
            else
            {
                // 未选中的分类按钮样式
                button.FontWeight = FontWeights.Normal;
                button.BorderThickness = new Thickness(0);
                SolidColorBrush grayBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#666666"));
                button.Foreground = grayBrush;
            }
        }
    }

    // 查找窗口中所有的分类按钮
    private static List<Button> FindAllCategoryButtons(Window win)
    {
        var buttons = new List<Button>();
        
        foreach (var child in LogicalTreeHelper.GetChildren(win))
        {
            if (child is Grid)
            {
                Grid grid = (Grid)child;
                FindCategoryButtonsInElement(grid, buttons);
            }
        }
        
        return buttons;
    }

    // 递归查找元素中的分类按钮
    private static void FindCategoryButtonsInElement(DependencyObject element, List<Button> buttons)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(element);
        
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);
            
            if (child is Button)
            {
                Button button = (Button)child;
                if (button.Name != null && button.Name.StartsWith("BtnCategory_"))
                {
                    buttons.Add(button);
                }
            }
            
            FindCategoryButtonsInElement(child, buttons);
        }
    }

    // 尝试直接设置ItemsSource
    private static void TrySetItemsDirectly(Window win, IDictionary<string, object> dataContext)
    {
        try
        {
            // 获取调试文本控件
            var debugText = win.FindName("DebugText") as TextBlock;
            StringBuilder debugInfo = new StringBuilder();
            
            if (dataContext.ContainsKey("newsData") && dataContext["newsData"] is Dictionary<string, object>)
            {
                var newsData = dataContext["newsData"] as Dictionary<string, object>;
                
                // 添加调试信息
                debugInfo.AppendLine("newsData字典包含以下键：");
                foreach (var key in newsData.Keys)
                {
                    string description = "非列表类型";
                    if (newsData[key] is IList<object>)
                    {
                        var list = newsData[key] as IList<object>;
                        description = list.Count + " 项";
                    }
                    debugInfo.AppendLine("- " + key + ": " + description);
                }
                
                // 设置V2EX数据
                var v2exControl = win.FindName("V2exItemsControl") as ItemsControl;
                if (v2exControl != null && newsData.ContainsKey("v2ex"))
                {
                    var v2exData = newsData["v2ex"];
                    if (v2exData is System.Collections.IEnumerable)
                    {
                        v2exControl.ItemsSource = (System.Collections.IEnumerable)v2exData;
                        
                        string typeName = "null";
                        if (v2exData != null)
                        {
                            typeName = v2exData.GetType().Name;
                        }
                        debugInfo.AppendLine("已设置V2EX数据源，类型：" + typeName);
                        
                        // 显示前三条数据的标题
                        if (v2exData is IList<object>)
                        {
                            var v2exList = v2exData as IList<object>;
                            if (v2exList.Count > 0)
                            {
                                debugInfo.AppendLine("V2EX前三条数据：");
                                int count = Math.Min(3, v2exList.Count);
                                for (int i = 0; i < count; i++)
                                {
                                    var item = v2exList[i];
                                    var props = item.GetType().GetProperties();
                                    var propTitle = props.FirstOrDefault(p => p.Name == "Title");
                                    string title = "未知标题";
                                    if (propTitle != null)
                                    {
                                        object titleObj = propTitle.GetValue(item);
                                        if (titleObj != null)
                                        {
                                            title = titleObj.ToString();
                                        }
                                    }
                                    debugInfo.AppendLine("  " + (i+1) + ". " + title);
                                }
                            }
                        }
                    }
                    else
                    {
                        debugInfo.AppendLine("V2EX数据不是可枚举类型，无法设置ItemsSource");
                    }
                }
                else
                {
                    debugInfo.AppendLine("未找到V2EX控件或v2ex数据");
                }
                
                // 设置微博数据
                var weiboControl = win.FindName("WeiboItemsControl") as ItemsControl;
                if (weiboControl != null && newsData.ContainsKey("weibo"))
                {
                    var weiboData = newsData["weibo"];
                    if (weiboData is System.Collections.IEnumerable)
                    {
                        weiboControl.ItemsSource = (System.Collections.IEnumerable)weiboData;
                        
                        string countText = "0";
                        if (weiboData is IList<object>)
                        {
                            var list = weiboData as IList<object>;
                            countText = list.Count.ToString();
                        }
                        debugInfo.AppendLine("已设置微博数据源，共" + countText + "项");
                    }
                    else
                    {
                        debugInfo.AppendLine("微博数据不是可枚举类型，无法设置ItemsSource");
                    }
                }
                else
                {
                    debugInfo.AppendLine("未找到微博控件或weibo数据");
                }
                
                // 设置IT之家数据
                var ithomeControl = win.FindName("IthomeItemsControl") as ItemsControl;
                if (ithomeControl != null && newsData.ContainsKey("ithome"))
                {
                    var ithomeData = newsData["ithome"];
                    if (ithomeData is System.Collections.IEnumerable)
                    {
                        ithomeControl.ItemsSource = (System.Collections.IEnumerable)ithomeData;
                        
                        string countText = "0";
                        if (ithomeData is IList<object>)
                        {
                            var list = ithomeData as IList<object>;
                            countText = list.Count.ToString();
                        }
                        debugInfo.AppendLine("已设置IT之家数据源，共" + countText + "项");
                    }
                    else
                    {
                        debugInfo.AppendLine("IT之家数据不是可枚举类型，无法设置ItemsSource");
                    }
                }
                else
                {
                    debugInfo.AppendLine("未找到IT之家控件或ithome数据");
                }
            }
            else
            {
                debugInfo.AppendLine("newsData不存在或不是Dictionary类型");
                if (dataContext.ContainsKey("newsData"))
                {
                    string typeName = "null";
                    if (dataContext["newsData"] != null)
                    {
                        typeName = dataContext["newsData"].GetType().Name;
                    }
                    debugInfo.AppendLine("newsData类型：" + typeName);
                }
                
                // 显示所有dataContext中的键
                debugInfo.AppendLine("\ndataContext中的所有键：");
                foreach (var key in dataContext.Keys)
                {
                    string typeName = "null";
                    if (dataContext[key] != null)
                    {
                        typeName = dataContext[key].GetType().Name;
                    }
                    debugInfo.AppendLine("- " + key + ": " + typeName);
                }
                
                MessageBox.Show("newsData不存在或不是Dictionary类型", "数据绑定错误");
            }
            
            // 更新调试文本
            if (debugText != null)
            {
                debugText.Text = debugInfo.ToString();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("设置数据源时出错: " + ex.Message, "NewsNow", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion 
} 