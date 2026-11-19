using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

const string Usage = """
Usage:
  dotnet run scaffold.cs -- new --name <Name> --out <dir> --arch <clean|vsa> --service <Service> [--port <n>]
                               [--auth] [--permissions] [--signup] [--docker] [--root <dir>] [--staging <dir>]
  dotnet run scaffold.cs -- add --solution <dir> --service <Service> --staging <dir> [--port <n>]
                               [--auth | --trust] [--permissions] [--signup]
  dotnet run scaffold.cs -- render --setup <setup.json> --out <dir>

Commands:
  new      Create a solution with its first service.
  add      Add a service to a solution that has .agents/setup.json (format 2).
  render   Render the solution that a setup.json describes into an empty folder.

Options:
  --name <Name>       Solution name, PascalCase segments, dots allowed (e.g. Acme.Shop).
  --out <dir>         Output folder. Must not exist or must be empty.
  --arch <arch>       Architecture: clean or vsa.
  --service <Service> Service name, one PascalCase segment (e.g. Admin). Projects: <Name>.<Service>.*
  --port <n>          HTTP port of the service in development. Default: 5070 for new,
                      the highest port in the solution + 1 for add.
  --auth              The service has its own accounts and issues tokens.
  --trust             The service accepts the tokens of an auth service in the solution (add only).
  --permissions       Roles and permissions (needs --auth).
  --signup            Self-registration on (needs --auth).
  --docker            Dockerfile per service and compose.yaml (new only; add uses the solution setting).
  --root <dir>        Folder for the root agent files (AGENTS.md, CLAUDE.md, .agents/INSTRUCTIONS.md),
                      for example the repository root (new). The --out folder or a parent of it.
                      Default: the --out folder.
  --solution <dir>    Solution folder (add).
  --staging <dir>     Empty folder for the files that the agent merges: the shared files that changed
                      (add), the root files that already exist (new).
  --setup <file>      A setup.json in format 2 (render).
  --help              Show this text.
""";

string[] moduleOrder = ["auth", "trust", "permissions", "signup", "docker", "jwt"];
var serviceModuleNames = new HashSet<string> { "auth", "trust", "permissions", "signup" };
var solutionModuleNames = new HashSet<string> { "docker" };
var known = moduleOrder.ToHashSet();

var nameRx = new Regex(@"^[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)*$");
var serviceRx = new Regex(@"^[A-Z][A-Za-z0-9]*$");
var fileHeader = new Regex(@"^(//|#|<!--)\s*@file-module:(\w+)\s*(-->)?$");
var startRx = new Regex(@"^(//|#|<!--)\s*@module:(!?)(\w+)\s*(-->)?$");
var eachRx = new Regex(@"^(//|#|<!--)\s*@each-service\s*(-->)?$");
var endRx = new Regex(@"^(//|#|<!--)\s*@end\s*(-->)?$");

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine(Usage);
    return args.Length == 0 ? 1 : 0;
}

var command = args[0];
var valueOptions = new Dictionary<string, string[]>
{
    ["new"] = ["--name", "--out", "--arch", "--service", "--port", "--root", "--staging"],
    ["add"] = ["--solution", "--service", "--port", "--staging"],
    ["render"] = ["--setup", "--out"],
};
var flagOptions = new Dictionary<string, string[]>
{
    ["new"] = ["--auth", "--permissions", "--signup", "--docker"],
    ["add"] = ["--auth", "--trust", "--permissions", "--signup"],
    ["render"] = [],
};
if (!valueOptions.ContainsKey(command)) return Fail($"Unknown command: {command}. Use new, add or render.");

var values = new Dictionary<string, string>();
var flags = new HashSet<string>();
for (var i = 1; i < args.Length; i++)
{
    var a = args[i];
    if (a is "--help" or "-h")
    {
        Console.WriteLine(Usage);
        return 0;
    }
    if (valueOptions[command].Contains(a))
    {
        if (i + 1 >= args.Length) return Fail($"Missing value for {a}.");
        values[a] = args[++i];
    }
    else if (flagOptions[command].Contains(a)) flags.Add(a[2..]);
    else return Fail($"Unknown argument for '{command}': {a}");
}

