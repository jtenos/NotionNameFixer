using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotionNameFixer;
using System.Reflection;
using System.Text.RegularExpressions;

IConfiguration config = new ConfigurationBuilder()
	.SetBasePath(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName!)
	.AddJsonFile("appsettings.json")
	.AddUserSecrets<Program>()
	.Build();

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = loggerFactory.CreateLogger<Program>();

try
{
	AppSettings appSettings = config.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

	foreach (DirPair dirPair in appSettings.DirPairs)
	{
		if (!Directory.Exists(dirPair.Input))
		{
			logger.LogCritical("Directory {Input} does not exist", dirPair.Input);
			Exit(1);
		}

		if (Directory.Exists(dirPair.Output))
		{
			logger.LogCritical("Directory {Output} already exists", dirPair.Output);
			Exit(1);
		}

		if (Directory.GetFiles(dirPair.Input).Length == 0
			&& Directory.GetDirectories(dirPair.Input).Length == 0)
		{
			logger.LogCritical("Input directory {Input} is empty", dirPair.Input);
			Exit(1);
		}

		logger.LogInformation("Processing directory {Input} to {Output}", dirPair.Input, dirPair.Output);

		HandleDirectory(dirPair.Input, dirPair.Output);
	}

	Exit(0);
}
catch (Exception ex)
{
	logger.LogCritical(ex, "An error occurred");
	Exit(1);
}

void Exit(int exitCode)
{
	loggerFactory.Dispose();
	Environment.Exit(exitCode);
}

void HandleDirectory(string inputDir, string outputDir)
{
	outputDir = Regex.Replace(outputDir, " [abcdefABCDEF1234567890]{32}", "");
	logger.LogInformation("Copying {Input} to {Output}", inputDir, outputDir);
	Directory.CreateDirectory(outputDir);
	foreach (string subDir in Directory.GetDirectories(inputDir))
	{
		HandleDirectory(subDir, Path.Combine(outputDir, new DirectoryInfo(subDir).Name));
	}

	foreach (string file in Directory.GetFiles(inputDir))
	{
		if (IsTextFile(file))
		{
			using StreamReader reader = new(file);
			using StreamWriter writer = new(Path.Combine(outputDir, 
				Regex.Replace(Path.GetFileName(file), " [abcdefABCDEF1234567890]{32}", "")));
			string? line;
			while ((line = reader.ReadLine()) is not null)
			{
				writer.WriteLine(
					Regex.Replace(
						Regex.Replace(line, @"\%20[abcdefABCDEF1234567890]{32}", ""),
						" [abcdefABCDEF1234567890]{32}",
						""
					)
				);
			}
		}
		else
		{
			File.Copy(file, Path.Combine(outputDir, Path.GetFileName(file)));
		}
	}
}

static bool IsTextFile(string fullFileName)
{
	using FileStream fileStream = new(fullFileName, FileMode.Open, FileAccess.Read);
	using StreamReader reader = new(fileStream);
	int c;
	while ((c = reader.Read()) != -1)
	{
		if (c == 0)
		{
			return false;
		}
	}
	return true;
}
