# 小红书数据采集 n8n 配置说明

## 工作流程配置

### 1. 打开n8n在HTTP 节点配置(需要打开小红书登录后获取cookie)
- Method: GET
- URL: https://www.xiaohongshu.com/explore
- Headers:
```bash
curl -X GET "https://www.xiaohongshu.com/explore" \
-H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36" \
-H "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" \
-H "Accept-Language: zh-CN,zh;q=0.9,en;q=0.8" \
-H "Accept-Encoding: gzip, deflate, br" \
-H "Connection: keep-alive" \
-H "Cache-Control: no-cache" \
-H "Pragma: no-cache" \
-H "Sec-Ch-Ua: \"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Google Chrome\";v=\"122\"" \
-H "Sec-Ch-Ua-Mobile: ?0" \
-H "Sec-Ch-Ua-Platform: \"Windows\"" \
-H "Sec-Fetch-Dest: document" \
-H "Sec-Fetch-Mode: navigate" \
-H "Sec-Fetch-Site: none" \
-H "Sec-Fetch-User: ?1" \
-H "Upgrade-Insecure-Requests: 1" \
-H "Cookie: xhsTrxxx5e5f"
```

### 2. Code 节点配置
- 语言: JavaScript
- 代码: 使用 `xiaohongshu_n8n.js` 中的代码

## 注意事项

1. Cookie 获取与更新
   - 在浏览器中打开小红书网页版
   - 按 F12 打开开发者工具
   - 在 Network 标签页中找到对 explore 的请求
   - 复制请求中的完整 Cookie
   - 更新 HTTP Request 节点中的 Cookie 值

2. 输出数据格式
```json
{
  "小红书": [
    {
      "Title": "文章标题",
      "Time": "当前时间",
      "Url": "文章链接"
    }
  ]
}
```

3. 可能遇到的问题
   - 如果请求返回 401，说明 Cookie 已过期，需要更新
   - 如果数据为空，检查网页源码格式是否变化
   - 建议设置每 10-15 分钟执行一次

4. 建议配置
   - 将 Cookie 设置为 n8n 凭证，方便统一管理
   - 添加错误通知
   - 配置数据重复检查 