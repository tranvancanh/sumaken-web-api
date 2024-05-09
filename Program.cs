using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Mvc.Versioning;
using NLog;
using NLog.Web;
using WarehouseWebApi.Common;
using WarehouseWebApi.Controllers;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Init Program | Start Application");

try
{

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<HttpResponseExceptionFilter>();
        options.Filters.Add<Filter>();
    });
    builder.Services.AddMvc(options =>
    {
    });
    builder.Services.AddApiVersioning(options =>
    {
        options.ApiVersionReader = new MediaTypeApiVersionReader("version");
    })
    .AddMvc();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add Hangfire services
    builder.Services.AddHangfire(config =>
    {
        //config.UseNLogLogProvider(); // Use NLog for Hangfire logging
        config.UseMemoryStorage(); // Use appropriate storage for production
    });

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    //other classes that need the logger 
    builder.Services.AddTransient<ShipmentController>();
    builder.Services.AddSingleton<SchedulingTask>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    // http://surferonwww.info/BlogEngine/post/2021/04/15/how-to-read-posted-body-from-httprequest-body.aspx
    app.Use(async (context, next) =>
    {
        context.Request.EnableBuffering();
        await next();
    });

    //app.UseMiddleware<IOMiddleware>(); //�ǋL

    // Configure Hangfire
#pragma warning disable CS0618 // Type or member is obsolete
    app.UseHangfireServer();
#pragma warning restore CS0618 // Type or member is obsolete
    app.UseHangfireDashboard();

    // Enqueue a job to run immediately
    BackgroundJob.Enqueue<SchedulingTask>(x => x.ExecuteImmediately());
    // Schedule a recurring job / recurring jobs using job Id
    RecurringJob.AddOrUpdate<SchedulingTask>("jobId2", (x) => x.ExecuteDailyAsync(), Cron.Daily()); // UTC time = Japan Time - 9

    // Schedule a job to run after 5 min delay, delayed job
    //BackgroundJob.Schedule<SchedulingTask>((x) => x.ExecuteAsync(), TimeSpan.FromMinutes(10));

    // Schedule a recurring job / recurring jobs using job Id
    //RecurringJob.AddOrUpdate<SchedulingTask>("jobId1", x => x.ExecuteMinutelyAsync(), Cron.Minutely);
    //RecurringJob.AddOrUpdate<SchedulingTask>("jobId2", (x) => x.ExecuteDailyAsync(), Cron.Daily()); // UTC time = Japan Time - 9
    //RecurringJob.AddOrUpdate<SchedulingTask>("jobId2", (x) => x.ExecuteDailyAsync(), Cron.Daily(07, 08)); // UTC time = Japan Time - 9
    //RecurringJob.AddOrUpdate<SchedulingTask>("jobId3", x => x.ExecuteMonthlyAsync(), Cron.Monthly(13, 08, 10)); // UTC time = Japan Time - 9


    await app.RunAsync();

}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}
