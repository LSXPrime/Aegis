namespace Aegis.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = CreateHostBuilder(args).Build();

        await builder.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}