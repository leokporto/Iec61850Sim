using FubarDev.FtpServer;
using FubarDev.FtpServer.AccountManagement;
using FubarDev.FtpServer.FileSystem.DotNet;

using IEC61850.Server;

using Iec61850Sim.Core;

using MudBlazor.Services;

using Iec61850Sim.Core.Commands;
using Iec61850Sim.Core.Device;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model.Tree;
using Iec61850Sim.Core.Points;
using Iec61850Sim.Core.Scl;
using Iec61850Sim.Core.Simulation;
using Iec61850Sim.Core.Comtrade;
using Iec61850Sim.Web.Components;
using Iec61850Sim.Web.Services;

namespace Iec61850Sim.Web;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            BuildApp(args).Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to start application: {ex.Message}");
        }
    }

    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Framework setup
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddMudServices();
        builder.WebHost.UseStaticWebAssets();
        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        // DI registrations
        builder.Services.AddSingleton<ISclConversionService, SclConversionService>();
        builder.Services.AddSingleton<ServerCapabilities>();
        builder.Services.AddSingleton<IedModel>(IedServerManager.CreateModel);
        builder.Services.AddSingleton<PointRegistry>();
        builder.Services.AddSingleton<IPointRegistry>(sp => sp.GetRequiredService<PointRegistry>());
        builder.Services.AddSingleton<DeviceManager>();
        builder.Services.AddSingleton<SimulationClock>();
        builder.Services.AddSingleton<SimulationEngine>();
        builder.Services.AddSingleton<ControlCommandProcessor>();
        builder.Services.AddSingleton<IecServerHost>();
        builder.Services.AddSingleton<RuntimeEngine>();
        builder.Services.AddSingleton<IOperationRuntime>(sp => sp.GetRequiredService<RuntimeEngine>());
        builder.Services.AddSingleton<ManualOperationService>();
        builder.Services.AddSingleton<IedServerManager>();
        builder.Services.AddSingleton<IComtradeService, ComtradeService>();
        builder.Services.AddSingleton<IModelNode>(IedServerManager.CreateModelNode);
        builder.Services.AddHostedService<SimulationService>();

        var fileStorePath = Path.Combine(AppContext.BaseDirectory, "FileStore");
        builder.Services.AddFtpServer(b => b.UseDotNetFileSystem());
        builder.Services.Configure<FtpServerOptions>(o => o.Port = 21);
        builder.Services.Configure<DotNetFileSystemOptions>(o => o.RootPath = fileStorePath);
        builder.Services.AddSingleton<IMembershipProvider, SingleUserMembershipProvider>();
        builder.Services
            .AddSingleton<Iec61850Sim.Core.Iec61850.IFtpServerHost, Iec61850Sim.Web.Services.FtpServerHost>();

        var app = builder.Build();

        // HTTP pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Scan model and build device registry
        app.Services.GetRequiredService<IedServerManager>().Initialize();

        return app;
    }
}
