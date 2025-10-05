using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Hosting;

namespace WineAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    //if (!context.HostingEnvironment.IsDevelopment())
                    //{
                    //    var builtConfig = config.Build();
                    //    var keyVaultUri = builtConfig["KeyVault:Uri"];
                    //    config.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
                    //}
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}