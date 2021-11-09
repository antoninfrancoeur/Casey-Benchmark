using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Debug = UnityEngine.Debug;

using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;


public class Benchmarker : MonoBehaviour {

    [Header("References")]
    public TextMeshProUGUI cpuConsoleOutput;
    public TextMeshProUGUI cpuInfo;
    public TextMeshProUGUI gpuConsoleOutput;
    public TextMeshProUGUI gpuInfo;

    [Header("Data")]
    public Texture2D image;
    public ComputeShader computeShader;
    public RenderTexture rt;


    private StringBuilder sbCPU = new StringBuilder();
    private StringBuilder sbGPU = new StringBuilder();
    private object flagLock = new object();
    private float[] flags;
    Stopwatch multiThreadSW = new Stopwatch();


    private void Start () {
        cpuInfo.SetText(SystemInfo.processorType);
        gpuInfo.SetText(SystemInfo.graphicsDeviceName);
    }

    private void OnDestroy () {
        rt.Release();
    }

    private void Update () {

        // Check to see of all flags are done.
        if(multiThreadSW.IsRunning) {
            lock(flagLock) {
                bool allThreadDone = true;
                foreach(float flag in flags) {
                    allThreadDone = allThreadDone && (flag > 0);
                }

                if(allThreadDone) {
                    EndCPUMultithread();
                }
            }
        }
    }



    public void RunCPUSingleCore () {
        StartCoroutine(Coroutine_RunCPUSingleCore());
    }

    IEnumerator Coroutine_RunCPUSingleCore () {
        PrintLineCPU("Starting CPU single core analysis...", Color.yellow);
        Stopwatch sw = new Stopwatch();
        sw.Start();
        double lastTime = 0;
        yield return new WaitForEndOfFrame();


        // Basic math
        PrintLineCPU("Running x20'000'000 math function calls");
        yield return new WaitForEndOfFrame();
        float check = 0;
        for(int i = 0; i < 20000000; i++) {
            check = Mathf.Sqrt(Mathf.Sin(check + Mathf.PerlinNoise(i, 0.5f * i)));
        }

        PrintLineCPU($"Elapsed: {sw.Elapsed.TotalMilliseconds - lastTime} ms");
        lastTime = sw.Elapsed.TotalMilliseconds;
        yield return new WaitForEndOfFrame();


        // Giz garbage
        PrintLineCPU("Zipping garbage data x3'000");
        yield return new WaitForEndOfFrame();
        string data = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus nisi dui, ullamcorper id sem elementum, suscipit pellentesque dolor. Sed suscipit vulputate justo, ut blandit mauris euismod eget. Duis bibendum velit in posuere aliquet. Cras in augue lobortis, malesuada erat nec, hendrerit velit. Aenean et augue enim. Sed turpis lacus, vulputate in augue at, pellentesque aliquam justo. Praesent eget varius lacus. Suspendisse ut diam tellus. Praesent posuere elementum ligula. Duis condimentum dignissim egestas. Vivamus ut nulla quis metus tristique iaculis in et nibh. Fusce fringilla ex sit amet nibh tempus, ac pellentesque massa dignissim. Fusce sed consectetur lectus, vel tristique diam. Nam ac ligula mauris.";
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        for(int i = 0; i < 3000; i++) {
            using(var msi = new MemoryStream(bytes))
            using(var mso = new MemoryStream()) {
                using(var gs = new GZipStream(mso, CompressionMode.Compress)) {
                    msi.CopyTo(gs);
                }

                bytes = mso.ToArray();
            }
        }

        PrintLineCPU($"Elapsed: {sw.Elapsed.TotalMilliseconds - lastTime} ms");
        lastTime = sw.Elapsed.TotalMilliseconds;
        yield return new WaitForEndOfFrame();


        // Png encoding
        PrintLineCPU("Encoding casey li to png x1'000");
        yield return new WaitForEndOfFrame();
        for(int i = 0; i < 1000; i++) {
            byte[] imageBytes = ImageConversion.EncodeToPNG(image);
            ImageConversion.LoadImage(new Texture2D(2, 2), imageBytes);
        }

        PrintLineCPU($"Elapsed: {sw.Elapsed.TotalMilliseconds - lastTime} ms");
        lastTime = sw.Elapsed.TotalMilliseconds;
        yield return new WaitForEndOfFrame();


        sw.Stop();
        PrintLineCPU($"Total time: {sw.Elapsed.TotalMilliseconds} ms", Color.yellow);
    }



    public void RunCPUMultithread () {
        if(multiThreadSW.IsRunning)
            return;

        PrintLineCPU("Starting CPU multithread analysis...", Color.yellow);

        multiThreadSW.Start();

        PrintLineCPU("Starting 200 threads.");
        PrintLineCPU("Running 2'000'000 math calls per thread.");
        flags = new float[200];
        for(int i = 0; i < flags.Length; i++) {
            int index = i;
            ThreadPool.QueueUserWorkItem((s) => {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                float check = 0;
                for(int i = 0; i < 2000000; i++) {
                    check = Mathf.Sqrt(Mathf.Sin(check + Mathf.PerlinNoise(i, 0.5f * i)));
                }
                sw.Stop();

                lock(flagLock) {
                    flags[index] = (float)sw.Elapsed.TotalMilliseconds;
                }
            });
        }

        
    }

    public void EndCPUMultithread () {

        for(int i = 0; i < flags.Length; i++) {
            PrintLineCPU($"Ran thread {i} in {flags[i]} ms", Color.magenta);
        }

        multiThreadSW.Stop();
        PrintLineCPU($"Total time: {multiThreadSW.Elapsed.TotalMilliseconds} ms", Color.yellow);
        multiThreadSW.Reset();
    }



    public void RunComputeShader () {
        StartCoroutine(Coroutine_RunComputeShader());
    }

    IEnumerator Coroutine_RunComputeShader () {
        PrintLineGPU("Starting GPU compute analysis...", Color.yellow);
        Stopwatch sw = new Stopwatch();
        sw.Start();
        PrintLineGPU("Rendering 1024x1024 image x1200");
        rt = new RenderTexture(1024, 1024, 0);
        rt.enableRandomWrite = true;
        rt.Create();
        for(int i = 0; i < 1200; i++) {
            computeShader.SetFloat("iTime", Time.time);
            computeShader.SetFloats("iResolution", rt.width, rt.height);
            computeShader.SetTexture(0, "Result", rt);
            computeShader.Dispatch(0, rt.width / 8, rt.height / 8, 1);
            yield return new WaitForEndOfFrame();
        }

        sw.Stop();
        PrintLineGPU($"Total time: {sw.Elapsed.TotalMilliseconds} ms", Color.yellow);
    }



    public void PrintLineCPU (string text) => PrintLineCPU(text, Color.white);
    public void PrintLineCPU (string text, Color color) {
        sbCPU.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>");
        cpuConsoleOutput.SetText(sbCPU);
    }

    public void PrintLineGPU (string text) => PrintLineGPU(text, Color.white);
    public void PrintLineGPU (string text, Color color) {
        sbGPU.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>");
        gpuConsoleOutput.SetText(sbGPU);
    }
}
