namespace AppName.ServiceName.Api.IntegrationTests.Infrastructure;

/// <summary>
/// One temporary directory tree per test host. Every file that the API writes during a test goes here,
/// so nothing touches the machine.
/// </summary>
public sealed class TestPaths
{
    private TestPaths(string root)
    {
        Root = root;
        DataDirectory = Path.Combine(root, "data");
    }

    public string Root { get; }

    /// <summary>The data files of the API, for example a key file or a database file.</summary>
    public string DataDirectory { get; }

    public static TestPaths Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "appname-tests", Guid.NewGuid().ToString("N"));
        var paths = new TestPaths(root);

        Directory.CreateDirectory(paths.DataDirectory);

        return paths;
    }

    public void Delete()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // The host may still hold a file open when it shuts down.
        }
    }
}
