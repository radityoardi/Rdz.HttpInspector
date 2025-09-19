using System.Text;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rdz.HttpInspector
{
    public class Program
    {
        public class Options
        {
            [Option('e', "endpoint", Required = false, HelpText = "Listening URL. Default: http://localhost:5000")]
            public string Endpoint { get; set; } = "http://localhost:5000";
        }

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(StartServer)
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("❌ Failed to parse command line arguments.");
                    Environment.Exit(1);
                });
        }

        private static void StartServer(Options opts)
        {
            var builder = WebApplication.CreateBuilder();

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            var app = builder.Build();

            Console.WriteLine($"🔍 HTTP Inspector running at: {opts.Endpoint}");
            Console.WriteLine("Waiting for requests...\n");

            app.Map("/{**catchAll}", async (HttpContext context) =>
            {
                var request = context.Request;
                var bodyStr = "";

                context.Request.EnableBuffering();

                if (request.ContentLength > 0)
                {
                    using var reader = new StreamReader(
                        request.Body,
                        encoding: Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true
                    );
                    bodyStr = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }

                Console.WriteLine("📥 Incoming Request:");
                Console.WriteLine($"➡️ Method:  {request.Method}");
                Console.WriteLine($"➡️ Path:    {request.Path}");
                Console.WriteLine($"➡️ Query:   {request.QueryString}");

                Console.WriteLine("➡️ Headers:");
                foreach (var header in request.Headers)
                {
                    Console.WriteLine($"   {header.Key}: {header.Value}");
                }

                if (!string.IsNullOrWhiteSpace(bodyStr))
                {
                    Console.WriteLine("➡️ Body:");
                    Console.WriteLine(bodyStr);
                }
                else
                {
                    Console.WriteLine("➡️ Body: (empty)");
                }

                Console.WriteLine("------------------------------------------------------\n");

                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("✅ Request received and logged.\n");
            });

            app.Run(opts.Endpoint);
        }
    }

}
