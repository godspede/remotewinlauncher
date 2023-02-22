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
			builder.Configuration
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.AddEnvironmentVariables();
			var app = builder.Build();
			Configuration = app.Configuration;


			Console.WriteLine($"Launching on {Configuration["Urls"]}{Environment.NewLine}");
			string sampleUrl = Configuration["Urls"]?.Split(';')?[0] ?? String.Empty;
			Console.WriteLine($"Use your secret to launch the program, for example:{Environment.NewLine}{sampleUrl}/launch/<secret>");
			if (HideOnStart)
				ConsoleExtension.Hide();

			app.MapGet("/launch/", HandleLaunch);
			app.MapGet("/launch/{secret}", HandleLaunch);
			app.MapGet("/kill/", HandleKill);
			app.MapGet("/kill/{secret}", HandleKill);

			await app.RunAsync();
		}

		static string HandleKill(string? secret = null)
		{
			if (!ValidateSecret(secret))
				return "no";
			return Kill();
		}
		static string HandleLaunch(string? secret = null)
		{
			if (!ValidateSecret(secret))
				return "no";
			return Launch();
		}

		static bool ValidateSecret(string? clientsecret)
		{
			if (clientsecret?.Trim() != Secret?.Trim())
			{
				if (String.IsNullOrEmpty(clientsecret))
					return false;
				if (++failAttempts > 1000)
				{
					ConsoleExtension.Show();
					Console.WriteLine("Seems like we're getting spammed, Cap'n. Press any key to terminate.");
					Console.Read();
					Environment.Exit(0);
				}
				return false;
			}
			return true;
		}

		static int failAttempts = 0;
		static Process lastStartedProcess = null;
		static string Launch()
		{
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

		static string Kill()
		{
			bool killed = false;
			var processes = Process.GetProcesses();
			foreach (var process in processes)
			{
				if (process.MainWindowTitle?.Contains(Title) ?? false)
				{
					process.Kill();
					killed = true;
				}
			}
			if (killed)
				return "ded";
			return "no alive sir";
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