// n8n专用代码节点

/**
 * 分析内容类型
 * @param {string} title - 标题文本
 * @returns {string} 内容类型
 */
function analyzeContentType(title) {
  const types = {
    'AI': ['ai', 'gpt', '文心', 'claude', 'deepseek', '智能', '模型', '生成'],
    '数码': ['手机', '电脑', 'app', '软件', '小米', '华为', '苹果', 'ios', 'android'],
    '汽车': ['汽车', '智驾', '电动', '新能源', '特斯拉', '比亚迪'],
    '科技': ['科技', '技术', '编程', '开发', '程序', '代码'],
    '互联网': ['互联网', '社交', '电商', '平台', '网络'],
    '创业': ['创业', '商业', '融资', '投资', '市场'],
    '职场': ['工作', '求职', '面试', '简历', '职场'],
    '其他': []
  };

  for (const [type, keywords] of Object.entries(types)) {
    if (keywords.some(keyword => title.toLowerCase().includes(keyword))) {
      return type;
    }
  }
  return '其他';
}

// 创建异步执行函数
function executeNode() {
  try {
    // 从items获取HTML内容
    const html = $input.first().json.data;
    if (!html) {
      throw new Error('未找到HTML内容');
    }

    const results = [];
    
    // 使用更精确的正则表达式匹配note-card结构
    const noteCardRegex = /<section class="note-item"[\s\S]*?<\/section>/g;
    const noteCards = html.match(noteCardRegex) || [];

    for (const card of noteCards) {
      try {
        // 提取标题
        const titleRegex = /<span[^>]*data-v-[^>]*>(?!<svg)(.*?)<\/span>/;
        const titleMatch = card.match(titleRegex);
        const title = titleMatch ? titleMatch[1].trim() : '';

        // 提取链接
        const urlRegex = /href="(\/explore\/[^"?]*)/;
        const urlMatch = card.match(urlRegex);
        const url = urlMatch ? `https://www.xiaohongshu.com${urlMatch[1]}` : '';

        if (title && url && !title.includes('<svg')) {
          // 分析内容类型
          const type = analyzeContentType(title);
          const currentTime = new Date().toTimeString().slice(0, 5);
          
          results.push({
            "Title": title,
            "Time": currentTime,
            "Url": url
          });
        }
      } catch (cardError) {
        console.error('处理卡片时出错:', cardError);
      }
    }

    // 返回结果
    return {
      "小红书": results
    };
    
  } catch (error) {
    // 处理任何错误
    const currentTime = new Date().toTimeString().slice(0, 5);
    return {
      "小红书": [
        {
          "Title": `获取小红书数据失败: ${error.message}`,
          "Time": currentTime,
          "Url": "https://www.xiaohongshu.com"
        }
      ]
    };
  }
}

// 在n8n中使用时，直接返回函数执行结果
return executeNode(); 