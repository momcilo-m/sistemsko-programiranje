using OpenMeteo.Models;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;

namespace OpenMeteo.Services
{
    public class OpenMeteoService : IDisposable
    {
        private readonly HttpListener httpListener;
        private readonly HttpClient httpClient;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public OpenMeteoService()
        {
            httpListener = new HttpListener();
            httpClient = new HttpClient();
        }
        
        public void Start()
        {
            httpListener.Prefixes.Add("http://localhost:5050/");
            httpListener.Start();
            Console.WriteLine("Server pokrenut na http://localhost:5050/");

            //Asinhroni Task koji ceka zahteve
            var requestObservable = Observable.Create<HttpListenerContext>(observer =>
            {
                var cancellationDisposable = new CancellationDisposable();
                var ct = cancellationDisposable.Token;

                Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            var context = await httpListener.GetContextAsync();
                            observer.OnNext(context);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    }
                }, ct);

                return cancellationDisposable;
            });

            //Zahtev se obradjuje preko TaskPoolSchedulera zbog MultiThreading-a
            var processingSubscription = requestObservable
                .ObserveOn(TaskPoolScheduler.Default)
                .Select(context => Observable.FromAsync(() => HandleRequest(context)))
                .Merge()
                .Subscribe(
                    context => Console.WriteLine($"Zahtev obradjen"),
                    ex => Console.WriteLine($"Greška u obradi zahteva: {ex.Message}"),
                    () => Console.WriteLine($"Server zaustavljen")
                );
            _disposables.Add(processingSubscription);
        }

        public async Task<HttpListenerContext> HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

                
            Console.WriteLine(context.Request);
            string? latitude = context.Request.QueryString["latitude"];
            string? longitude = context.Request.QueryString["longitude"];
            string? start = context.Request.QueryString["start_date"];
            string? end = context.Request.QueryString["end_date"];

            if (latitude == null || longitude == null || start == null || end == null)
            {
                response.StatusCode = 400;
                var buffer = Encoding.UTF8.GetBytes("Missing query parameters (latitude, longitude, start_date, end_date)");
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
                return context;
            }

            try
            {
                var aqForeccast = await AirQualityForecast(latitude,longitude,start,end);

                await SendResponse(response, aqForeccast);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Greška: {ex.Message}");
                response.StatusCode = 500;
                await SendTextResponse(response, "Internal Server Error: " + ex.Message);
            }

            return context;
        }

        private async Task<AirQuality> AirQualityForecast(string latitude, string longitude,string start, string end)
        {

            var url =$"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={latitude}&longitude={longitude}&hourly=pm10,pm2_5,carbon_monoxide,nitrogen_dioxide&start_date={start}&end_date={end}";

            httpClient.DefaultRequestHeaders.Add("User-Agent", "ReactiveNewsServer/1.0");

            var json = await httpClient.GetStringAsync(url);
            var apiResponse = JsonSerializer.Deserialize<AirQuality>(json);

            if (apiResponse == null)
                throw new Exception("Failed to parse Air Quality API response.");
            

            if (apiResponse == null)
                throw new Exception("Failed to parse Air Quality API response.");

            return apiResponse!;
        }
        public void Stop()
        {
            if(httpListener.IsListening)
            {
                httpListener.Stop();
                httpListener.Close();
                Console.WriteLine($"Server zaustavljen");
            }
        }

        private static async Task SendResponse(HttpListenerResponse response, object data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            var buffer = Encoding.UTF8.GetBytes(json);

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private static async Task SendTextResponse(HttpListenerResponse response, string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        public void Dispose()
        {
            Stop();
            _disposables.Dispose();
            httpClient.Dispose();
        }
    }
}
