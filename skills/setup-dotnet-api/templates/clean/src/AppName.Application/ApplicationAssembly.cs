using System.Reflection;

namespace AppName.Application;

/// <summary>
/// Finds this assembly for the registrations that scan it (MediatR handlers, FluentValidation validators).
/// </summary>
public static class ApplicationAssembly
{
    public static Assembly Instance { get; } = typeof(ApplicationAssembly).Assembly;
}
