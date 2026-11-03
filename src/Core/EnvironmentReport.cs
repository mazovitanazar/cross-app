namespace Core;

// Лише дані про середовище; друкує їх Cli.
public sealed record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string FrameworkDescription,
    string ClrVersion,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string BaseDirectory,
    string CurrentDirectory,
    string BuildNote);
