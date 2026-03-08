using IEC61850.Server;
using Iec61850Sim.Web.Components;
using Iec61850Sim.Web.Core;
using Iec61850Sim.Web.Iec61850;
using Iec61850Sim.Web.Model;
using Iec61850Sim.Web.Services;

namespace Iec61850Sim.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        //carregar modelo IEC61850 
        var cfgPath = Path.Combine(
            AppContext.BaseDirectory,
            "./Config/Demo_Ed2.cfg");

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
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        

        app.UseAntiforgery();

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
        
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}