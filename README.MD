# [bgm.tv](https://bgm.tv) 为Emby获取元数据

## [下载地址](https://github.com/fuxin052/Emby.Plugins.Bangumi/raw/main/Emby.Plugins.Bangumi/bin/Release/net6.0/Emby.Plugins.Bangumi.dll) 

### dll文件下载完成后移动到 `config/plugins` 文件夹内

目前支持电影和电视剧

电视剧目录结构
```
├─棺姬嘉依卡                    # 文件夹名是用来识别
│      S01E01.mp4              # 视频文件名必须包含S01Exx， 例如 example S01E01.mp4也是可以的，最好不要包含其他数字，防止集号识别错误
│      S01E02.mp4
│      S01E03.mp4
       ......
│      S01E12.mp4

```

如果有些条目获取不到，请去配置页面填写bangumi的token，r18条目（官方称为nsfw）的数据需要token才能获取到


代码大部分逻辑都是参照[https://github.com/kookxiang/jellyfin-plugin-bangumi](https://github.com/kookxiang/jellyfin-plugin-bangumi)的代码，感谢@kookxiang
