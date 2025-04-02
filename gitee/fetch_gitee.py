import requests
import json
import datetime
import time
import random
from typing import List, Dict, Any

def fetch_gitcode_news(max_retries=3) -> List[Dict[str, Any]]:
    """获取GitCode行业动态数据并解析为标准新闻格式"""
    url = "https://web-api.gitcode.com/api/v1/agg/index"
    
    params = {
        "page": 1,
        "per_page": 10, 
        "total": 0,
        "channel_id": "67bc3f5f97a0293d6bfebd01",
        "sub_channel_id": "",
        "m_code": "dynamics",
        "d_code": "industry_news",
        "c_id": "67bc3f5f97a0293d6bfebd01"
    }
    
    headers = {
        "accept": "application/json, text/plain, */*",
        "accept-language": "zh-CN,zh;q=0.9,en;q=0.8",
        "origin": "https://gitcode.com",
        "referer": "https://gitcode.com/",
        "user-agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36",
        "x-app-channel": "gitcode-fe",
        "x-platform": "web"
    }
    
    retry_count = 0
    while retry_count < max_retries:
        try:
            # 创建不验证SSL的会话，并禁用代理
            session = requests.Session()
            session.trust_env = False  # 禁用环境变量中的代理设置
            session.verify = False  # 不验证SSL证书（仅用于测试环境）
            
            response = session.get(
                url,
                params=params,
                headers=headers,
                timeout=10,
                proxies={"http": None, "https": None}  # 显式禁用代理
            )
            
            response.raise_for_status()
            data = response.json()
            
            # 直接解析接口返回的结构
            news_list = []
            if "content" in data:
                for item in data['content']:
                    # 从当前时间获取小时和分钟作为临时时间
                    now = datetime.datetime.now()
                    formatted_time = now.strftime("%H:%M")
                    
                    # 尝试从industry_news中获取标题和时间
                    title = None
                    url = None
                    publish_time = None
                    
                    # 优先使用行业动态的标题和时间
                    if "industry_news" in item and isinstance(item["industry_news"], dict):
                        industry_news = item["industry_news"]
                        if "title" in industry_news and industry_news["title"]:
                            title = industry_news["title"]
                        if "publish_time" in industry_news and industry_news["publish_time"]:
                            try:
                                # 解析时间
                                dt = datetime.datetime.fromisoformat(industry_news["publish_time"].replace("Z", "+00:00"))
                                # 转换为本地时间
                                local_dt = dt.astimezone()
                                formatted_time = local_dt.strftime("%H:%M")
                            except:
                                pass  # 如果解析失败，使用当前时间
                    
                    # 如果industry_news中没有标题，尝试从project_info中获取
                    if not title and "project_info" in item and isinstance(item["project_info"], dict):
                        project_info = item["project_info"]
                        if "name" in project_info and project_info["name"]:
                            title = project_info["name"]
                            # 如果有描述，添加到标题中
                            if "description" in project_info and project_info["description"]:
                                desc = project_info["description"]
                                if len(desc) > 50:
                                    desc = desc[:47] + "..."
                                title = f"{title}: {desc}"
                    
                    # 获取URL
                    if "project_info" in item and isinstance(item["project_info"], dict):
                        project_info = item["project_info"]
                        if "web_url" in project_info and project_info["web_url"]:
                            url = project_info["web_url"]
                    
                    # 构造标准新闻数据格式
                    if title and url:
                        # 截断长标题
                        if len(title) > 100:
                            title = title[:97] + "..."
                        
                        news_item = {
                            "title": title,
                            "time": formatted_time,
                            "url": url
                        }
                        
                        news_list.append(news_item)
                    
            return news_list
        
        except requests.exceptions.RequestException as e:
            print(f"获取GitCode数据时出错: {str(e)}")
            retry_count += 1
            if retry_count < max_retries:
                wait_time = 2 ** retry_count  # 指数退避
                print(f"等待 {wait_time} 秒后重试...")
                time.sleep(wait_time)
            else:
                print("达到最大重试次数，使用模拟数据")
                return get_mock_data()
        except json.JSONDecodeError as e:
            print(f"解析JSON数据时出错: {str(e)}")
            return get_mock_data()
    
    return []

def get_mock_data() -> List[Dict[str, Any]]:
    """生成模拟的GitCode数据用于测试"""
    mock_data = []
    project_names = [
        "youtube-music", "Ventoy", "nvm", "dockge", "nest-admin",
        "highway", "cvat", "NSMusicS", "OpenEmu", "ffmpeg-commander"
    ]
    
    descriptions = [
        "个性化音乐播放器，畅享自由！", "一键打造多系统启动U盘，高效安全！", 
        "Node.js版本管理工具", "Docker Compose管理工具",
        "基于NestJS的后台管理系统", "高性能SIMD库", 
        "图像标注与数据管理工具", "在线音乐播放与管理",
        "复古游戏模拟器", "FFmpeg命令生成工具"
    ]
    
    # 使用当前时间生成不同的时间字符串
    now = datetime.datetime.now()
    
    for i in range(len(project_names)):
        # 生成随机的分钟差，让时间不同
        minutes_ago = random.randint(0, 120)
        time_str = (now - datetime.timedelta(minutes=minutes_ago)).strftime("%H:%M")
        
        mock_data.append({
            "title": f"🔥{project_names[i]}：{descriptions[i]}全网新增星标！",
            "time": time_str,
            "url": f"https://gitcode.com/gh_mirrors/{project_names[i].lower()}/{project_names[i].lower()}"
        })
    
    return mock_data

def save_to_json(news_list: List[Dict[str, Any]]) -> None:
    """将新闻数据保存为JSON文件"""
    try:
        # 构造包含多个来源的数据格式
        data = {"GitCode": news_list}
        
        with open("gitcode_news.json", "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
    except Exception as e:
        print(f"保存JSON文件时出错: {str(e)}")

if __name__ == "__main__":
    news_list = fetch_gitcode_news()
    if news_list:
        save_to_json(news_list)
        print(f"获取到 {len(news_list)} 条GitCode新闻")
        for news in news_list:
            print(f"{news['time']} - {news['title']}")
    else:
        print("未获取到任何新闻数据")
