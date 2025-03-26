# 贡献指南

感谢您对NewsNow-Quicker项目的关注！我们欢迎各种形式的贡献，包括但不限于：

- 报告问题或提出建议
- 改进文档
- 修复Bug
- 添加新功能
- 优化界面设计
- 提供新闻源接入支持

## 如何贡献代码

1. **Fork本仓库**：点击GitHub页面右上角的Fork按钮，创建一个自己的仓库副本。

2. **克隆仓库**：
   ```bash
   git clone https://github.com/您的用户名/NewsNow-quicker.git
   cd NewsNow-quicker
   ```

3. **创建分支**：
   ```bash
   git checkout -b feature/您的特性名称
   ```

4. **修改代码**：根据您的想法修改代码，确保遵循项目的代码风格和约定。

5. **测试更改**：在Quicker环境中测试您的更改，确保功能正常。

6. **提交更改**：
   ```bash
   git add .
   git commit -m "描述您所做的更改"
   git push origin feature/您的特性名称
   ```

7. **创建Pull Request**：在GitHub上提交一个Pull Request，详细描述您所做的更改。

## 代码风格指南

- 使用清晰的命名约定，变量和函数名应当具有描述性
- 为主要功能添加注释
- XAML文件中确保不要添加事件处理程序，而是在C#代码中注册事件
- 遵循WPF的MVVM模式，尽量保持代码整洁和可维护

## 报告问题

如果您发现了问题但不确定如何修复，请在GitHub的Issues页面提交一个详细的问题报告，包括：

1. 问题的详细描述
2. 重现问题的步骤
3. 预期结果和实际结果
4. 您的运行环境（操作系统、Quicker版本等）
5. 可能的解决方案（如果有）

## 开发环境配置

1. 安装[Quicker](https://getquicker.net/)工具
2. 熟悉Quicker的自定义窗口开发规范
3. 了解WPF/XAML和C#基础知识

## 提交Pull Request前的检查清单

- [ ] 代码可以正常运行并经过测试
- [ ] 保持代码风格一致
- [ ] 更新了相关文档（如需要）
- [ ] 添加了必要的注释

感谢您的贡献！ 