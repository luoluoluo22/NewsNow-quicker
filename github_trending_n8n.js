// n8n专用代码节点
// 由于n8n环境限制，使用预定义数据代替HTTP请求

// 创建异步执行函数
function executeNode() {
  try {
    // 当前时间，用于所有项目
    const currentTime = new Date().toTimeString().slice(0, 5);
    
    // 预定义的热门GitHub项目列表
    // 这些数据将定期更新
    const projects = [
      {
        "Title": "khoj-ai/khoj: 你的AI第二大脑。可自托管。从网络或文档获取答案。构建自定义代理，安排自动化，进行深入研究。",
        "Time": currentTime,
        "Url": "https://github.com/khoj-ai/khoj"
      },
      {
        "Title": "kubernetes/ingress-nginx: Kubernetes的Ingress NGINX控制器",
        "Time": currentTime,
        "Url": "https://github.com/kubernetes/ingress-nginx"
      },
      {
        "Title": "alibaba/spring-ai-alibaba: Java开发者的代理AI框架",
        "Time": currentTime,
        "Url": "https://github.com/alibaba/spring-ai-alibaba"
      },
      {
        "Title": "ourongxing/newsnow: 雅致阅读实时和最热新闻",
        "Time": currentTime,
        "Url": "https://github.com/ourongxing/newsnow"
      },
      {
        "Title": "joanrod/star-vector: 用于SVG生成的基础模型，将矢量化转变为代码生成任务",
        "Time": currentTime,
        "Url": "https://github.com/joanrod/star-vector"
      },
      {
        "Title": "deepseek-ai/DeepSeek-V3: 最新AI模型",
        "Time": currentTime,
        "Url": "https://github.com/deepseek-ai/DeepSeek-V3"
      },
      {
        "Title": "shadps4-emu/shadPS4: 用C++编写的Windows、Linux和macOS的PlayStation 4模拟器",
        "Time": currentTime,
        "Url": "https://github.com/shadps4-emu/shadPS4"
      },
      {
        "Title": "Akkudoktor-EOS/EOS: 能源优化系统",
        "Time": currentTime,
        "Url": "https://github.com/Akkudoktor-EOS/EOS"
      },
      {
        "Title": "browser-use/browser-use: 使网站对AI代理可访问",
        "Time": currentTime,
        "Url": "https://github.com/browser-use/browser-use"
      },
      {
        "Title": "NirDiamant/GenAI_Agents: 生成式AI代理技术教程和实现",
        "Time": currentTime,
        "Url": "https://github.com/NirDiamant/GenAI_Agents"
      },
      {
        "Title": "bregman-arie/devops-exercises: Linux、Jenkins、AWS、SRE、Prometheus、Docker等DevOps面试题",
        "Time": currentTime,
        "Url": "https://github.com/bregman-arie/devops-exercises"
      },
      {
        "Title": "signalapp/Signal-Android: 一个私密的Android消息应用",
        "Time": currentTime,
        "Url": "https://github.com/signalapp/Signal-Android"
      },
      {
        "Title": "juspay/hyperswitch: 用Rust编写的开源支付交换机，使支付快速、可靠和实惠",
        "Time": currentTime,
        "Url": "https://github.com/juspay/hyperswitch"
      }
    ];
    
    // 返回结果
    return {
      "GitHub趋势": projects
    };
    
  } catch (error) {
    // 处理任何错误
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

// 在n8n中使用时，直接返回函数执行结果
return executeNode(); 