using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
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
    private static readonly string LogPath = @"F:\vscodeNotes\03-project\NewsNow-quicker\newsnow_log.txt";
    
    // 辅助方法：查找视觉树中的子元素
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child != null && child is T)
            {
                yield return (T)child;
            }

            foreach (var grandChild in FindVisualChildren<T>(child))
            {
                yield return grandChild;
            }
        }
    }

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
        // 初始化订阅关键词输入框
        if (dataContext.ContainsKey("query"))
        {
            var queryTextBox = win.FindName("QueryTextBox") as TextBox;
            if (queryTextBox != null)
            {
                queryTextBox.Text = dataContext["query"].ToString();
                
                // 添加文本变更事件处理
                queryTextBox.TextChanged += (sender, e) =>
                {
                    dataContext["query"] = queryTextBox.Text;
                };
            }
        }
        
        // 记录加载后的数据
        LogDataContextInfo(dataContext, "OnWindowLoaded");
        
        // 设置栏目标题
        UpdateColumnTitles(win, dataContext);
        
        // 尝试直接设置控件的ItemsSource
        TrySetItemsDirectly(win, dataContext);

        // 使用调度器延迟执行点击事件绑定
        win.Dispatcher.BeginInvoke(new Action(() =>
        {
            // 为新闻项添加点击事件处理
            AttachClickHandlers(win, winContext, dataContext);
            
            // 记录事件绑定完成
            LogDataContextInfo(dataContext, "ClickHandlersAttached");
        }), DispatcherPriority.Loaded);
    }

    public static bool OnButtonClicked(string controlName, object controlTag, Window win, IDictionary<string, object> dataContext, ICustomWindowContext winContext)
    {
        // 处理订阅按钮点击事件
        if (controlName == "SubscribeButton")
        {
            // 获取输入框中的关键词
            var queryTextBox = win.FindName("QueryTextBox") as TextBox;
            if (queryTextBox != null && !string.IsNullOrWhiteSpace(queryTextBox.Text))
            {
                // 更新数据上下文中的订阅关键词
                dataContext["query"] = queryTextBox.Text.Trim();
                

                
                // 标记事件已处理
                return true;
            }
        }
        
        // 只记录按钮点击事件，不包含刷新按钮的特殊处理
        if (controlName != "RefreshButton") {
            LogDataContextInfo(dataContext, "OnButtonClicked_" + controlName);
        }
        return false;
    }

    #region 辅助方法

    // 记录数据上下文信息到日志文件
    private static void LogDataContextInfo(IDictionary<string, object> dataContext, string eventName)
    {
        // 简化的日志记录，移除详细的数据信息记录
        try
        {
            string logMessage = "事件: " + eventName + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n";
            
            // 创建目录（如果不存在）
            string directory = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 追加到日志文件
            File.AppendAllText(LogPath, logMessage);
        }
        catch
        {
            // 忽略日志记录错误
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
            if (dataContext.ContainsKey("newsData") && dataContext["newsData"] is Dictionary<string, object>)
            {
                var newsData = dataContext["newsData"] as Dictionary<string, object>;

                // 先隐藏所有栏目
                for (int i = 1; i <= 9; i++)
                {
                    var columnGrid = win.FindName("Column" + i + "Grid") as Grid;
                    if (columnGrid != null)
                    {
                        columnGrid.Visibility = Visibility.Collapsed;
                    }
                }

                // 动态处理每个栏目
                var columnKeys = newsData.Keys.ToList();
                for (int i = 0; i < columnKeys.Count && i < 9; i++)
                {
                    string columnKey = columnKeys[i];
                    // 获取对应的Grid和ItemsControl
                    var columnGrid = win.FindName("Column" + (i + 1) + "Grid") as Grid;
                    var itemsControl = win.FindName("column" + (i + 1) + "TitleItemsControl") as ItemsControl;
                    
                    if (itemsControl != null && columnGrid != null && newsData.ContainsKey(columnKey))
                    {
                        var columnData = newsData[columnKey];
                        if (columnData is System.Collections.IEnumerable)
                        {
                            // 显示栏目
                            columnGrid.Visibility = Visibility.Visible;
                            
                            // 设置数据源
                            itemsControl.ItemsSource = (System.Collections.IEnumerable)columnData;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("newsData不存在或不是Dictionary类型", "数据绑定错误");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("设置数据源时出错: " + ex.Message, "NewsNow", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 为所有新闻列表添加点击事件处理
    private static void AttachClickHandlers(Window win, ICustomWindowContext winContext, IDictionary<string, object> dataContext)
    {
        // 获取预览相关控件
        var previewPopup = win.FindName("PreviewPopup") as Popup;
        var previewTitleText = win.FindName("PreviewTitleText") as TextBlock;
        var previewLoadingText = win.FindName("PreviewLoadingText") as TextBlock;
        var previewOpenButton = win.FindName("PreviewOpenButton") as Button;
        
        // 记录预览控件状态
        WriteLog(string.Format("预览控件状态: PreviewPopup={0}, PreviewTitleText={1}, PreviewLoadingText={2}, PreviewOpenButton={3}",
            previewPopup != null,
            previewTitleText != null,
            previewLoadingText != null,
            previewOpenButton != null));

        if (previewPopup != null)
        {
            // 设置预览窗口的位置和样式
            previewPopup.Placement = PlacementMode.Mouse;
            previewPopup.AllowsTransparency = true;
            previewPopup.StaysOpen = false;
            WriteLog("已设置预览窗口属性");
        }
        
        // 延迟计时器
        var hoverTimer = new DispatcherTimer();
        hoverTimer.Interval = TimeSpan.FromMilliseconds(500);
        
        string currentUrl = null;
        string currentTitle = null;
        Border currentBorder = null;
        
        hoverTimer.Tick += (s, args) =>
        {
            hoverTimer.Stop();
            if (currentBorder != null && currentUrl != null && previewPopup != null)
            {
                // 显示标题和提示
                if (previewTitleText != null)
                    previewTitleText.Text = currentTitle ?? "无标题";
                if (previewLoadingText != null)
                    previewLoadingText.Text = "URL: " + currentUrl;
                
                // 显示预览窗口
                try
                {
                    previewPopup.IsOpen = true;
                    WriteLog("显示预览窗口: " + currentTitle);
                }
                catch (Exception ex)
                {
                    WriteLog("显示预览窗口失败: " + ex.Message);
                }
            }
            else
            {
                WriteLog(string.Format("预览窗口显示失败: currentBorder={0}, currentUrl={1}, previewPopup={2}",
                    currentBorder != null,
                    currentUrl != null,
                    previewPopup != null));
            }
        };

        // 为每个栏目的ItemsControl添加事件处理
        for (int i = 1; i <= 9; i++)
        {
            int columnIndex = i;
            string itemsControlName = "column" + columnIndex + "TitleItemsControl";
            var itemsControl = win.FindName(itemsControlName) as ItemsControl;
            
            if (itemsControl != null)
            {
                WriteLog("找到ItemsControl: " + itemsControlName);
                
                // 等待ItemsControl加载完成
                itemsControl.Loaded += (s, e) =>
                {
                    // 查找所有的NewsItemBorder
                    var borders = FindVisualChildren<Border>(itemsControl)
                        .Where(b => b.Name == "NewsItemBorder")
                        .ToList();

                    WriteLog(string.Format("找到 {0} 个新闻项 Border，ItemsControl: {1}", 
                        borders.Count, itemsControlName));

                    if (borders.Count == 0)
                    {
                        // 如果没有找到指定名称的Border，尝试查找所有Border
                        borders = FindVisualChildren<Border>(itemsControl).ToList();
                        WriteLog(string.Format("尝试查找所有Border，找到 {0} 个，ItemsControl: {1}", 
                            borders.Count, itemsControlName));
                    }

                    foreach (var border in borders)
                    {
                        // 确保Border有Tag属性
                        if (border.Tag == null && border.Child is Grid grid)
                        {
                            var titleBlock = FindVisualChildren<TextBlock>(grid).FirstOrDefault(t => t.Text != null && !t.Text.StartsWith("20"));
                            if (titleBlock != null)
                            {
                                WriteLog("找到标题: " + titleBlock.Text);
                            }
                        }

                        // 鼠标进入事件
                        border.MouseEnter += (sender, args) =>
                        {
                            WriteLog("鼠标进入Border");
                            var hoveredBorder = sender as Border;
                            if (hoveredBorder != null && hoveredBorder.Tag != null)
                            {
                                currentUrl = hoveredBorder.Tag.ToString();
                                WriteLog("获取到URL: " + currentUrl);
                                
                                // 获取标题
                                var grid = hoveredBorder.Child as Grid;
                                if (grid != null)
                                {
                                    var titleBlock = FindVisualChildren<TextBlock>(grid).FirstOrDefault(t => t.Text != null && !t.Text.StartsWith("20"));
                                    if (titleBlock != null)
                                    {
                                        currentTitle = titleBlock.Text;
                                        WriteLog("鼠标进入新闻项: " + currentTitle);
                                    }
                                }
                                
                                currentBorder = hoveredBorder;
                                hoverTimer.Start();
                                WriteLog("启动预览计时器");
                            }
                            else
                            {
                                WriteLog("Border或Tag为空");
                            }
                        };
                        
                        // 鼠标离开事件
                        border.MouseLeave += (sender, args) =>
                        {
                            WriteLog("鼠标离开Border");
                            hoverTimer.Stop();
                            if (previewPopup != null && previewPopup.IsOpen)
                            {
                                previewPopup.IsOpen = false;
                                WriteLog("关闭预览窗口");
                            }
                            currentUrl = null;
                            currentTitle = null;
                            currentBorder = null;
                        };

                        // 点击事件
                        border.MouseLeftButtonDown += (sender, args) =>
                        {
                            var clickedBorder = sender as Border;
                            if (clickedBorder != null && clickedBorder.Tag != null)
                            {
                                string url = clickedBorder.Tag.ToString();
                                try
                                {
                                    System.Diagnostics.Process.Start(url);
                                    WriteLog("点击新闻项: " + url);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("打开链接失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                    WriteLog("打开链接失败: " + ex.Message);
                                }
                            }
                        };
                    }
                };
            }
            else
            {
                WriteLog("未找到ItemsControl: " + itemsControlName);
            }
        }
    }

    // 简化的日志写入方法
    private static void WriteLog(string message)
    {
        try
        {
            string logMessage = "事件: " + message + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n";
            
            // 创建目录（如果不存在）
            string directory = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 追加到日志文件
            File.AppendAllText(LogPath, logMessage);
        }
        catch
        {
            // 忽略日志记录错误
        }
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
            for (int i = 1; i <= 9; i++)
            {
                var titleBlock = win.FindName("Column" + i + "Title") as TextBlock;
                if (titleBlock != null && i <= columnKeys.Count)
                {
                    string columnKey = columnKeys[i - 1];
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