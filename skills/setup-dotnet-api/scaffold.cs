using System.Text;
using System.Text.RegularExpressions;

const string Usage = """
Usage:
  dotnet run scaffold.cs -- --name <Name> --out <dir> --arch <clean|vsa> [--auth] [--permissions] [--signup] [--docker]

Options:
  --name <Name>     Solution name, PascalCase segments, dots allowed (e.g. Acme.Shop).
  --out <dir>       Output folder. Must not exist or must be empty.
  --arch <arch>     Architecture: clean or vsa.
  --auth            Enable the auth module.
  --permissions     Enable the permissions module (needs --auth).
  --signup          Enable the signup module (needs --auth).
  --docker          Enable the docker module.
  --help            Show this text.
""";

static int Fail(string message, string usage)
{
    Console.Error.WriteLine("Error: " + message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(usage);
    return 1;
}

string? name = null, outDir = null, arch = null;
var enabled = new HashSet<string>();
var known = new HashSet<string> { "auth", "permissions", "signup", "docker" };

for (var i = 0; i < args.Length; i++)
{
    var a = args[i];
    switch (a)
    {
        case "--help":
        case "-h":
            Console.WriteLine(Usage);
            return 0;
        case "--name":
        case "--out":
        case "--arch":
            if (i + 1 >= args.Length) return Fail($"Missing value for {a}.", Usage);
            var v = args[++i];
            if (a == "--name") name = v; else if (a == "--out") outDir = v; else arch = v;
            break;
        case "--auth": enabled.Add("auth"); break;
        case "--permissions": enabled.Add("permissions"); break;
        case "--signup": enabled.Add("signup"); break;
        case "--docker": enabled.Add("docker"); break;
        default:
            return Fail($"Unknown argument: {a}", Usage);
    }
}

if (name is null) return Fail("--name is required.", Usage);
if (!Regex.IsMatch(name, @"^[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)*$"))
    return Fail($"Invalid --name '{name}'. Use PascalCase segments separated by dots.", Usage);
if (outDir is null) return Fail("--out is required.", Usage);
if (arch is null) return Fail("--arch is required.", Usage);
if (arch != "clean" && arch != "vsa") return Fail($"Invalid --arch '{arch}'. Use clean or vsa.", Usage);
if (enabled.Contains("permissions") && !enabled.Contains("auth")) return Fail("--permissions needs --auth.", Usage);
if (enabled.Contains("signup") && !enabled.Contains("auth")) return Fail("--signup needs --auth.", Usage);

var outFull = Path.GetFullPath(outDir);
if (File.Exists(outFull)) return Fail($"--out '{outFull}' is a file.", Usage);
if (Directory.Exists(outFull) && Directory.EnumerateFileSystemEntries(outFull).Any())
    return Fail($"--out folder '{outFull}' is not empty.", Usage);

var scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Directory.GetCurrentDirectory();
var templateRoot = Path.Combine(scriptDir, "templates", arch);
if (!Directory.Exists(templateRoot))
    return Fail($"Template folder not found: {templateRoot}", Usage);

var lower = name.Replace('.', '-').ToLowerInvariant();
string Rename(string s) => s.Replace("AppName", name, StringComparison.Ordinal).Replace("appname", lower, StringComparison.Ordinal);

var fileHeader = new Regex(@"^(//|#|<!--)\s*@file-module:(\w+)\s*(-->)?$");
var startRx = new Regex(@"^(//|#|<!--)\s*@module:(!?)(\w+)\s*(-->)?$");
var endRx = new Regex(@"^(//|#|<!--)\s*@end\s*(-->)?$");

var written = 0;
var files = Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories)
    .Select(f => (Full: f, Rel: Path.GetRelativePath(templateRoot, f)))
    .Where(f => f.Rel.Split(Path.DirectorySeparatorChar).All(s => s != "bin" && s != "obj") && Path.GetFileName(f.Full) != ".DS_Store")
    .OrderBy(f => f.Rel, StringComparer.Ordinal)
    .ToList();

var errors = new List<string>();
var outputs = new List<(string Path, string Text)>();

foreach (var (full, rel) in files)
{
    var text = File.ReadAllText(full, Encoding.UTF8);
    if (text.Length > 0 && text[0] == '﻿') text = text[1..];

    var lines = text.Split('\n').ToList();
    if (lines.Count > 0)
    {
        var m = fileHeader.Match(lines[0].Trim());
        if (m.Success)
        {
            var mod = m.Groups[2].Value;
            if (!known.Contains(mod))
            {
                errors.Add($"{rel}:1: unknown module '{mod}'.");
                continue;
            }
            if (!enabled.Contains(mod)) continue;
            lines.RemoveAt(0);
        }
    }

    var offset = lines.Count > 0 && text.Split('\n').Length != lines.Count ? 1 : 0;
    var stack = new List<(bool Keep, int Line)>();
    var sb = new List<string>();
    var bad = false;
    for (var i = 0; i < lines.Count; i++)
    {
        var lineNo = i + 1 + offset;
        var trimmed = lines[i].Trim();
        var sm = startRx.Match(trimmed);
        if (sm.Success)
        {
            var mod = sm.Groups[3].Value;
            if (!known.Contains(mod))
            {
                errors.Add($"{rel}:{lineNo}: unknown module '{mod}'.");
                bad = true;
                break;
            }
            var neg = sm.Groups[2].Value == "!";
            stack.Add((enabled.Contains(mod) != neg, lineNo));
            continue;
        }
        if (endRx.IsMatch(trimmed))
        {
            if (stack.Count == 0)
            {
                errors.Add($"{rel}:{lineNo}: @end without a matching @module.");
                bad = true;
                break;
            }
            stack.RemoveAt(stack.Count - 1);
            continue;
        }
        if (stack.All(s => s.Keep)) sb.Add(lines[i]);
    }
    if (bad) continue;
    if (stack.Count > 0)
    {
        errors.Add($"{rel}:{stack[^1].Line}: @module without @end at end of file.");
        continue;
    }

    var result = Rename(string.Join('\n', sb));
    if (full.EndsWith(".cs", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(result)) continue;

    var outRel = string.Join(Path.DirectorySeparatorChar, rel.Split(Path.DirectorySeparatorChar).Select(Rename));
    outputs.Add((Path.Combine(outFull, outRel), result));
}

if (errors.Count > 0)
{
    Console.Error.WriteLine("Error: template problems found. Nothing was written.");
    foreach (var e in errors) Console.Error.WriteLine("  " + e);
    return 1;
}

var utf8 = new UTF8Encoding(false);
foreach (var (path, content) in outputs)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, content, utf8);
    written++;
}

var modules = enabled.Count == 0 ? "core" : "core, " + string.Join(", ", new[] { "auth", "permissions", "signup", "docker" }.Where(enabled.Contains));
Console.WriteLine($"Solution folder: {outFull}");
Console.WriteLine($"Architecture:    {arch}");
Console.WriteLine($"Modules:         {modules}");
Console.WriteLine($"Files written:   {written}");
return 0;
