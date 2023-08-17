# Serilog.Sinks.Aliyun [![release](https://github.com/RayMMond/serilog-sinks-aliyun/actions/workflows/publish.yml/badge.svg)](https://github.com/RayMMond/serilog-sinks-aliyun/actions/workflows/publish.yml) [![NuGet](https://img.shields.io/nuget/v/Serilog.Sinks.Aliyun-SLS.svg)](https://nuget.org/packages/serilog.sinks.aliyun-sls)

A Serilog sink that writes events to the [Aliyun SLS](https://help.aliyun.com/zh/sls/).

### Getting started

Install _Serilog.Sinks.Aliyun-SLS_ into your .NET project:

```powershell
> dotnet add package Serilog.Sinks.Aliyun-SLS
```

Point the logger to Aliyun:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Aliyun("<accessKeyId>", "<accessKeySecret>", "<endpoint>", "<project>", "<logStore>")
    .CreateLogger();
```

And use the Serilog logging methods to associate named properties with log events:

```csharp
Log.Error("Failed to log on user {ContactId}", contactId);
```

Then query log event properties like `ContactId` from the browser:

When the application shuts down, [ensure any buffered events are propertly flushed to Aliyun](https://merbla.com/2016/07/06/serilog-log-closeandflush/) by disposing the logger or calling `Log.CloseAndFlush()`:

```csharp
Log.CloseAndFlush();
```

### JSON `appsettings.json` configuration

To use the Aliyun sink with _Microsoft.Extensions.Configuration_, for example with ASP.NET Core or .NET Core, use the [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) package. First install that package if you have not already done so:

```powershell
dotnet add package Serilog.Settings.Configuration
```

Instead of configuring the Aliyun sink directly in code, call `ReadFrom.Configuration()`:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
```

In your `appsettings.json` file, under the `Serilog` node, :

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Aliyun",
        "Args": {
          "endpoint": "",
          "project": "",
          "logStore": "",
          "accessKeyId": "",
          "accessKeySecret": ""
        }
      }
    ]
  }
}
```

### Dynamic log level control

The Aliyun sink can dynamically adjust the logging level up or down based on the level associated with an API key in Aliyun. To use this feature, create a `LoggingLevelSwitch` to control the `MinimumLevel`, and pass this in the `controlLevelSwitch` parameter of `WriteTo.Aliyun()`:

```csharp
var levelSwitch = new LoggingLevelSwitch();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .WriteTo.Aliyun(... , controlLevelSwitch: levelSwitch)
    .CreateLogger();
```

The equivalent configuration in JSON is:

```json
{
  "Serilog": {
    "LevelSwitches": { "$controlSwitch": "Information" },
    "MinimumLevel": { "ControlledBy": "$controlSwitch" },
    "WriteTo": [
      {
        "Name": "Aliyun",
        "Args": {}
      }
    ]
  }
}
```

### Troubleshooting

#### Client-side issues

If there's no information in the ingestion log, the application was probably unable to reach the server because of network configuration or connectivity issues. These are reported to the application through Serilog's `SelfLog`.

Add the following line after the logger is configured to print any error information to the console:

```csharp
Serilog.Debugging.SelfLog.Enable(Console.Error);
```

If the console is not available, you can pass a delegate into `SelfLog.Enable()` that will be called with each error message:

```csharp
Serilog.Debugging.SelfLog.Enable(message => {
    // Do something with `message`
});
```
