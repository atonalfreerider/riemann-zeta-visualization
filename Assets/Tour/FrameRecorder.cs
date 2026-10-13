using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

/// <summary>
/// Deterministic portrait (1080×1920) recorder. Renders the camera into an offscreen texture at a fixed
/// capture framerate and streams raw frames to ffmpeg (H.264 mp4). If ffmpeg is not on PATH it falls back
/// to a PNG sequence. Narration audio is muxed afterwards with Tools/mux_recording.py using timeline.json.
/// </summary>
public class FrameRecorder : MonoBehaviour
{
    public int Width = 1080;
    public int Height = 1920;
    public int Fps = 30;
    public int Crf = 18;

    public bool IsRecording { get; private set; }
    public int FrameIndex { get; private set; }
    public string OutputDirectory { get; private set; }

    Camera cam;
    RenderTexture target;
    Process ffmpeg;
    Stream ffmpegInput;
    Thread writerThread;
    BlockingCollection<byte[]> frames;
    bool usePng;
    Coroutine captureLoop;
    int previousCaptureFramerate;
    int previousVSync;

    public static FrameRecorder Create(Camera cam)
    {
        FrameRecorder recorder = cam.gameObject.AddComponent<FrameRecorder>();
        recorder.cam = cam;
        return recorder;
    }

    /// <summary>True while the camera renders into the offscreen portrait texture (recording or storyboard).</summary>
    public bool IsOffscreen { get; private set; }

    /// <summary>Render offscreen at a fixed frame rate without streaming frames (used for storyboard snapshots).</summary>
    public void BeginOffscreen(string directory)
    {
        if (IsOffscreen) return;
        OutputDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(OutputDirectory);
        FrameIndex = 0;

        target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { name = "RecorderTarget", antiAliasing = 4 };
        target.Create();
        cam.targetTexture = target;

        previousCaptureFramerate = Time.captureFramerate;
        previousVSync = QualitySettings.vSyncCount;
        Time.captureFramerate = Fps;
        QualitySettings.vSyncCount = 0;
        IsOffscreen = true;
        captureLoop = StartCoroutine(CaptureLoop());
    }

    public void EndOffscreen()
    {
        if (!IsOffscreen) return;
        IsOffscreen = false;
        if (captureLoop != null) StopCoroutine(captureLoop);
        AsyncGPUReadback.WaitAllRequests();
        cam.targetTexture = null;
        target.Release();
        Destroy(target);
        target = null;
        Time.captureFramerate = previousCaptureFramerate;
        QualitySettings.vSyncCount = previousVSync;
    }

