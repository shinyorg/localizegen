using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddStronglyTypedLocalizations();
var app = builder.Build();

var localizer = app.Services.GetRequiredService<SampleClassLocalized>();
Console.WriteLine(localizer.First);
Console.WriteLine(localizer.Second);
Console.WriteLine(localizer.MyStringFormat("Allan", "Ritchie"));