# VideoPlayer

[English](README.md)

## 特性

 - 软件解码，提供高精度时间同步事件
 - 高精度时间跳转
 - 无需创建独立句柄以播放视频，因此视频不会遮盖WPF窗体，支持UI元素组合
 - 支持透明通道
 - 支持多线程图像渲染

## 快速开始

### 介绍

``VideoPlayer`` 命名空间下包含两个主要类
 - ``VideoViewer`` 类用于渲染视频画面的UI控件
 - ``VideoController`` 类用于控制视频的播放

### 使用方法

1. 设置两个 ffms2.dll 的构建处理 为 无, 并设置复制到输出目录

   - ffms2/x86/ffms2.dll
   - ffms2/x64/ffms2.dll

2. 应用程序启动后初始化环境

```C#
// 判断指令集
string instructionSet;
switch (IntPtr.Size)
{
    case 4:
        instructionSet = "x86";
        break;
    case 8:
        instructionSet = "x64";
        break;
    default:
        throw new Exception("未知 CPU 指令集");
}

// 配置 ffms 环境变量
var ffms2Dir = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffms2");
Environment.SetEnvironmentVariable("PATH", Path.Combine(ffms2Dir, instructionSet) + ";" + Environment.GetEnvironmentVariable("PATH"));
```

3. 在 xaml 的 ``Window`` 标签中引用命名空间

```xml
xmlns:videoplayer="clr-namespace:VideoPlayer;assembly=VideoPlayer"
```

4. 在 xaml 中声明控件

```xml
<videoplayer:VideoViewer x:Name="Viewer"/>
```

5. (可选)设置图像渲染线程数

```C#
Viewer.RenderThreadNumber = 2;
```

6. 创建视频控制器

```C#
VideoController videoController = new VideoController(this.Viewer);
```

7. 设定回放时间处理方法，这里以循环播放为例

```C#
videoController.TimeUpdatedHander = (double time) =>
{
    // 若播放到结尾，则将回放时间设置为 0
    if (time >= videoController.VideoSource.LastTime)
    {
        videoController.PlaybackTime = 0;
    }
};
```

8. 加载视频文件

```C#
string videoPath = "test.mp4";

videoController.Init(videoPath,
   (object sender1, double e1) =>
   {
        // 加载进度更新处理方法
        Console.WriteLine($"载入中: {e1 * 100:0.00} %");
   },
   (object sender1, VideoController.LoadingState e1) =>
   {
        // 加载状态更新处理方法
        switch (e1)
        {
            case VideoController.LoadingState.IndexingFrames:
                Console.WriteLine("正在建立视频帧索引...");
                break;
        }
   });
```

9. 播放/暂停视频

```C#
videoController.Play();
videoController.Pause();
```

10. 关闭视频

```C#
videoController.Close();
```

## TODO

 - [ ] 开放自定义图像着色接口

### 参考

 - [ffms2](https://github.com/ffms/ffms2)
 - [FFMSSharp](https://github.com/nixxquality/FFMSSharp)

