using CommandLine;
using Microsoft.Win32;
using System.Diagnostics;

namespace remotewinlauncher
{
	public class Options
	{
		[Option('s', "secret", Required = true, HelpText = "The auth secret used at <url>/launch/<secret> to kick off the program.")]
		public string Secret { get; set; }
		[Option('p', "path", Required = true, HelpText = "The path of the program to launch.")]
		public string Path { get; set; }
		[Option('t', "title", Required = true, HelpText = "The title of the window that will be launched. Used to ensure that a second instance won't be started.")]
		public string Title { get; set; }
		[Option('h', "hide", Required = false, HelpText = "If provided, hides the window on start.", Default = false)]
		public bool HideOnStart { get; set; }
	}
	public class Program
	{
		static IConfiguration? configuration;
		static IConfiguration Configuration { get => configuration ?? throw new Exception(); set => configuration = value; }

		static string Secret { get; set; }
		static string Path { get; set; }
		static string Title { get; set; }
		static bool HideOnStart { get; set; }

		public static async Task Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed<Options>(o =>
				{
					Secret = o.Secret;
					Path = o.Path;
					HideOnStart = o.HideOnStart;
					Title = o.Title;
				})
				.WithNotParsed<Options>((e) =>
				{
					ConsoleExtension.Show();
					Console.WriteLine("Missing arguments! Press any key to terminate.");
					Console.Read();
					Environment.Exit(0);
				});

			var builder = WebApplication.CreateBuilder();
			var app = builder.Build();
			Configuration = app.Configuration;


			Console.WriteLine($"Launching on {Configuration["Urls"]}{Environment.NewLine}");
			string sampleUrl = Configuration["Urls"]?.Split(';')?[0] ?? String.Empty;
			Console.WriteLine($"Use your secret to launch the program, for example:{Environment.NewLine}{sampleUrl}/launch/<secret>");
			if (HideOnStart)
				ConsoleExtension.Hide();

			app.MapGet("/launch/", HandleIt);
			app.MapGet("/launch/{secret}", HandleIt);

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
			return await Launch(cts.Token, secret);
		}

		static int failAttempts = 0;
		static Process lastStartedProcess = null;
		static async Task<string> Launch(CancellationToken ct, string? clientsecret)
		{
			if (clientsecret?.Trim() != Secret?.Trim())
			{
				if (String.IsNullOrEmpty(clientsecret))
					return "no";
				if (++failAttempts > 1000)
				{
					ConsoleExtension.Show();
					Console.WriteLine("Seems like we're getting spammed, Cap'n. Press any key to terminate.");
					Console.Read();
					Environment.Exit(0);
				}
				return "no";
			}
			var result = ProcessFinder.GetHandleWindow(Title);
			if (result != default(IntPtr))
			{
				// Already opened
				return "already running";
			}
			if (lastStartedProcess != null && !lastStartedProcess.HasExited)
			{
				return "process still running";
			}
			lastStartedProcess = StartProcess(Path, Title);
			return "ok";
		}

		static Process StartProcess(string path, string title)
		{
			var process = new Process();
			process.StartInfo.FileName = path; 
			process.StartInfo.UseShellExecute = true;
			process.StartInfo.CreateNoWindow = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			process.Start();

			SpinWait.SpinUntil(() => process.MainWindowHandle != IntPtr.Zero);
			ProcessFinder.SetWindowText(process.MainWindowHandle, title);

			return process;
		}
	}
}