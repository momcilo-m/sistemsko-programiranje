using ImgToGif.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Img2Gif
{

    class Program
    {
        private static readonly string RootFolder = Directory.GetCurrentDirectory()+"\\images";
        static readonly Color[] colors = [Color.Coral, Color.White, Color.Chocolate, Color.Crimson, Color.DarkCyan, Color.DimGrey];
        static readonly int delay = 10;

        private static readonly ConcurrentDictionary<string, CacheItem> cache = new();
        private static readonly TimeSpan ValidTime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CacheHitTime = TimeSpan.FromMinutes(1);

        static void Main(string[] args) {

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5050/");
            listener.Start();

            Console.WriteLine("Server pokrenut na http://localhost:5050/");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                stopWatch.Stop();
                Console.WriteLine($"Funkcija je trajala: {stopWatch.Elapsed.TotalNanoseconds} ns");
            }
        }

        static void HandleRequest(HttpListenerContext context)
        {

            string? imageName = context.Request.RawUrl?.TrimStart('/');

            if(string.IsNullOrEmpty(imageName))
            {
                Error(403, "Please insert image name",context);
                Console.WriteLine("Error: Please insert image name");
                return;
            }

            string fullPath = Path.Combine(RootFolder, imageName);

            if (!File.Exists(fullPath))
            {
                Error(404, "Image not found", context);
                Console.WriteLine("Error: Image not found");
                return;
            }


            //Da li slika ima u kesu
            if (cache.TryGetValue(imageName, out var img))
            {
                if (img.ExpiredTime >= DateTime.UtcNow)
                {
                    lock (img)
                    {
                        img.ExpiredTime += CacheHitTime;
                    }
                    
                    Console.WriteLine("Success: Image was successfully founded in cache");
                    ResponseWithImage(200, "image/gif", img.Image, context);
                    return;
                }
                //Isteklo
                else
                {
                    cache.TryRemove(imageName, out img);
                    Console.WriteLine("Success: Image was successfully founded in cache, but time has expired");
                }
            };
            

            try
            {
                using Image<Rgba32> original = Image.Load<Rgba32>(fullPath);
                Image<Rgba32> gif;

                bool busy = IsThreadPoolBusy();
               
                gif = MultiThreadGif(original);

                gif.Frames.RemoveFrame(0);

                cache.TryAdd(imageName, new CacheItem(gif, DateTime.UtcNow + ValidTime));

                Console.WriteLine("Success: Image was successfully created");
                ResponseWithImage(200, "image/gif", gif, context);

            }
            catch(Exception ex) 
            {
                Console.WriteLine("Internal Server Error");
                Error(500, "Internal Server Error" + ex.Message, context);
            }
        }
        static Image<Rgba32> MultiThreadGif(Image<Rgba32> original)
        {
            Image<Rgba32> gif = CreateGif(original);

            var frames = new Image<Rgba32>[colors.Length]; ;
            Parallel.ForEach(Enumerable.Range(0, colors.Length), i =>
            {

                Image<Rgba32> layer = original.Clone();

                layer.Mutate(img =>
                {
                    img.Fill(colors[i].WithAlpha(0.3f));
                });


                var metadata = layer.Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = delay;

                frames[i] = layer;
            });

            foreach (var frame in frames)
            {
                gif.Frames.AddFrame(frame.Frames.RootFrame);
            }

            return gif;
        }

        static void Error(int code, string message,HttpListenerContext context)
        {
            context.Response.StatusCode = code;
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            context.Response.ContentType = "text/plain";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        static void ResponseWithImage(int code, string type, Image<Rgba32> image, HttpListenerContext context)
        {
            context.Response.StatusCode = code;
            context.Response.ContentType = type;
            image.SaveAsGif(context.Response.OutputStream);
            context.Response.OutputStream.Close();
        }

        static Image<Rgba32> CreateGif(Image<Rgba32> original)
        {
            var gif = new Image<Rgba32>(original.Width, original.Height);
            var gifMetaData = gif.Metadata.GetGifMetadata();
            gifMetaData.RepeatCount = 0;

            return gif;
        }

        static bool IsThreadPoolBusy()
        {
            ThreadPool.GetAvailableThreads(out int availableThreads, out _);
            ThreadPool.GetMaxThreads(out int maxThreads, out _);

            double loadFactor = 1.0 - (double)availableThreads / maxThreads;
            return loadFactor > 0.8;
        }
    } 
}