string? Value(string option) => values.TryGetValue(option, out var v) ? v : null;

var scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Directory.GetCurrentDirectory();
var pluginJson = Path.Combine(scriptDir, "..", "..", ".claude-plugin", "plugin.json");
var kitVersion = File.Exists(pluginJson)
    ? JsonNode.Parse(File.ReadAllText(pluginJson))?["version"]?.GetValue<string>() ?? "unknown"
    : "unknown";
var utf8 = new UTF8Encoding(false);

switch (command)
{
    case "new":
    {
        var name = Value("--name");
        var outDir = Value("--out");
        var arch = Value("--arch");
        var serviceName = Value("--service");
        if (name is null) return Fail("--name is required.");
        if (outDir is null) return Fail("--out is required.");
        if (arch is null) return Fail("--arch is required.");
        if (serviceName is null) return Fail("--service is required.");
        if (!TryParsePort(Value("--port") ?? "5070", out var port)) return Fail($"Invalid --port '{Value("--port")}'.");

        var outFull = Path.GetFullPath(outDir);
        var rootFull = Path.GetFullPath(Value("--root") ?? outDir);
        var solutionPath = Path.GetRelativePath(rootFull, outFull);
        if (solutionPath == ".." || solutionPath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(solutionPath))
            return Fail($"--root '{rootFull}' must be the --out folder or a parent of it.");

        var setup = new Setup(name, arch, kitVersion,
            flags.Where(solutionModuleNames.Contains).ToList(),
            [new Service(serviceName, port, flags.Where(serviceModuleNames.Contains).ToList())],
            solutionPath.Replace(Path.DirectorySeparatorChar, '/'));
        return WriteNewSolution(setup, outFull, rootFull, Value("--staging"), "Solution folder");
    }
    case "render":
    {
        var setupPath = Value("--setup");
        var outDir = Value("--out");
        if (setupPath is null) return Fail("--setup is required.");
        if (outDir is null) return Fail("--out is required.");
        if (!TryReadSetup(setupPath, out var setup, out var error)) return Fail(error);
        // The render keeps the layout of the original: the root files in --out, the solution in
        // --out/<solutionPath>.
        var outFull = Path.GetFullPath(outDir);
        if (Directory.Exists(outFull) && Directory.EnumerateFileSystemEntries(outFull).Any())
            return Fail($"--out folder '{outFull}' is not empty.");
        var solutionFull = Path.GetFullPath(Path.Combine(outFull, setup.SolutionPath));
        return WriteNewSolution(setup with { KitVersion = kitVersion }, solutionFull, outFull, null, "Render folder");
    }
    default:
        return AddService();
}

