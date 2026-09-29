using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var info = new
{
    App = "CrossApp — практикум з крос-платформного програмування",
    Student = "Назар Мазовіта",
    OSDescription = RuntimeInformation.OSDescription,
    EnvironmentOS = Environment.OSVersion.ToString(),
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    BaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Склад (товари, партії, залишки, переміщення)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    Console.WriteLine(JsonSerializer.Serialize(info, options));
}
else
{
    Console.WriteLine(info.App);
    Console.WriteLine($"Студент: {info.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription): {info.OSDescription}");
    Console.WriteLine($"ОС (Environment)  : {info.EnvironmentOS}");
    Console.WriteLine($"Архітектура процесу: {info.Architecture}");
    Console.WriteLine($"Версія .NET (CLR) : {info.DotNetVersion}");
    Console.WriteLine($"Runtime            : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку: {info.BaseDirectory}");
    Console.WriteLine($"Поточний каталог  : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область : {info.Domain}");
}