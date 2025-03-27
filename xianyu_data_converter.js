// n8n专用代码节点
// 将闲鱼数据转换为新闻数据格式

function executeNode() {
  try {
    // 从上游节点获取闲鱼数据
    const inputData = $input.first().json;
    if (!inputData) {
      throw new Error('未找到输入数据');
    }

    // 清理JSON字符串中的中文标点和非标准格式
    function cleanJsonString(jsonStr) {
      if (typeof jsonStr !== 'string') {
        return jsonStr;
      }
      
      // 替换中文标点和不规则格式
      return jsonStr
        .replace(/"([^"]+)"：/g, '"$1":') // 将"key"： 替换为 "key":
        .replace(/：/g, ':')             // 将其他：替换为:
        .replace(/，/g, ',')             // 将，替换为,
        .replace(/、/g, ',')             // 将、替换为,
        .replace(/\s+\]/g, ']')          // 处理 ] 前的空格
        .replace(/\s+\}/g, '}')          // 处理 } 前的空格
        .replace(/\]\s*，/g, '],')       // 处理 ],
        .replace(/\}\s*，/g, '},')       // 处理 },
        .replace(/"\s*\[/g, '"[')        // 处理 "[
        .replace(/"\s*\]/g, '"]')        // 处理 "]
        .replace(/"\s*,/g, '",')         // 处理 ",
        .replace(/"\s*}/g, '"}');        // 处理 "}
    }

    // 处理数据
    let cleanData;
    
    // 如果输入是字符串，尝试解析
    if (typeof inputData === 'string') {
      const cleanedStr = cleanJsonString(inputData);
      try {
        cleanData = JSON.parse(cleanedStr);
      } catch (e) {
        throw new Error(`JSON解析失败: ${e.message}`);
      }
    } else {
      // 如果已经是对象，直接使用
      cleanData = inputData;
    }

    // 转换为新闻数据格式
    const results = [];
    
    // 提取闲鱼商品列表
    const items = Array.isArray(cleanData) ? cleanData : 
                 (cleanData.data ? cleanData.data : []);
    
    // 调试日志
    console.log(`找到 ${items.length} 条商品信息`);
    
    for (const item of items) {
      // 不再过滤价格为0的商品
      // 处理标题过长问题
      let title = item.title || '无标题';
      if (title.length > 100) {
        title = title.substring(0, 97) + '...';
      }
      
      const currentTime = new Date().toTimeString().slice(0, 5);
      
      results.push({
        "Title": title,
        "Time": currentTime,
        "Url": item.detail_url || `https://www.goofish.com/item?id=${item.item_id}`
      });
    }

    // 调试日志
    console.log(`转换后共 ${results.length} 条数据`);

    // 返回符合新闻数据格式的结果
    return {
      "闲鱼二手": results
    };
    
  } catch (error) {
    // 处理错误
    const currentTime = new Date().toTimeString().slice(0, 5);
    return {
      "闲鱼二手": [
        {
          "Title": `数据转换失败: ${error.message}`,
          "Time": currentTime,
          "Url": "https://www.goofish.com"
        }
      ]
    };
  }
}

// 返回执行结果
return executeNode(); 