int AddService()
{
    var solutionDir = Value("--solution");
    var serviceName = Value("--service");
    var stagingDir = Value("--staging");
    if (solutionDir is null) return Fail("--solution is required.");
    if (serviceName is null) return Fail("--service is required.");
    if (stagingDir is null) return Fail("--staging is required.");

    var solutionFull = Path.GetFullPath(solutionDir);
    var setupPath = Path.Combine(solutionFull, ".agents", "setup.json");
    if (!File.Exists(setupPath)) return Fail($"{setupPath} does not exist. Write the setup record (format 2) first.");
    if (!TryReadSetup(setupPath, out var oldSetup, out var readError)) return Fail(readError);

    var port = oldSetup.Services.Max(s => s.Port) + 1;
    if (Value("--port") is { } portText && !TryParsePort(portText, out port)) return Fail($"Invalid --port '{portText}'.");

    var service = new Service(serviceName, port, flags.Where(serviceModuleNames.Contains).ToList());
    var newSetup = oldSetup with { Services = [.. oldSetup.Services, service] };
    var setupErrors = Validate(newSetup);
    if (setupErrors.Count > 0) return Fail(string.Join(Environment.NewLine + "       ", setupErrors));

    var stagingFull = Path.GetFullPath(stagingDir);
    if (File.Exists(stagingFull)) return Fail($"--staging '{stagingFull}' is a file.");
    if (Directory.Exists(stagingFull) && Directory.EnumerateFileSystemEntries(stagingFull).Any())
        return Fail($"--staging folder '{stagingFull}' is not empty.");

    var slnxFiles = Directory.GetFiles(solutionFull, "*.slnx");
    if (slnxFiles.Length != 1) return Fail($"Expected one .slnx file in {solutionFull}, found {slnxFiles.Length}.");

    var errors = new List<string>();
    var oldSolution = RenderSolution(oldSetup, SolutionTemplateRoots(oldSetup), errors);
    var newSolution = RenderSolution(newSetup, SolutionTemplateRoots(newSetup), errors);
    var serviceFiles = RenderService(newSetup, service, errors);
    if (errors.Count > 0) return TemplateFail(errors);

    var writes = new List<(string Rel, string Text)>();
    foreach (var (rel, text) in serviceFiles)
    {
        if (File.Exists(Path.Combine(solutionFull, rel)))
            errors.Add($"{rel}: the file already exists in the solution.");
        writes.Add((rel, text));
    }
    if (errors.Count > 0) return TemplateFail(errors, "The service conflicts with existing files. Nothing was written.");

    var added = new List<string>();
    var merges = new List<string>();
    foreach (var (rel, text) in newSolution)
    {
        if (rel.EndsWith(".slnx", StringComparison.Ordinal)) continue;
        var target = Path.Combine(solutionFull, rel);
        // A shared project that the solution does not have (for example a solution changed by hand)
        // is added whole, so the new service can reference it.
        var parts = rel.Split(Path.DirectorySeparatorChar);
        if (parts.Length >= 3 && parts[0] is ("src" or "tests") && !Directory.Exists(Path.Combine(solutionFull, parts[0], parts[1])))
        {
            writes.Add((rel, text));
            added.Add(rel);
            continue;
        }
        if (!oldSolution.TryGetValue(rel, out var oldText))
        {
            // A file of a module that this service turns on for the solution, for example the
            // shared JWT project. Add it when it is missing; never overwrite a user's file.
            if (!File.Exists(target))
            {
                writes.Add((rel, text));
                added.Add(rel);
                continue;
            }
            oldText = "";
        }
        else if (oldText == text) continue;

        merges.Add(rel);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(stagingFull, "new", rel))!);
        File.WriteAllText(Path.Combine(stagingFull, "new", rel), text, utf8);
        if (oldText.Length > 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(stagingFull, "old", rel))!);
            File.WriteAllText(Path.Combine(stagingFull, "old", rel), oldText, utf8);
        }
    }

    var slnxText = AddProjectsToSlnx(File.ReadAllText(slnxFiles[0]),
        writes.Select(w => w.Rel).Where(r => r.EndsWith(".csproj", StringComparison.Ordinal)));

    foreach (var (rel, text) in writes)
    {
        var path = Path.Combine(solutionFull, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, utf8);
    }
    File.WriteAllText(slnxFiles[0], slnxText, utf8);
    File.WriteAllText(setupPath, SetupJson(newSetup), utf8);

    Console.WriteLine($"Solution folder: {solutionFull}");
    Console.WriteLine($"Service:         {service.Name} (port {service.Port}, modules: {ModuleList(service.Modules)})");
    Console.WriteLine($"Service files:   {serviceFiles.Count}");
    Console.WriteLine($"Shared files added ({added.Count}):");
    foreach (var rel in added) Console.WriteLine($"  {rel}");
    Console.WriteLine($"Shared files to merge ({merges.Count}):");
    foreach (var rel in merges) Console.WriteLine($"  {rel}");
    if (merges.Count > 0)
    {
        Console.WriteLine($"Staging folder:  {stagingFull}");
        Console.WriteLine("  new/<file> is the render with the new service, old/<file> the render without it.");
        Console.WriteLine("  Apply the difference between them to the file in the solution.");
    }
    Console.WriteLine($"Updated:         {Path.GetFileName(slnxFiles[0])}, .agents/setup.json");
    return 0;
}

