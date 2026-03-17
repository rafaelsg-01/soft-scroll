using System;
using System.IO;
using Serilog;

namespace SoftScroll;

public static class LoggingConfig
{
    public static void Configure()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SoftScroll", "logs");
        
        Directory.CreateDirectory(logDir);
        
        var logPath = Path.Combine(logDir, "softscroll-.log");
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        
        Log.Information("Soft Scroll started");
    }
    
    public static void Shutdown()
    {
        Log.Information("Soft Scroll shutting down");
        Log.CloseAndFlush();
    }
}