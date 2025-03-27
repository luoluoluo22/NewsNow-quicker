# GitHub趋势数据采集 n8n 配置说明

## 工作流程配置

### 1. HTTP Request 节点配置
- Method: GET
- URL: https://github.com/trending
- Headers:
```json
{
  "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
  "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
  "Accept-Language": "zh-CN,zh;q=0.9,en;q=0.8",
  "Cache-Control": "no-cache",
  "Pragma": "no-cache",
  "Sec-Ch-Ua": "\"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Google Chrome\";v=\"122\"",
  "Sec-Ch-Ua-Mobile": "?0",
  "Sec-Ch-Ua-Platform": "\"Windows\"",
  "Sec-Fetch-Dest": "document",
  "Sec-Fetch-Mode": "navigate",
  "Sec-Fetch-Site": "none",
  "Sec-Fetch-User": "?1",
  "Upgrade-Insecure-Requests": "1"
}
```

### 2. Code 节点配置
- 语言: JavaScript
- 代码: 使用 `github_trending_n8n.js` 中的代码

## 输出数据格式
```json
{
  "GitHub趋势": [
    {
      "Title": "仓库名: 仓库描述",
      "Time": "当前时间",
      "Url": "仓库链接"
    }
  ]
}
```

## 注意事项

1. 数据更新
   - GitHub趋势页面每天更新
   - 建议设置为每小时采集一次
   - 可以通过URL参数指定语言和时间范围

2. 可能遇到的问题
   - 如果返回429，说明请求过于频繁
   - 如果数据为空，检查网页源码格式是否变化
   - 某些仓库可能没有描述，此时Title只包含仓库名

3. 建议配置
   - 添加请求频率限制
   - 配置错误通知
   - 添加数据重复检查
   - 可选：添加代理以避免限制

4. URL参数选项
   - 语言: ?spoken_language_code=zh
   - 时间: ?since=daily|weekly|monthly
   - 示例: https://github.com/trending?spoken_language_code=zh&since=daily 