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

var packageName = Argument<string>("PackageName", null) 
	?? EnvironmentVariable<string>("INPUT_PACKAGENAME", null) // Input from GHA to Cake
	?? "TestedLibrary";

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
		Information("Testing...");
		DotNetTest(@"./TestedLibrary.sln");
	});

Task("PackAndPush")
	.IsDependentOn("__PackageArgsCheck")
	.IsDependentOn("VersionInfo")
	.IsDependentOn("BuildAndTest")
	.Does(() => {

		// https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack
		Information("Packing...");
		var settings = new DotNetMSBuildSettings
		{
			PackageVersion = versionNumber
		};

		var packSettings = new DotNetPackSettings
		{
			Configuration = "Release",
			OutputDirectory = "./artifacts/",
			MSBuildSettings = settings
		};
		DotNetPack("src/TestedLibrary/TestedLibrary.csproj", packSettings);

		Information("Pushing...");
		var pushSettings = new DotNetNuGetPushSettings
		{
			Source = packageSource,
			ApiKey = apiKey
		};
		DotNetNuGetPush($"artifacts/{fullPackageName}", pushSettings);
	});

Task("Default")
    .IsDependentOn("BuildAndTest");

RunTarget(target);