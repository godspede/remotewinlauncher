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

            app.MapGet("/", HandleIt);
            app.MapGet("/{secret}", HandleIt);

            Console.WriteLine($"Launching on {Configuration["Urls"]}");
            ConsoleExtension.Hide();

            app.Run();
        }

        static async Task<string> HandleIt(string? secret = null)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                cts.Cancel();
                e.Cancel = true;
            };
            return await Do(cts.Token, secret);
        }

        static async Task<string> Do(CancellationToken ct, string? clientsecret)
        {
            var regkey = Configuration["RegKey"];
            var key = Registry.CurrentUser.OpenSubKey(regkey);
            var regsecret = key?.GetValue("Secret")?.ToString();
            if (String.IsNullOrEmpty(regsecret))
            {
                ConsoleExtension.Show();
                Console.WriteLine($"Unconfigured!");
                //TODO: collect secret and save to registry
                return "Configuration hasn't been initialized!";
            }
            return "ok";
        }


    }
}