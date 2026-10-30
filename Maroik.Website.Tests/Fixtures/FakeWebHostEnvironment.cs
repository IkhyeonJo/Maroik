using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IWebHostEnvironment"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    /// <summary>The value <see cref="IWebHostEnvironment.WebRootPath"/> answers; settable by a test.</summary>
    public string WebRootPath { get; set; } = default!;

    /// <summary>The value <see cref="IWebHostEnvironment.WebRootFileProvider"/> answers; settable by a test.</summary>
    public IFileProvider WebRootFileProvider { get; set; } = default!;

    /// <summary>The value <see cref="IHostEnvironment.EnvironmentName"/> answers; settable by a test.</summary>
    public string EnvironmentName { get; set; } = default!;

    /// <summary>The value <see cref="IHostEnvironment.ApplicationName"/> answers; settable by a test.</summary>
    public string ApplicationName { get; set; } = default!;

    /// <summary>The value <see cref="IHostEnvironment.ContentRootPath"/> answers; settable by a test.</summary>
    public string ContentRootPath { get; set; } = default!;

    /// <summary>The value <see cref="IHostEnvironment.ContentRootFileProvider"/> answers; settable by a test.</summary>
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
}
