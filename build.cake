///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////
#addin nuget:?package=Cake.Json&version=7.0.1

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////
#tool dotnet:?package=GitVersion.Tool&version=5.12.0

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");

var configuration = Argument("configuration", "Release");

var packageSource = Argument<string>("Source", null) 
	?? EnvironmentVariable<string>("INPUT_SOURCE", null); // Input from GHA to Cake

var apiKey = Argument<string>("ApiKey", null) 
	?? EnvironmentVariable<string>("INPUT_APIKEY", null); // Input from GHA to Cake

var packageName = "TestedLibrary";

string versionNumber;
string fullPackageName;

///////////////////////////////////////////////////////////////////////////////
// Setup / Teardown
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    // Executed BEFORE the first task.
});

Teardown(context =>
{
    // Executed AFTER the last task.
});

///////////////////////////////////////////////////////////////////////////////
// Tasks
///////////////////////////////////////////////////////////////////////////////
Task("__PackageArgsCheck")
	.Does(() => {
		if (string.IsNullOrEmpty(packageSource))
			throw new ArgumentException("Source is required");

		if (string.IsNullOrEmpty(apiKey))
			throw new ArgumentException("ApiKey is required");
	});

Task("VersionInfo")
	.Does(() => {

		var version = GitVersion();
		Information(SerializeJsonPretty(version));
		versionNumber = version.SemVer;

		fullPackageName = $"{packageName}.{versionNumber}.nupkg";
		Information($"Full package Name: {fullPackageName}");
	});

Task("BuildAndTest")
	.Does(() => {

		var settings = new DotNetTestSettings
		{
			Configuration = configuration,
			ResultsDirectory = "./artifacts/"
		};

		// Console log for build agent
		settings.Loggers.Add("console;verbosity=normal");
		
		// Logging for file artifacts
		settings.Loggers.Add("trx;logfilename=TestedLibrary.Tests.trx");

		DotNetTest(@"./TestedLibrary.sln", settings);
	});

Task("BuildAndBenchmark")
	.Does(() => {

		var settings = new DotNetRunSettings
		{
			Configuration = "Release", 
			ArgumentCustomization = args => {
				return args
					.Append("--artifacts")
					.AppendQuoted("./artifacts/TestedLibrary.Benchmark");
			}
		};

		DotNetRun(@"./test/TestedLibrary.Benchmark/TestedLibrary.Benchmark.csproj", settings);
	});

Task("PackAndPush")
	.IsDependentOn("__PackageArgsCheck")
	.IsDependentOn("VersionInfo")
	.IsDependentOn("BuildAndTest")
	.Does(() => {

		Information("Packing...");
		var settings = new DotNetMSBuildSettings
		{
			PackageVersion = versionNumber
		};

		var packSettings = new DotNetPackSettings
		{
			Configuration = "Release",
			OutputDirectory = "./artifacts/packages",
			MSBuildSettings = settings
		};
		DotNetPack("src/TestedLibrary/TestedLibrary.csproj", packSettings);

		Information("Pushing...");
		var pushSettings = new DotNetNuGetPushSettings
		{
			Source = packageSource,
			ApiKey = apiKey
		};
		DotNetNuGetPush($"artifacts/packages/{fullPackageName}", pushSettings);
	});

Task("Default")
    .IsDependentOn("BuildAndTest");

RunTarget(target);