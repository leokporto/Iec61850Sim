using IEC61850.Server;
using Iec61850Sim.Api.Core.Model;
using Iec61850Sim.Api.Model;
using Iec61850Sim.Api.Model.Devices;

namespace Iec61850Sim.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

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

        //criar registry
        var registry = new PointRegistry();

        //scan do modelo
        var scanner = new ModelScanner();

        scanner.Scan(model, registry, new List<string> { "Measurement", "ProtCtrl" });

        Console.WriteLine($"Points discovered: {registry.All.Count()}");
        
        // criar device manager
        var deviceManager = new DeviceManager();

        //construir devices
        var builderDevices = new DeviceBuilder();

        builderDevices.Build(registry, deviceManager);

        Console.WriteLine($"Breakers: {deviceManager.Breakers.Count}");
        Console.WriteLine($"Measurements: {deviceManager.Measurements.Count}");

        // só para teste
        foreach (var b in deviceManager.Breakers)
        {
            Console.WriteLine($"Breaker detected: {b.Name}");
        }

        foreach (var m in deviceManager.Measurements)
        {
            Console.WriteLine($"Measurement detected: {m.Name}");
        }


        app.Run();
    }
}