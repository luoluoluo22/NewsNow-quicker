using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
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
        
        // 设置窗口拖动
        SetupWindowDragging(win);
        
        // 设置窗口控制按钮
        SetupWindowControlButtons(win);
    }

    public static void OnWindowLoaded(Window win, IDictionary<string, object> dataContext, ICustomWindowContext winContext)
    {
        // 记录加载后的数据
        LogDataContextInfo(dataContext, "OnWindowLoaded");
        
        // 设置栏目标题
        UpdateColumnTitles(win, dataContext);
        
        // 尝试直接设置控件的ItemsSource
        TrySetItemsDirectly(win, dataContext);
        
        // 为刷新按钮添加事件处理
        var refreshButton = win.FindName("RefreshButton") as Button;
        if (refreshButton != null)
        {
            refreshButton.Click += (sender, args) =>
            {
                // 更新栏目标题
                UpdateColumnTitles(win, dataContext);
                
                // 刷新时尝试再次设置数据
                TrySetItemsDirectly(win, dataContext);
                
                // 刷新时记录日志
                LogDataContextInfo(dataContext, "Refresh_Clicked");
                
                // 提示信息
                MessageBox.Show("已重新设置数据源并记录到日志文件：" + LogPath, "NewsNow");
            };
        }

        // 为新闻项添加点击事件处理
        AttachClickHandlers(win, winContext);
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

                // 动态处理每个栏目
                var columnKeys = newsData.Keys.ToList();
                for (int i = 0; i < columnKeys.Count && i < 3; i++)
                {
                    string columnKey = columnKeys[i];
                    // 根据栏目标题查找对应的ItemsControl
                    var itemsControl = win.FindName("column" + (i + 1) + "TitleItemsControl") as ItemsControl;
                    if (itemsControl != null && newsData.ContainsKey(columnKey))
                    {
                        var columnData = newsData[columnKey];
                        if (columnData is System.Collections.IEnumerable)
                        {
                            itemsControl.ItemsSource = (System.Collections.IEnumerable)columnData;
                            
                            string typeName = "null";
                            if (columnData != null)
                            {
                                typeName = columnData.GetType().Name;
                            }
                            debugInfo.AppendLine(string.Format("已设置{0}数据源，类型：{1}", columnKey, typeName));
                            
                            // 显示前三条数据的标题
                            if (columnData is IList<object>)
                            {
                                var columnList = columnData as IList<object>;
                                if (columnList.Count > 0)
                                {
                                    debugInfo.AppendLine(columnKey + "前三条数据：");
                                    int count = Math.Min(3, columnList.Count);
                                    for (int j = 0; j < count; j++)
                                    {
                                        var item = columnList[j];
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
                                        debugInfo.AppendLine("  " + (j + 1) + ". " + title);
                                    }
                                }
                            }
                        }
                        else
                        {
                            debugInfo.AppendLine(columnKey + "数据不是可枚举类型，无法设置ItemsSource");
                        }
                    }
                    else
                    {
                        debugInfo.AppendLine("未找到column" + (i + 1) + "TitleItemsControl控件或" + columnKey + "数据");
                    }
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

    // 为所有新闻列表添加点击事件处理
    private static void AttachClickHandlers(Window win, ICustomWindowContext winContext)
    {
        // 为每个栏目的ItemsControl添加点击事件处理
        for (int i = 1; i <= 3; i++)
        {
            var itemsControl = win.FindName("column" + i + "TitleItemsControl") as ItemsControl;
            AttachItemsControlClickHandler(itemsControl, winContext);
        }
    }

    // 为单个ItemsControl添加点击事件处理
    private static void AttachItemsControlClickHandler(ItemsControl itemsControl, ICustomWindowContext winContext)
    {
        if (itemsControl == null) return;
        
        // 使用事件委托方式为ItemsControl添加事件处理
        // 因为ItemsControl的Items是动态生成的，所以需要在容器级别添加事件
        itemsControl.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler((sender, e) => 
        {
            // 获取点击的元素
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource == null) return;
            
            // 向上查找Border元素
            Border targetBorder = FindParent<Border>(originalSource);
            if (targetBorder != null && targetBorder.Name == "NewsItemBorder")
            {
                string url = targetBorder.Tag as string;
                if (!string.IsNullOrEmpty(url))
                {
                    // 打开URL
                    try
                    {
                        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                        {
                            url = "https://" + url;
                        }
                        System.Diagnostics.Process.Start(url);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("无法打开链接: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }));
    }

    // 查找指定类型的父元素
    private static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        // 获取父元素
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        
        // 如果找不到父元素，返回null
        if (parentObject == null) return null;
        
        // 如果父元素是我们要找的类型，返回它
        T parent = parentObject as T;
        if (parent != null) return parent;
        
        // 否则，继续向上查找
        return FindParent<T>(parentObject);
    }

    // 设置窗口拖动
    private static void SetupWindowDragging(Window win)
    {
        var titleBar = win.FindName("TitleBar") as UIElement;
        if (titleBar != null)
        {
            titleBar.MouseLeftButtonDown += (sender, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    win.DragMove();
                }
            };
        }
    }

    // 设置窗口控制按钮
    private static void SetupWindowControlButtons(Window win)
    {
        var closeButton = win.FindName("CloseButton") as Button;
        if (closeButton != null)
        {
            closeButton.Click += (sender, e) =>
            {
                win.Close();
            };
        }
        
        var minimizeButton = win.FindName("MinimizeButton") as Button;
        if (minimizeButton != null)
        {
            minimizeButton.Click += (sender, e) =>
            {
                win.WindowState = WindowState.Minimized;
            };
        }
    }

    // 更新栏目标题
    private static void UpdateColumnTitles(Window win, IDictionary<string, object> dataContext)
    {
        if (dataContext.ContainsKey("newsData") && dataContext["newsData"] is Dictionary<string, object>)
        {
            var newsData = dataContext["newsData"] as Dictionary<string, object>;
            var columnKeys = newsData.Keys.ToList();

            // 更新栏目标题
            for (int i = 1; i <= 3; i++)
            {
                var titleBlock = win.FindName("Column" + i + "Title") as TextBlock;
                if (titleBlock != null && i <= columnKeys.Count)
                {
                    string columnKey = columnKeys[i - 1];
                    // 将键名转换为更友好的显示格式
                    string displayTitle = FormatColumnTitle(columnKey);
                    titleBlock.Text = displayTitle;
                }
            }
        }
    }

    // 格式化栏目标题
    private static string FormatColumnTitle(string columnKey)
    {
        // 直接返回原始键名
        return columnKey;
    }

    #endregion 
} 