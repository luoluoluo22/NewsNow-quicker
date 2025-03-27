// n8n专用代码节点
// 从GitHub Trending页面提取热门项目

function executeNode() {
  try {
    // 从items获取HTML内容
    const html = $input.first().json.data;
    if (!html) {
      throw new Error('未找到HTML内容');
    }

    const results = [];
    
    // 提取仓库信息
    // 匹配整个仓库卡片
    const repoRegex = /<article class="Box-row">[\s\S]*?<\/article>/g;
    const repos = html.match(repoRegex) || [];

    for (const repo of repos) {
      try {
        // 提取仓库名和链接
        const titleRegex = /<h2 class="h3 lh-condensed">[\s\S]*?<a[^>]*href="([^"]*)"[^>]*>([\s\S]*?)<\/a>/;
        const titleMatch = repo.match(titleRegex);
        
        if (titleMatch) {
          const url = `https://github.com${titleMatch[1]}`;
          
          // 提取描述
          const descRegex = /<p class="col-9 color-fg-muted my-1 pr-4">([\s\S]*?)<\/p>/;
          const descMatch = repo.match(descRegex);
          const description = descMatch ? descMatch[1].trim() : '';
          
          // 组合标题
          const title = `${titleMatch[2].trim().replace(/\s+/g, ' ')}: ${description}`;
          
          const currentTime = new Date().toTimeString().slice(0, 5);
          
          results.push({
            "Title": title,
            "Time": currentTime,
            "Url": url
          });
        }
      } catch (repoError) {
        console.error('处理仓库数据时出错:', repoError);
      }
    }

    // 返回结果
    return {
      "GitHub趋势": results
    };
    
  } catch (error) {
    // 处理错误
    const currentTime = new Date().toTimeString().slice(0, 5);
    return {
      "GitHub趋势": [
        {
          "Title": `获取GitHub数据失败: ${error.message}`,
          "Time": currentTime,
          "Url": "https://github.com/trending"
        }
      ]
    };
  }
}

// 返回执行结果
return executeNode(); 