    /// <summary>Save the next rendered frame as a PNG in the output directory (offscreen mode only).</summary>
    public void Snapshot(string fileName)
    {
        if (!IsOffscreen || target == null) return;
        string path = Path.Combine(OutputDirectory, fileName);
        int w = Width, h = Height;
        AsyncGPUReadback.Request(target, 0, TextureFormat.RGBA32, request =>
        {
            if (request.hasError) return;
            byte[] data = request.GetData<byte>().ToArray();
            bool flip = NeedsFlip;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                byte[] flipped = flip ? FlipVertically(data, w, h, 4) : data;
                File.WriteAllBytes(path, ImageConversion.EncodeArrayToPNG(flipped, GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h));
            });
        });
    }

    public void StartRecording(string directory)
    {
        if (IsRecording) return;
        BeginOffscreen(directory);

        frames = new BlockingCollection<byte[]>(boundedCapacity: 60);
        needsFlip = NeedsFlip;
        usePng = !TryStartFfmpeg();
        writerThread = new Thread(WriterLoop) { IsBackground = true, Name = "FrameRecorderWriter" };
        writerThread.Start();

        IsRecording = true;
        Debug.Log($"Recording {Width}x{Height}@{Fps} to {OutputDirectory} ({(usePng ? "PNG sequence" : "ffmpeg h264")})");
    }

    public void StopRecording(List<(string id, float time, int frame)> audioTimeline = null)
    {
        if (!IsRecording) return;
        IsRecording = false;

        AsyncGPUReadback.WaitAllRequests();
        frames.CompleteAdding();
        writerThread.Join();

        if (ffmpegInput != null)
        {
            ffmpegInput.Flush();
            ffmpegInput.Close();
            ffmpeg.WaitForExit();
            ffmpeg.Dispose();
            ffmpeg = null;
            ffmpegInput = null;
        }

        EndOffscreen();
        WriteTimeline(audioTimeline);
        Debug.Log($"Recording stopped after {FrameIndex} frames → {OutputDirectory}");
    }

    void WriteTimeline(List<(string id, float time, int frame)> audioTimeline)
    {
        StringBuilder sb = new();
        sb.Append("{\n  \"width\": ").Append(Width).Append(", \"height\": ").Append(Height)
          .Append(", \"fps\": ").Append(Fps).Append(", \"frames\": ").Append(FrameIndex)
          .Append(", \"video\": \"").Append(usePng ? "frames/frame_%06d.png" : "video.mp4").Append("\",\n  \"audio\": [\n");
        if (audioTimeline != null)
        {
            for (int i = 0; i < audioTimeline.Count; i++)
            {
                (string id, float time, int frame) = audioTimeline[i];
                sb.Append("    {\"id\": \"").Append(id).Append("\", \"time\": ")
                  .Append(time.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(", \"frame\": ").Append(frame).Append('}');
                if (i < audioTimeline.Count - 1) sb.Append(',');
                sb.Append('\n');
            }
        }

        sb.Append("  ]\n}\n");
        File.WriteAllText(Path.Combine(OutputDirectory, "timeline.json"), sb.ToString());
    }

    bool TryStartFfmpeg()
    {
        try
        {
            string output = Path.Combine(OutputDirectory, "video.mp4");
            ProcessStartInfo info = new()
            {
                FileName = "ffmpeg",
                Arguments = $"-y -loglevel error -f rawvideo -pix_fmt rgba -s {Width}x{Height} -r {Fps} -i - " +
                            (NeedsFlip ? "-vf vflip " : "") +
                            $"-c:v libx264 -preset fast -crf {Crf} -pix_fmt yuv420p -movflags +faststart \"{output}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            ffmpeg = Process.Start(info);
            if (ffmpeg == null) return false;
            ffmpegInput = ffmpeg.StandardInput.BaseStream;
            ffmpeg.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.LogWarning("ffmpeg: " + e.Data); };
            ffmpeg.BeginErrorReadLine();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ffmpeg not available ({e.Message}); writing PNG frames instead.");
            ffmpeg = null;
            ffmpegInput = null;
            Directory.CreateDirectory(Path.Combine(OutputDirectory, "frames"));
            return false;
        }
    }

    IEnumerator CaptureLoop()
    {
        WaitForEndOfFrame endOfFrame = new();
        while (IsOffscreen)
        {
            yield return endOfFrame;
            if (target == null) yield break;

            if (Application.isBatchMode)
            {
                // no game view in batch mode: render explicitly so the texture is up to date
                cam.Render();
            }
            else
            {
                // show the portrait frame on screen as well (the camera is rendering offscreen)
                Graphics.Blit(target, (RenderTexture)null);
            }

            int index = FrameIndex++;
            if (IsRecording)
            {
                AsyncGPUReadback.Request(target, 0, TextureFormat.RGBA32, request => OnFrame(request, index));
            }
        }
    }

    void OnFrame(AsyncGPUReadbackRequest request, int index)
    {
        if (request.hasError)
        {
            Debug.LogWarning($"Frame {index}: GPU readback failed");
            return;
        }

        if (frames == null || frames.IsAddingCompleted) return;
        byte[] data = request.GetData<byte>().ToArray();
        try
        {
            frames.Add(data);
        }
        catch (InvalidOperationException)
        {
            // recording stopped while a readback was in flight
        }
    }

    void WriterLoop()
    {
        int written = 0;
        foreach (byte[] frame in frames.GetConsumingEnumerable())
        {
            if (usePng)
            {
                byte[] flipped = needsFlip ? FlipVertically(frame, Width, Height, 4) : frame;
                byte[] png = ImageConversion.EncodeArrayToPNG(flipped, GraphicsFormat.R8G8B8A8_UNorm, (uint)Width, (uint)Height);
                File.WriteAllBytes(Path.Combine(OutputDirectory, "frames", $"frame_{written:D6}.png"), png);
            }
            else
            {
                ffmpegInput.Write(frame, 0, frame.Length);
            }

            written++;
        }
    }

    /// <summary>
    /// Whether readback rows must be flipped before encoding. Verified on Linux/OpenGL (no flip needed);
    /// APIs with top-left UV origin render the camera target upside down relative to that.
    /// Override with the environment variable ZETA_FLIP=0/1 if a platform disagrees.
    /// </summary>
    static bool NeedsFlip
    {
        get
        {
            string env = Environment.GetEnvironmentVariable("ZETA_FLIP");
            if (env == "0") return false;
            if (env == "1") return true;
            return SystemInfo.graphicsUVStartsAtTop;
        }
    }
    bool needsFlip;

    static byte[] FlipVertically(byte[] src, int width, int height, int bpp)
    {
        byte[] dst = new byte[src.Length];
        int stride = width * bpp;
        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(src, y * stride, dst, (height - 1 - y) * stride, stride);
        }

        return dst;
    }

    void OnDestroy()
    {
        if (IsRecording) StopRecording();
        else if (IsOffscreen) EndOffscreen();
    }
}
