using LabTrack.Core;

namespace LabTrack.App;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var smoke = args.Length == 2 && args[0] == "--smoke-test";
        var folder = smoke ? Path.GetFullPath(args[1]) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabTrack");
        Directory.CreateDirectory(folder);
        try
        {
            using var form = new MainForm(new SampleStore(Path.Combine(folder, "samples.db")));
            if (smoke)
            {
                form.Shown += (_, _) => form.BeginInvoke(() =>
                {
                    try { form.SmokeTest(folder); File.WriteAllText(Path.Combine(folder, "PASS.txt"), "WinForms add, edit, filter, render, export, delete and validation passed."); }
                    catch (Exception exception) { File.WriteAllText(Path.Combine(folder, "FAIL.txt"), exception.ToString()); Environment.ExitCode = 1; }
                    finally { form.Close(); }
                });
            }
            Application.Run(form);
            return Environment.ExitCode;
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(folder, "error.log"), exception.ToString());
            if (!smoke) MessageBox.Show("LabTrack could not start. Details were saved to " + Path.Combine(folder, "error.log"), "LabTrack", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
