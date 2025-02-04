using API;
public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    /*
     Kestrel is the web server used in the example, it's a new cross-platform web server for 
     ASP.NET Core that's included in new project templates by default. Kestrel is fine to use 
     on it's own for internal applications and development, but for public facing websites and 
     applications it should sit behind a more mature reverse proxy server (IIS, Apache, Nginx etc) 
     that will receive HTTP requests from the internet and forward them to Kestrel after initial 
     handling and security checks.
    */

    public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)

            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });

}