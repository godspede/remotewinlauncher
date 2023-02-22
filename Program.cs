using CommandLine;
using Microsoft.Win32;

namespace remotewinlauncher
{
	public class Options
	{
		[Option('s', "secret", Required = true, HelpText = "The auth secret used at <url>/<secret> to kick off the program.")]
		public string Secret { get; set; }
		[Option('p', "path", Required = true, HelpText = "The path of the program to launch.")]
		public string Path { get; set; }
		[Option('h', "hide", Required = false, HelpText = "If provided, hides the window on start.", Default = false)]
		public bool HideOnStart { get; set; }
	}
	public class Program
	{
		static IConfiguration? configuration;
		static IConfiguration Configuration { get => configuration ?? throw new Exception(); set => configuration = value; }

		static string Secret { get; set; }
		static string Path { get; set; }
		static bool HideOnStart { get; set; }

		public static async Task Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed<Options>(o =>
				{
					Secret = o.Secret;
					Path = o.Path;
					HideOnStart = o.HideOnStart;
				})
				.WithNotParsed<Options>((e) =>
				{
					ConsoleExtension.Show();
					Console.WriteLine("Missing arguments! Press any key to terminate.");
					Console.ReadLine();
					Environment.Exit(0);
				});

			var builder = WebApplication.CreateBuilder();
			var app = builder.Build();
			Configuration = app.Configuration;


			Console.WriteLine($"Launching on {Configuration["Urls"]}{Environment.NewLine}");
			string sampleUrl = Configuration["Urls"]?.Split(';')?[0] ?? String.Empty;
			Console.WriteLine($"Use your secret to launch the program, for example:{Environment.NewLine}{sampleUrl}/<secret>");
			if (HideOnStart)
				ConsoleExtension.Hide();

			app.MapGet("/", HandleIt);
			app.MapGet("/{secret}", HandleIt);

			await app.RunAsync();
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

		static int failAttempts = 0;
		static async Task<string> Do(CancellationToken ct, string? clientsecret)
		{
			if (clientsecret?.Trim() != Secret?.Trim())
			{
				if (String.IsNullOrEmpty(clientsecret))
					return "no";
				if (++failAttempts > 1000)
				{
					ConsoleExtension.Show();
					Console.WriteLine("Seems like we're getting spammed, Cap'n. Press any key to terminate.");
					Console.ReadLine();
					Environment.Exit(0);
				}
				return "no";
			}
			return Path;
		}


	}
}