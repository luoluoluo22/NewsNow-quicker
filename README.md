# NewsNow-Quicker

## 项目简介

NewsNow-Quicker 是一个基于 Quicker 自定义窗口实现的新闻聚合阅读工具，可以聚合显示来自多个来源的最新资讯，包括 V2EX、微博热搜、IT之家等。

![预览图](preview.png)

## 功能特点

- **多源聚合**：同时展示多个来源的最新资讯
- **简洁界面**：采用现代化设计风格，简洁美观
- **一键跳转**：点击新闻条目直接跳转到原文阅读
- **实时刷新**：一键刷新获取最新资讯

## 文件说明

- `NewsNow_Quicker.xaml`：自定义窗口的XAML界面设计文件
- `NewsNow_Code.cs`：C#代码文件，实现新闻数据获取和处理逻辑
- `NewsNow_DataMapping.txt`：数据映射配置文件
- `newsnow_log.txt`：调试日志文件

## 使用方法

### 安装步骤

1. 确保已安装 [Quicker](https://getquicker.net/) 工具
2. 在 Quicker 中点击"新建动作"
3. 选择"自定义窗口"类型
4. 将以下三个文件的内容分别复制到对应的标签页中：
   - `NewsNow_Quicker.xaml` → XAML设计页面
   - `NewsNow_DataMapping.txt` → 数据映射页面
   - `NewsNow_Code.cs` → 辅助C#代码页面

### 使用说明

- 启动自定义窗口后，会自动加载各个来源的最新资讯
- 点击各条新闻可在浏览器中打开原始链接
- 点击顶部的"刷新"按钮可重新获取最新资讯

## 技术实现

- **前端界面**：使用 WPF/XAML 构建用户界面
- **后端逻辑**：使用 C# 语言实现数据获取和处理
- **数据绑定**：使用 Quicker 的数据绑定机制实现界面更新

## 开发计划

- [ ] 添加更多新闻源
- [ ] 支持自定义主题颜色
- [ ] 添加新闻搜索功能
- [ ] 支持新闻收藏与历史记录
- [ ] 添加夜间模式切换

## 贡献指南

欢迎对本项目提出改进建议或提交代码贡献。请遵循以下步骤：

1. Fork 本仓库
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m '添加某某功能'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 许可证

本项目采用 MIT 许可证 - 详情请参阅 [LICENSE](LICENSE) 文件 