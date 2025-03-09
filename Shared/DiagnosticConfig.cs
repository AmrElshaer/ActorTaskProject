using System.Diagnostics;

namespace Shared;

public static class DiagnosticConfig
{
    public static readonly ActivitySource Client = new("client-api");
    public static readonly ActivitySource ServiceA = new("service-a-api");

    public static readonly ActivitySource ServiceB = new("service-b-api");
}