int WriteNewSolution(Setup setup, string outFull, string rootFull, string? stagingDir, string label)
{
    var setupErrors = Validate(setup);
    if (setup.Services.Any(s => s.Modules.Contains("trust")) && command == "new")
        setupErrors.Add("--trust needs an auth service in the solution. Add the trust service with 'add'.");
    if (setupErrors.Count > 0) return Fail(string.Join(Environment.NewLine + "       ", setupErrors));

    if (File.Exists(outFull)) return Fail($"--out '{outFull}' is a file.");
    if (Directory.Exists(outFull) && Directory.EnumerateFileSystemEntries(outFull).Any())
        return Fail($"--out folder '{outFull}' is not empty.");
    if (File.Exists(rootFull)) return Fail($"--root '{rootFull}' is a file.");

    var errors = new List<string>();
    var files = RenderSolution(setup, SolutionTemplateRoots(setup), errors);
    foreach (var service in setup.Services)
    {
        foreach (var (rel, text) in RenderService(setup, service, errors))
        {
            if (!files.TryAdd(rel, text)) errors.Add($"{rel}: a service template and a solution template write the same file.");
        }
    }
    var rootFiles = RenderSolution(setup, [Path.Combine(scriptDir, "templates", "root")], errors);
    if (setup.SolutionPath == ".")
    {
        foreach (var rel in rootFiles.Keys.Where(files.ContainsKey))
            errors.Add($"{rel}: a root template and a solution template write the same file.");
    }
    if (errors.Count > 0) return TemplateFail(errors);

    var slnx = files.Keys.Where(k => k.EndsWith(".slnx", StringComparison.Ordinal) && !k.Contains(Path.DirectorySeparatorChar)).ToList();
    if (slnx.Count != 1) return TemplateFail([$"Expected one .slnx file in the solution templates, found {slnx.Count}."]);
    files[slnx[0]] = AddProjectsToSlnx(files[slnx[0]], files.Keys.Where(k => k.EndsWith(".csproj", StringComparison.Ordinal)));
    files[Path.Combine(".agents", "setup.json")] = SetupJson(setup);

    // A root file that exists belongs to the repository. The agent merges the kit's text into it.
    var writes = files.Select(f => (Path: Path.Combine(outFull, f.Key), f.Value)).ToList();
    var rootWritten = new List<string>();
    var rootMerges = new List<string>();
    foreach (var (rel, text) in rootFiles)
    {
        var target = Path.Combine(rootFull, rel);
        if (File.Exists(target)) rootMerges.Add(rel);
        else
        {
            writes.Add((target, text));
            rootWritten.Add(rel);
        }
    }

    string? stagingFull = null;
    if (rootMerges.Count > 0)
    {
        if (stagingDir is null)
            return Fail($"The root folder already has {string.Join(", ", rootMerges)}. Pass --staging <empty folder>: the kit's version of these files goes there. Nothing was written.");
        stagingFull = Path.GetFullPath(stagingDir);
        if (File.Exists(stagingFull)) return Fail($"--staging '{stagingFull}' is a file.");
        if (Directory.Exists(stagingFull) && Directory.EnumerateFileSystemEntries(stagingFull).Any())
            return Fail($"--staging folder '{stagingFull}' is not empty.");
        foreach (var rel in rootMerges)
            writes.Add((Path.Combine(stagingFull, "new", rel), rootFiles[rel]));
    }

    foreach (var (path, text) in writes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, utf8);
    }

    Console.WriteLine($"{label + ":",-17}{outFull}");
    Console.WriteLine($"Root folder:     {rootFull}");
    Console.WriteLine($"Architecture:    {setup.Arch}");
    Console.WriteLine($"Solution:        {setup.Name} (modules: {ModuleList(setup.Modules)})");
    foreach (var s in setup.Services)
        Console.WriteLine($"Service:         {s.Name} (port {s.Port}, modules: {ModuleList(s.Modules)})");
    Console.WriteLine($"Files written:   {files.Count} in the solution folder, {rootWritten.Count} in the root folder");
    Console.WriteLine($"Root files to merge ({rootMerges.Count}):");
    foreach (var rel in rootMerges) Console.WriteLine($"  {rel}");
    if (stagingFull is not null)
    {
        Console.WriteLine($"Staging folder:  {stagingFull}");
        Console.WriteLine("  new/<file> is the kit's version. Merge it into the file in the root folder.");
    }
    return 0;
}

