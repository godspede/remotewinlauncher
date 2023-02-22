using Microsoft.Win32;

namespace remotewinlauncher
{
    public class Program
    {
        static IConfiguration? configuration;
        public static IConfiguration Configuration { get => configuration ?? throw new Exception(); set => configuration = value; }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            Configuration = app.Configuration;

            app.MapGet("/", async () =>
            {
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (s, e) =>
                {
                    cts.Cancel();
                    e.Cancel = true;
                };

                return await Do(cts.Token);
            });


            //Consoler.ShowConsole();
            Console.WriteLine($"Launching on {Configuration["Urls"]}");

            app.Run();
        }

        static async Task<string> Do(CancellationToken ct)
        {
            var regkey = Configuration["RegKey"];
            var key = Registry.CurrentUser.OpenSubKey(regkey);
            var secret = key?.GetValue("Secret")?.ToString();
            if (String.IsNullOrEmpty(secret))
            {
                Consoler.ShowConsole();
                Console.WriteLine("Unconfigured!");
                //TODO: collect secret and save to registry
                return "Configuration hasn't been initialized!";
            }
            return "ok";
        }


    }
}