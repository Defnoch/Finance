using NSwag;
using NSwag.CodeGeneration.TypeScript;

namespace Finance.ApiClientGenerator;

class Program
{
    public static async Task Main(string[] args)
    {
        // Vind de echte projectdirectory, niet bin/Debug
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectDir = exeDir;
        while (!File.Exists(Path.Combine(projectDir, "Finance.ApiClientGenerator.csproj")))
        {
            var parent = Directory.GetParent(projectDir);
            if (parent == null) break;
            projectDir = parent.FullName;
        }
        var swaggerPath = Path.Combine(projectDir, "swagger.json");
        var outputPath = Path.Combine(projectDir, "GeneratedClients/api-client.ts");

        Console.WriteLine($"Loading OpenAPI document from {swaggerPath}...");

        var documentJson = await File.ReadAllTextAsync(swaggerPath);
        var document = await OpenApiDocument.FromJsonAsync(documentJson);

        var settings = new TypeScriptClientGeneratorSettings
        {
            ClassName = "ApiClient",
            Template = TypeScriptTemplate.Angular,
            RxJsVersion = 7,
            InjectionTokenType = InjectionTokenType.InjectionToken,
            // Belangrijk: voeg withCredentials toe zodat cookies/credentials altijd worden meegestuurd
            WithCredentials = true
        };
        settings.TypeScriptGeneratorSettings.TypeScriptVersion = 5.0m;
        settings.TypeScriptGeneratorSettings.DateTimeType = NJsonSchema.CodeGeneration.TypeScript.TypeScriptDateTimeType.String;

        var generator = new TypeScriptClientGenerator(document, settings);
        var code = generator.GenerateFile();

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir!);
        }

        await File.WriteAllTextAsync(outputPath, code);

        Console.WriteLine($"Generated Angular API client to {outputPath}");

        // Voer het shellscript uit om de .NET client te genereren
        var scriptPath = Path.Combine(projectDir, "generate-dotnet-client.sh");
        if (File.Exists(scriptPath))
        {
            Console.WriteLine($"Running {scriptPath} ...");
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = scriptPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Console.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine($"Error: {error}");
        }
        else
        {
            Console.WriteLine($"Script {scriptPath} not found. Skipping .NET client generation.");
        }
    }
}
