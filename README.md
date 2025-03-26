# NewsNow-quicker 新闻数据获取工具

这是一个使用C#开发的新闻数据获取工具，专为Quicker软件设计。工具可以从多个来源获取最新的新闻信息，并以JSON格式保存到Quicker变量中。

## 项目文件说明

本项目主要包含以下文件：

- **NewsGathererForQuicker.cs** - Quicker环境兼容版新闻获取脚本
  - 使用Quicker环境内置的Newtonsoft.Json库处理JSON序列化
  - 通过网络请求获取真实的最新新闻数据
  - 支持从V2EX、微博热搜、IT之家、知乎热榜、GitHub热门和小红书推荐获取数据
  - 生成符合JSON格式的数据结构
  - 实现智能缓存机制，加快加载速度

## 如何在Quicker中使用

1. 打开Quicker并创建一个新的动作
2. 添加一个"运行C#代码"步骤
3. 将NewsGathererForQuicker.cs的内容粘贴到代码编辑框中
4. 运行动作，新闻数据将被保存到Quicker变量`newsData`中

## 数据格式

生成的数据遵循以下JSON格式：

```json
{
  "栏目名称": [
    {
      "Title": "新闻标题",
      "Time": "时间",
      "Url": "链接地址"
    },
    // 更多新闻条目...
  ],
  // 更多栏目...
}
```

## 数据来源

脚本会从以下来源获取最新数据，每个来源约9条信息：

- V2EX热门话题 -> "V2EX热门"栏目
- 微博热搜榜 -> "微博热搜"栏目
- IT之家最新资讯 -> "IT之家"栏目
- 知乎热榜 -> "知乎热榜"栏目
- GitHub趋势项目 -> "GitHub热门"栏目
- 小红书推荐笔记 -> "小红书推荐"栏目

## 缓存机制

为了提高用户体验和降低加载时间，脚本实现了智能缓存机制：

- 首次运行时，脚本会创建缓存并保存到`文档\QuickerCache\newsData.json`
- 后续运行时，脚本会立即加载最近的缓存数据，同时在后台更新新数据
- 缓存会在后台自动更新，无需等待就能看到内容
- 缓存有效期为12小时，超过时间会自动刷新
- 即使网络不稳定也能显示最近的新闻数据

## 注意事项

- 脚本需要网络连接才能获取最新数据，但离线时会使用缓存
- 所有缓存文件保存在用户文档目录下的`QuickerCache`文件夹中
- 根据需要可以修改保存路径和新闻来源配置
- 脚本使用Newtonsoft.Json库进行序列化，确保在Quicker环境中可用
- 获取失败时会添加备用信息作为条目，保证内容完整性

## 自定义

如需自定义新闻来源或修改数据格式，可以编辑脚本中的相关部分：

- 修改网络请求方法以适应不同的数据源
- 调整 `newsDictionary` 的键名和数据内容
- 更改 `CacheFilePath` 变量以修改缓存位置 