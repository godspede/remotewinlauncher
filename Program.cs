using Microsoft.Win32;

namespace remotewinlauncher
{
    public class Program
    {
        static IConfiguration? configuration;
        static IConfiguration Configuration { get => configuration ?? throw new Exception(); set => configuration = value; }
        static string RegKey => Configuration["RegKey"];

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            Configuration = app.Configuration;

            app.MapGet("/", HandleIt);
            app.MapGet("/{secret}", HandleIt);

            Console.WriteLine($"Launching on {Configuration["Urls"]}");
            ConsoleExtension.Hide();

            if (String.IsNullOrEmpty(Secret))
			{
                await PromptSecret();
			}

            await app.RunAsync();
        }
        static string? Secret
        {
            get => Registry.CurrentUser.OpenSubKey(RegKey)?.GetValue("Secret")?.ToString();
            set
            {
                var key = Registry.CurrentUser.OpenSubKey(RegKey);
                if (key == null)
				{
                    key = Registry.LocalMachine.CreateSubKey(RegKey);
                    key.SetValue("Secret", value?.ToString() ?? String.Empty);
				}
            }
        }

        static async Task PromptSecret()
		{
            var key = Registry.CurrentUser.OpenSubKey(RegKey);
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
            var key = Registry.CurrentUser.OpenSubKey(RegKey);
            var regsecret = key?.GetValue("Secret")?.ToString();
            if (String.IsNullOrEmpty(regsecret))
            {
                ConsoleExtension.Show();
                Console.WriteLine($"Unconfigured!");
                //TODO: collect secret and save to registry
                return "Configuration hasn't been initialized! Use the console application window to do so.";
            }
            return "ok";
        }


    }
}