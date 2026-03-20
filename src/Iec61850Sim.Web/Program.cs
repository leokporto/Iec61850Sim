using IEC61850.Server;
using Iec61850Sim.Core;
using MudBlazor.Services;
using Iec61850Sim.Core.Commands;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Model.Tree;
using Iec61850Sim.Core.Points;
using Iec61850Sim.Core.Simulation;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Web.Components;
using Iec61850Sim.Web.Services;

namespace Iec61850Sim.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var app = BuildApp(args);

        if (app == null)
        {
            Console.Write("Unable to start application: Loading failed.");
            return;
        }

        app.Run();
    }

    public static WebApplication? BuildApp(string[] args)
    {
        //var options = new WebApplicationOptions
        //{
        //    ContentRootPath = AppContext.BaseDirectory,
        //    WebRootPath = "wwwroot"
        //};

        var builder = WebApplication.CreateBuilder(args);

        


        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddMudServices();

        builder.WebHost.UseStaticWebAssets();

        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        var modelPath = Environment.GetEnvironmentVariable("IEC_MODEL")
               ?? "./Config/Demo_Ed2.cfg";

        //carregar modelo IEC61850 
        var cfgPath = Path.Combine(AppContext.BaseDirectory, modelPath);

        var model = ConfigFileParser.CreateModelFromConfigFile(cfgPath);
        if (model == null)
        {
            Console.WriteLine("No valid data model found");
            return null;
        }

        model.SetIedName("Demo");

        // Add DI references
        builder.Services.AddSingleton(model);
        builder.Services.AddSingleton<PointRegistry>();
        builder.Services.AddSingleton<DeviceManager>();
        builder.Services.AddSingleton<SimulationClock>();
        builder.Services.AddSingleton<SimulationEngine>();
        builder.Services.AddSingleton<ControlCommandProcessor>();

        builder.Services.AddSingleton<IecServerHost>();
        

        builder.Services.AddSingleton<RuntimeEngine>();

        builder.Services.AddSingleton<IModelNode>(sp =>
        {
            var mdl = sp.GetRequiredService<IedModel>();
            var reg = sp.GetRequiredService<PointRegistry>();
            return new ModelTreeBuilder().Build("Demo", mdl, reg);
        });

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


        app.UseAntiforgery();


        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }

    
}