# 介绍
Quartz任务调用系统，支持持久化存储和远程原理

# 功能介绍
- [x] 持久化存储
- [x] 支持URL job
- [x] 支持通过dll文件增加job
- [ ] 加强job的添加修改验证
- [ ] 支持部署到服务器的服务中

# 部署问题

## 发布到Windows服务器

* 安装.NET Core SDK 
下载地址：<https://www.microsoft.com/net/download/windows>安装完成后需要重启服务器才能生效
* 发布的时候需要在csproj中添加  

```
<PublishWithAspNetCoreTargetManifest>false</PublishWithAspNetCoreTargetManifest>
```

