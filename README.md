# VideoPlayerDemo

[简体中文](README_zh-CN.md)

## Features

 - Software decoding which provides high precision time synchronisation events
 - High-precision time leaping
 - No separate hWnd needs to be created to play the video, thus the video does not mask the WPF form and supports UI element composition
 - Support for transparent channels
 - Support for multi-threaded graphics rendering

## Quick start

### Introduction

There are two main classes under the namespace ``VideoPlayer``
 - Class ``VideoViewer`` is a UI control for rendering video frames
 - Class ``VideoController`` is used to control the playback of videos

### Usage

1. Set the Build Action for two ``ffms2.dll``s to None, and set them Copy to Output Directory

   - ffms2/x86/ffms2.dll
   - ffms2/x64/ffms2.dll

2. Initialising the environment after the app started

```C#
// Determining the instruction set
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
        throw new Exception("Unknow CPU instruction set");
}

// Configuring ffms environment variables
var ffms2Dir = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffms2");
Environment.SetEnvironmentVariable("PATH", Path.Combine(ffms2Dir, instructionSet) + ";" + Environment.GetEnvironmentVariable("PATH"));
```

3. Referencing namespaces in the ``Window`` tag within xaml

```xml
 xmlns:videoplayer="clr-namespace:VideoPlayer"
```

4. Declaring the control in xaml

```xml
<videoplayer:VideoViewer x:Name="Viewer"/>
```

5. (Optional) Set the number of graphics rendering threads

```C#
Viewer.RenderThreadNumber = 2;
```

6. Create a video controller

```C#
VideoController videoController = new VideoController(this.Viewer);
```

7. Set the playback time processing method, here is an example of loop play

```C#
videoController.TimeUpdatedHander = (double time) =>
{
    // If playback reaches the end, set the playback time to 0
    if (time >= videoController.VideoSource.LastTime)
    {
        videoController.PlaybackTime = 0;
    }
};
```

8. Load a video file

```C#
string videoPath = "test.mp4";

videoController.Init(videoPath,
   (object sender1, double e1) =>
   {
        // Load progress updated handling functions
        Console.WriteLine($"Loading: {e1 * 100:0.00} %");
   },
   (object sender1, VideoController.LoadingState e1) =>
   {
        // Loading status updated handling method
        switch (e1)
        {
            case VideoController.LoadingState.IndexingFrames:
                Console.WriteLine("Building index of video frames...");
                break;
        }
   });
```

9. Play/Pause the video

```C#
videoController.Play();
videoController.Pause();
```

10. Close the video

```C#
videoController.Close();
```

## TODO

 - [ ] Open custom graphics shading interface

### Reference

 - [ffms2](https://github.com/ffms/ffms2)
 - [FFMSSharp](https://github.com/nixxquality/FFMSSharp)

