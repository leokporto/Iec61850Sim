using IEC61850.Server;
using Iec61850Sim.Api.Core;
using Iec61850Sim.Api.Iec61850;
using Iec61850Sim.Api.Model;
using Iec61850Sim.Api.Model.Devices;
using Iec61850Sim.Api.Services;

namespace Iec61850Sim.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //carregar modelo IEC61850 
        var cfgPath = Path.Combine(
            AppContext.BaseDirectory,
            "Demo_Ed2.cfg");

        var model = ConfigFileParser.CreateModelFromConfigFile(cfgPath);
        if (model == null)
        {
            Console.WriteLine("No valid data model found");
            return;
        }

        model.SetIedName("Demo");
        
        // Add DI references
        builder.Services.AddSingleton(model);
        builder.Services.AddSingleton<PointRegistry>();
        builder.Services.AddSingleton<DeviceManager>();
        builder.Services.AddSingleton<SimulationClock>();
        builder.Services.AddSingleton<SimulationEngine>();
        builder.Services.AddSingleton<IecServerHost>();
        
        //Add background services
        builder.Services.AddHostedService<SimulationService>();
        
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        var currentModel = app.Services.GetRequiredService<IedModel>();

        //criar registry
        var registry = app.Services.GetRequiredService<PointRegistry>();
        
        //scan do modelo
        var scanner = new ModelScanner();

        scanner.Scan(currentModel, registry, new List<string> { "Measurement", "ProtCtrl" });
        
        // Separacao por devices para simulacao
        var deviceManager = app.Services.GetRequiredService<DeviceManager>();
        var builderDevices = new DeviceBuilder();

        builderDevices.Build(registry, deviceManager);

        Console.WriteLine($"Devices loaded: {deviceManager.Devices.Count}");

        

        app.Run();
    }
}