// ── Rendering ──────────────────────────────────────────────────────────────────

string[] SolutionTemplateRoots(Setup setup) =>
    [Path.Combine(scriptDir, "templates", "shared"), Path.Combine(scriptDir, "templates", setup.Arch, "solution")];

// Renders templates that exist once per solution: the solution templates, or the root templates.
SortedDictionary<string, string> RenderSolution(Setup setup, string[] roots, List<string> errors)
{
    var modules = SolutionModules(setup);
    var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
    foreach (var (full, rel) in TemplateFiles(roots, errors))
    {
        if (rel.Contains("ServiceName", StringComparison.Ordinal))
        {
            errors.Add($"{rel}: a solution template path must not contain ServiceName.");
            continue;
        }
        var text = RenderFile(full, rel, modules, null, setup, errors);
        if (text is null) continue;
        if (text.Contains("ServiceName", StringComparison.Ordinal) || text.Contains("ServicePort", StringComparison.Ordinal))
        {
            errors.Add($"{rel}: ServiceName or ServicePort is outside an @each-service block.");
            continue;
        }
        result[Rename(rel, setup, null)] = text;
    }
    return result;
}

SortedDictionary<string, string> RenderService(Setup setup, Service service, List<string> errors)
{
    var roots = new[] { Path.Combine(scriptDir, "templates", setup.Arch, "service") };
    var modules = ServiceModules(setup, service);
    var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
    foreach (var (full, rel) in TemplateFiles(roots, errors))
    {
        var text = RenderFile(full, rel, modules, service, setup, errors);
        if (text is null) continue;
        result[Rename(rel, setup, service)] = text;
    }
    return result;
}

List<(string Full, string Rel)> TemplateFiles(string[] roots, List<string> errors)
{
    foreach (var root in roots)
        if (!Directory.Exists(root))
        {
            errors.Add($"Template folder not found: {root}");
            return [];
        }

    var files = roots
        .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => (Full: f, Rel: Path.GetRelativePath(root, f))))
        .Where(f => f.Rel.Split(Path.DirectorySeparatorChar).All(s => s != "bin" && s != "obj") && Path.GetFileName(f.Full) != ".DS_Store")
        .OrderBy(f => f.Rel, StringComparer.Ordinal)
        .ToList();
    foreach (var dup in files.GroupBy(f => f.Rel).Where(g => g.Count() > 1))
        errors.Add($"{dup.Key}: the file is in more than one template folder ({string.Join(", ", roots.Select(r => Path.GetRelativePath(scriptDir, r)))}). Keep one copy.");
    return files;
}

