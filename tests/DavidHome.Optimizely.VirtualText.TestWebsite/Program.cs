namespace DavidHome.Optimizely.VirtualText.TestWebsite;

public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureCmsDefaults()
            .ConfigureAppConfiguration(config => config.AddUserSecrets<Program>())
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}