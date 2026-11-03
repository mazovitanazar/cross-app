using System.Text.Encodings.Web;
using System.Text.Json;
using Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };
    Console.WriteLine(JsonSerializer.Serialize(report, options));
    return;
}

Console.WriteLine("CrossApp — інформація про середовище");
Console.WriteLine("Студент: Назар Мазовіта");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"ОС (OSDescription) : {report.OsDescription}");
Console.WriteLine($"ОС (Environment)   : {report.OsVersion}");
Console.WriteLine($"Runtime            : {report.FrameworkDescription}");
Console.WriteLine($"Версія CLR         : {report.ClrVersion}");
Console.WriteLine($"Архітектура        : {report.ProcessArchitecture}");
Console.WriteLine($"RID (визначено)    : {report.DetectedRid}");
Console.WriteLine($"RID (від .NET)     : {report.ReportedRid}");
Console.WriteLine($"Каталог застосунку : {report.BaseDirectory}");
Console.WriteLine($"Поточний каталог   : {report.CurrentDirectory}");
Console.WriteLine($"Збірка             : {report.BuildNote}");
