# n8n JavaScript开发规范

## 1. 环境限制与注意事项

### 1.1 模块和API限制

- **不可使用Node.js核心模块**：如`https`、`http`、`fs`等模块在n8n代码节点中不可用
- **不可使用第三方npm包**：无法直接导入或使用外部npm包
- **无浏览器API**：如`fetch`、`XMLHttpRequest`等浏览器API不可用
- **无`$http`对象**：某些n8n版本中不提供`$http`工具

### 1.2 语法限制

- **异步语法支持有限**：避免使用复杂的`async/await`结构
- **ES6+兼容性**：部分ES6+特性可能不受支持，保持代码风格简单
- **函数定义**：优先使用函数声明而非箭头函数

## 2. 最佳实践

### 2.1 数据获取

- **优先使用n8n的HTTP Request节点**：用于获取外部数据
- **使用预定义数据**：在无法进行HTTP请求时，使用预定义数据集
- **分离数据获取与处理**：使用独立节点获取数据，然后在Code节点中处理

### 2.2 错误处理

- **始终使用try/catch块**：捕获可能出现的错误
- **提供有意义的错误信息**：清晰说明错误原因
- **返回默认数据**：在失败时返回默认值，而不是中断流程

### 2.3 代码组织

- **简化逻辑**：保持代码简单明了
- **避免过多嵌套**：减少回调嵌套层级
- **模块化处理**：将复杂逻辑分解为小型可管理的函数

## 3. 代码示例

### 3.1 基本Code节点模板

```javascript
// 简单处理数据的Code节点
function processData() {
  try {
    // 获取输入项
    const items = $input.all();
    
    // 处理数据
    const processedItems = items.map(item => {
      // 数据处理逻辑
      return {
        ...item.json,
        processed: true,
        timestamp: new Date().toISOString()
      };
    });
    
    // 返回结果
    return processedItems;
    
  } catch (error) {
    // 错误处理
    console.log('处理数据时出错:', error.message);
    return [{error: error.message}];
  }
}

// 返回处理结果
return processData();
```

### 3.2 预定义数据示例 (网络请求替代方案)

```javascript
function provideData() {
  // 当前时间
  const currentTime = new Date().toTimeString().slice(0, 5);
  
  // 预定义数据
  return {
    "数据源": [
      {
        "Title": "预定义数据项1",
        "Time": currentTime,
        "Url": "https://example.com/1"
      },
      {
        "Title": "预定义数据项2",
        "Time": currentTime,
        "Url": "https://example.com/2"
      }
    ]
  };
}

return provideData();
```

## 4. 常见问题与解决方案

### 4.1 HTTP请求问题

**问题**: 无法使用标准HTTP客户端库  
**解决方案**: 
- 使用单独的HTTP Request节点获取数据
- 使用预定义数据替代实时数据
- 将需要的数据预先保存为JSON文件，通过Read Binary File节点读取

### 4.2 模块导入错误

**问题**: 尝试导入模块时出现错误  
**解决方案**:
- 避免使用`require`或`import`语句
- 使用原生JavaScript实现所需功能
- 分解复杂流程为多个节点

### 4.3 异步处理问题

**问题**: 复杂异步操作无法正常工作  
**解决方案**:
- 简化异步逻辑
- 使用Promise而非async/await
- 将异步流程分解为多个节点

## 5. 调试技巧

### 5.1 日志记录

- 使用`console.log()`输出调试信息
- 添加详细注释说明代码逻辑
- 使用n8n的执行日志追踪问题

### 5.2 逐步测试

- 每次只改动一小部分代码
- 频繁执行工作流程测试变更
- 使用简单的测试数据验证流程

## 6. 性能优化

- 减少代码复杂度
- 避免处理大型数据集
- 将计算密集型任务分解到多个节点

## 7. 安全注意事项

- 不在代码中硬编码敏感信息
- 使用n8n的凭据管理功能
- 验证和清理输入数据

---

本文档总结了在n8n环境中使用JavaScript的主要注意事项和最佳实践。由于n8n版本更新可能带来变化，建议定期查阅官方文档以获取最新信息。 