/// <returns>The rendered text, or null when the file is left out or has an error.</returns>
string? RenderFile(string full, string rel, HashSet<string> modules, Service? service, Setup setup, List<string> errors)
{
    var text = File.ReadAllText(full, Encoding.UTF8);
    if (text.Length > 0 && text[0] == '﻿') text = text[1..];

    var lines = text.Split('\n').ToList();
    var firstLineNo = 1;
    var header = fileHeader.Match(lines[0].Trim());
    if (header.Success)
    {
        var mod = header.Groups[2].Value;
        if (!known.Contains(mod))
        {
            errors.Add($"{rel}:1: unknown module '{mod}'.");
            return null;
        }
        if (!modules.Contains(mod)) return null;
        lines.RemoveAt(0);
        firstLineNo = 2;
    }

    var rendered = RenderLines(lines, firstLineNo, modules, service, setup, rel, errors);
    if (rendered is null) return null;
    var result = Rename(string.Join('\n', rendered), setup, service);
    if (full.EndsWith(".cs", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(result)) return null;
    return result;
}

List<string>? RenderLines(List<string> lines, int firstLineNo, HashSet<string> modules, Service? service, Setup setup, string rel, List<string> errors)
{
    var output = new List<string>();
    var stack = new List<(bool Keep, int Line)>();
    for (var i = 0; i < lines.Count; i++)
    {
        var lineNo = firstLineNo + i;
        var trimmed = lines[i].Trim();

        if (eachRx.IsMatch(trimmed))
        {
            if (service is not null)
            {
                errors.Add($"{rel}:{lineNo}: @each-service is allowed only in a solution template, outside another @each-service.");
                return null;
            }
            var end = FindEnd(lines, i);
            if (end < 0)
            {
                errors.Add($"{rel}:{lineNo}: @each-service without @end at end of file.");
                return null;
            }
            if (stack.All(s => s.Keep))
            {
                var block = lines.GetRange(i + 1, end - i - 1);
                foreach (var s in setup.Services)
                {
                    var part = RenderLines(block, lineNo + 1, ServiceModules(setup, s), s, setup, rel, errors);
                    if (part is null) return null;
                    output.AddRange(part.Select(l => Rename(l, setup, s)));
                }
            }
            i = end;
            continue;
        }

        var start = startRx.Match(trimmed);
        if (start.Success)
        {
            var mod = start.Groups[3].Value;
            if (!known.Contains(mod))
            {
                errors.Add($"{rel}:{lineNo}: unknown module '{mod}'.");
                return null;
            }
            stack.Add((modules.Contains(mod) != (start.Groups[2].Value == "!"), lineNo));
            continue;
        }
        if (endRx.IsMatch(trimmed))
        {
            if (stack.Count == 0)
            {
                errors.Add($"{rel}:{lineNo}: @end without a matching @module.");
                return null;
            }
            stack.RemoveAt(stack.Count - 1);
            continue;
        }
        if (stack.All(s => s.Keep)) output.Add(lines[i]);
    }
    if (stack.Count > 0)
    {
        errors.Add($"{rel}:{stack[^1].Line}: @module without @end at end of file.");
        return null;
    }
    return output;
}

int FindEnd(List<string> lines, int start)
{
    var depth = 1;
    for (var j = start + 1; j < lines.Count; j++)
    {
        var t = lines[j].Trim();
        if (startRx.IsMatch(t) || eachRx.IsMatch(t)) depth++;
        else if (endRx.IsMatch(t) && --depth == 0) return j;
    }
    return -1;
}

string Rename(string s, Setup setup, Service? service)
{
    if (service is not null)
    {
        s = s.Replace("ServiceName", service.Name, StringComparison.Ordinal)
            .Replace("servicename", service.Name.ToLowerInvariant(), StringComparison.Ordinal)
            .Replace("ServicePort", service.Port.ToString(), StringComparison.Ordinal);
    }
    // SolutionPath/ in a root template is the path from the root folder to the solution folder.
    s = s.Replace("SolutionPath/", setup.SolutionPath == "." ? "" : setup.SolutionPath + "/", StringComparison.Ordinal);
    return s.Replace("AppName", setup.Name, StringComparison.Ordinal)
        .Replace("appname", setup.Name.Replace('.', '-').ToLowerInvariant(), StringComparison.Ordinal);
}

// A solution template sees a module as on when any service has it. "jwt" is on when a service has
// auth or trust.
HashSet<string> SolutionModules(Setup setup)
{
    var modules = setup.Modules.Concat(setup.Services.SelectMany(s => s.Modules)).ToHashSet();
    if (modules.Contains("auth") || modules.Contains("trust")) modules.Add("jwt");
    return modules;
}

HashSet<string> ServiceModules(Setup setup, Service service)
{
    var modules = setup.Modules.Concat(service.Modules).ToHashSet();
    if (modules.Contains("auth") || modules.Contains("trust")) modules.Add("jwt");
    return modules;
}

// ── Solution file and setup record ─────────────────────────────────────────────

string AddProjectsToSlnx(string slnxText, IEnumerable<string> projectPaths)
{
    var doc = XDocument.Parse(slnxText, LoadOptions.PreserveWhitespace);
    var root = doc.Root!;
    foreach (var rel in projectPaths.OrderBy(p => p, StringComparer.Ordinal))
    {
        var path = rel.Replace(Path.DirectorySeparatorChar, '/');
        var folderName = "/" + path.Split('/')[0] + "/";
        if (root.Descendants("Project").Any(p => string.Equals(((string?)p.Attribute("Path"))?.Replace('\\', '/'), path, StringComparison.OrdinalIgnoreCase)))
            continue;

        var folder = root.Elements("Folder").FirstOrDefault(f => (string?)f.Attribute("Name") == folderName);
        if (folder is null)
        {
            folder = new XElement("Folder", new XAttribute("Name", folderName));
            root.Add(folder);
        }
        var project = new XElement("Project", new XAttribute("Path", path));
        var next = folder.Elements("Project").FirstOrDefault(p => string.CompareOrdinal((string?)p.Attribute("Path"), path) > 0);
        if (next is null) folder.Add(project);
        else next.AddBeforeSelf(project);
    }
    // Reformat: the inserted elements have no whitespace of their own.
    return XDocument.Parse(doc.ToString(SaveOptions.DisableFormatting)).Root!.ToString() + "\n";
}

string SetupJson(Setup setup)
{
    var record = new JsonObject
    {
        ["kit"] = "nkryp-ai-kit",
        ["kitVersion"] = setup.KitVersion,
        ["format"] = 2,
        ["name"] = setup.Name,
        ["arch"] = setup.Arch,
        ["solutionPath"] = setup.SolutionPath,
        ["modules"] = ModuleArray(setup.Modules),
        ["services"] = new JsonArray(setup.Services.Select(s => (JsonNode)new JsonObject
        {
            ["name"] = s.Name,
            ["port"] = s.Port,
            ["modules"] = ModuleArray(s.Modules),
        }).ToArray()),
    };
    return record.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
}

JsonArray ModuleArray(List<string> modules) =>
    new(moduleOrder.Where(modules.Contains).Select(m => (JsonNode)m).ToArray());

string ModuleList(List<string> modules) =>
    modules.Count == 0 ? "none" : string.Join(", ", moduleOrder.Where(modules.Contains));

bool TryReadSetup(string path, out Setup setup, out string error)
{
    setup = null!;
    error = "";
    try
    {
        var node = JsonNode.Parse(File.ReadAllText(path));
        if (node?["format"]?.GetValue<int>() != 2)
        {
            error = $"{path} is not in format 2. Write the setup record in format 2 first.";
            return false;
        }
        setup = new Setup(
            node["name"]!.GetValue<string>(),
            node["arch"]!.GetValue<string>(),
            node["kitVersion"]?.GetValue<string>() ?? "unknown",
            node["modules"]!.AsArray().Select(m => m!.GetValue<string>()).ToList(),
            node["services"]!.AsArray().Select(s => new Service(
                s!["name"]!.GetValue<string>(),
                s["port"]!.GetValue<int>(),
                s["modules"]!.AsArray().Select(m => m!.GetValue<string>()).ToList())).ToList(),
            node["solutionPath"]?.GetValue<string>() ?? ".");
    }
    catch (Exception e) when (e is IOException or JsonException or InvalidOperationException or NullReferenceException or FormatException)
    {
        error = $"Cannot read {path}: {e.Message}";
        return false;
    }
    var errors = Validate(setup);
    error = string.Join(Environment.NewLine + "       ", errors.Select(e => $"{path}: {e}"));
    return errors.Count == 0;
}

List<string> Validate(Setup setup)
{
    var errors = new List<string>();
    if (!nameRx.IsMatch(setup.Name)) errors.Add($"Invalid name '{setup.Name}'. Use PascalCase segments separated by dots.");
    if (setup.Arch is not ("clean" or "vsa")) errors.Add($"Invalid arch '{setup.Arch}'. Use clean or vsa.");
    if (setup.SolutionPath != "." && (setup.SolutionPath.Contains('\\') || Path.IsPathRooted(setup.SolutionPath)
            || setup.SolutionPath.Split('/').Any(p => p is "" or "." or "..")))
        errors.Add($"Invalid solutionPath '{setup.SolutionPath}'. Use '.' or a relative path down from the root folder, with '/' (for example backend).");
    foreach (var m in setup.Modules.Where(m => !solutionModuleNames.Contains(m)))
        errors.Add($"'{m}' is not a solution module. Solution modules: {string.Join(", ", solutionModuleNames)}.");
    if (setup.Services.Count == 0) errors.Add("The solution needs at least one service.");
    foreach (var dup in setup.Services.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        errors.Add($"The service name '{dup.Key}' is used more than once.");
    foreach (var dup in setup.Services.GroupBy(s => s.Port).Where(g => g.Count() > 1))
        errors.Add($"The port {dup.Key} is used by more than one service.");
    var hasAuthService = setup.Services.Any(s => s.Modules.Contains("auth"));
    // All services share one signing key, issuer and audience. A second account store would issue
    // tokens that the first one accepts for its own, unrelated accounts.
    if (setup.Services.Count(s => s.Modules.Contains("auth")) > 1)
        errors.Add("Only one service may have auth. Use trust for the other services.");
    foreach (var s in setup.Services)
    {
        if (!serviceRx.IsMatch(s.Name)) errors.Add($"Invalid service name '{s.Name}'. Use one PascalCase segment, for example Admin.");
        if (s.Port is < 1 or > 65535) errors.Add($"Service {s.Name}: invalid port {s.Port}.");
        foreach (var m in s.Modules.Where(m => !serviceModuleNames.Contains(m)))
            errors.Add($"Service {s.Name}: '{m}' is not a service module. Service modules: {string.Join(", ", serviceModuleNames)}.");
        if (s.Modules.Contains("auth") && s.Modules.Contains("trust")) errors.Add($"Service {s.Name}: auth and trust exclude each other.");
        if (s.Modules.Contains("permissions") && !s.Modules.Contains("auth")) errors.Add($"Service {s.Name}: permissions needs auth.");
        if (s.Modules.Contains("signup") && !s.Modules.Contains("auth")) errors.Add($"Service {s.Name}: signup needs auth.");
        if (s.Modules.Contains("trust") && !hasAuthService) errors.Add($"Service {s.Name}: trust needs another service with auth in the solution.");
    }
    return errors;
}

static bool TryParsePort(string text, out int port) => int.TryParse(text, out port) && port is >= 1 and <= 65535;

int TemplateFail(List<string> errors, string title = "Template problems found. Nothing was written.")
{
    Console.Error.WriteLine("Error: " + title);
    foreach (var e in errors) Console.Error.WriteLine("  " + e);
    return 1;
}

static int Fail(string message)
{
    Console.Error.WriteLine("Error: " + message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 1;
}

record Service(string Name, int Port, List<string> Modules);

/// <param name="SolutionPath">The path from the folder with the root agent files to the solution folder, or ".".</param>
record Setup(string Name, string Arch, string KitVersion, List<string> Modules, List<Service> Services, string SolutionPath);
