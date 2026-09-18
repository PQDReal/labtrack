using System.Text;
using LabTrack.Core;

namespace LabTrack.App;

public sealed class MainForm : Form
{
    private readonly SampleStore store;
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false, BackgroundColor = Color.White };
    private readonly TextBox search = new() { Width = 240, PlaceholderText = "Search code, type or location" };
    private readonly ComboBox filter = new() { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox code = new() { Dock = DockStyle.Fill, MaxLength = 40 };
    private readonly TextBox kind = new() { Dock = DockStyle.Fill, MaxLength = 60 };
    private readonly TextBox location = new() { Dock = DockStyle.Fill, MaxLength = 60 };
    private readonly TextBox notes = new() { Dock = DockStyle.Fill, Multiline = true, MaxLength = 1000, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox status = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label feedback = new() { AutoSize = true, ForeColor = Color.DarkSlateGray, Padding = new Padding(0, 8, 0, 0) };
    private readonly Label count = new() { AutoSize = true, Padding = new Padding(8) };
    private readonly Button save = new() { Text = "Save sample", AutoSize = true };
    private long selectedId;

    public MainForm(SampleStore store)
    {
        this.store = store;
        Text = "LabTrack | Sample tracker";
        Size = new Size(1120, 760); MinimumSize = new Size(980, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 247, 250);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22), ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new(SizeType.Absolute, 56));
        root.RowStyles.Add(new(SizeType.Absolute, 48));
        root.RowStyles.Add(new(SizeType.Percent, 100));
        root.RowStyles.Add(new(SizeType.Absolute, 36));
        root.RowStyles.Add(new(SizeType.Absolute, 182));
        root.RowStyles.Add(new(SizeType.Absolute, 74));
        Controls.Add(root);
        root.Controls.Add(new Label { Text = "LabTrack  /  Sample tracker", Font = new Font("Segoe UI", 21, FontStyle.Bold), AutoSize = true });
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        filter.Items.AddRange(["All", .. SampleStore.Statuses]); filter.SelectedIndex = 0;
        var export = new Button { Text = "Export CSV", AutoSize = true };
        var fresh = new Button { Text = "New sample", AutoSize = true };
        var delete = new Button { Text = "Delete selected", AutoSize = true };
        toolbar.Controls.AddRange([search, filter, fresh, delete, export]);
        root.Controls.Add(toolbar); root.Controls.Add(grid); root.Controls.Add(count);
        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3 };
        editor.ColumnStyles.Add(new(SizeType.Absolute, 95)); editor.ColumnStyles.Add(new(SizeType.Percent, 50));
        editor.ColumnStyles.Add(new(SizeType.Absolute, 95)); editor.ColumnStyles.Add(new(SizeType.Percent, 50));
        editor.RowStyles.Add(new(SizeType.Absolute, 38)); editor.RowStyles.Add(new(SizeType.Absolute, 38)); editor.RowStyles.Add(new(SizeType.Percent, 100));
        void Field(string title, Control control, int column, int row) { editor.Controls.Add(new Label { Text = title, AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, column, row); editor.Controls.Add(control, column + 1, row); }
        Field("Code *", code, 0, 0); Field("Type *", kind, 2, 0);
        Field("Location *", location, 0, 1); Field("Status *", status, 2, 1);
        Field("Notes", notes, 0, 2); editor.SetColumnSpan(notes, 3);
        status.Items.AddRange(SampleStore.Statuses); status.SelectedIndex = 0;
        root.Controls.Add(editor);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        footer.Controls.Add(save); footer.Controls.Add(feedback); root.Controls.Add(footer);
        search.TextChanged += (_, _) => Guard(RefreshGrid);
        filter.SelectedIndexChanged += (_, _) => Guard(RefreshGrid);
        grid.SelectionChanged += (_, _) => LoadSelected();
        fresh.Click += (_, _) => ClearEditor();
        save.Click += (_, _) => Guard(SaveSample);
        delete.Click += (_, _) => Guard(() => {
            if (selectedId == 0) throw new ArgumentException("Select a sample first.");
            if (MessageBox.Show(this, "Delete sample " + code.Text + "? This cannot be undone.", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            DeleteSelected();
        });
        export.Click += (_, _) => Guard(() => {
            using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "labtrack-samples.csv" };
            if (dialog.ShowDialog(this) == DialogResult.OK) { Export(dialog.FileName); feedback.Text = "Exported the currently filtered samples."; }
        });
        RefreshGrid(); ClearEditor();
    }

    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
        { feedback.Text = exception.Message; }
    }
    private void RefreshGrid()
    {
        var rows = store.List(search.Text, filter.Text);
        grid.DataSource = rows;
        grid.Columns[nameof(Sample.Id)].Visible = false;
        count.Text = $"{rows.Count} samples shown  |  {rows.Count(s => s.Status == "Completed")} completed";
        ClearEditor();
    }
    private void ClearEditor() { grid.ClearSelection(); selectedId = 0; code.Clear(); kind.Clear(); location.Clear(); notes.Clear(); status.SelectedIndex = 0; feedback.Text = "New sample: fill the required fields (*) and save."; }
    private void LoadSelected()
    {
        if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].DataBoundItem is not Sample sample) return;
        selectedId = sample.Id; code.Text = sample.Code; kind.Text = sample.Kind; location.Text = sample.Location; status.Text = sample.Status; notes.Text = sample.Notes;
        feedback.Text = "Editing " + sample.Code;
    }
    private void SaveSample() { var created = selectedId == 0; store.Save(new(selectedId, code.Text, kind.Text, location.Text, status.Text, notes.Text)); RefreshGrid(); feedback.Text = created ? "Sample added." : "Sample updated."; }
    private void DeleteSelected() { store.Delete(selectedId); RefreshGrid(); feedback.Text = "Sample deleted."; }
    private void Export(string path) => File.WriteAllText(path, SampleStore.Csv(store.List(search.Text, filter.Text)), new UTF8Encoding(true));

    // Exercises the real form controls against a disposable database, including the published EXE.
    internal void SmokeTest(string folder)
    {
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        save.PerformClick(); Require(feedback.Text.Contains("required"), "Empty form validation failed.");
        code.Text = "DEMO-001"; kind.Text = "Water"; location.Text = "Shelf A"; notes.Text = "Demo sample - no patient data";
        save.PerformClick(); Require(store.List().Count == 1, "Add failed.");
        grid.Rows[0].Selected = true; LoadSelected(); status.Text = "Completed"; save.PerformClick();
        Require(store.List()[0].Status == "Completed", "Update failed.");
        search.Text = "not-found"; Require(grid.Rows.Count == 0, "Search failed."); search.Clear();
        filter.Text = "Received"; Require(grid.Rows.Count == 0, "Status filter failed."); filter.Text = "All";
        code.Text = "DEMO-002"; kind.Text = "Buffer"; location.Text = "Shelf B"; save.PerformClick();
        Export(Path.Combine(folder, "samples.csv")); Require(File.ReadAllText(Path.Combine(folder, "samples.csv")).Contains("DEMO-002"), "Export failed.");
        Refresh(); Application.DoEvents();
        using (var bitmap = new Bitmap(Width, Height)) { DrawToBitmap(bitmap, new Rectangle(0, 0, Width, Height)); bitmap.Save(Path.Combine(folder, "app.png")); }
        grid.Rows[0].Selected = true; LoadSelected(); DeleteSelected();
        Require(store.List().Count == 1, "Delete failed.");
    }
}
