using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
using WarehouseWebApi.Common;
using WarehouseWebApi.Controllers;

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
app.Use(async (context, next) => {
    context.Request.EnableBuffering();
    await next();
});

//app.UseMiddleware<IOMiddleware>(); //’Ç‹L

app.Run();
