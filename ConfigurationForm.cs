namespace Databasemanager;

public sealed class ConfigurationForm : Form
{
    private readonly TextBox serverTextBox = new();
    private readonly TextBox databaseTextBox = new();
    private readonly TextBox scriptsPathTextBox = new();
    private readonly TextBox outputDirectoryTextBox = new();
    private readonly TextBox identityTextBox = new();
    private readonly AppSettings settings;

    public ConfigurationForm(AppSettings settings)
    {
        this.settings = settings;

        Text = "Configuration";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 280);
        MinimumSize = new Size(560, 270);

        serverTextBox.Text = settings.ServerInstanceName;
        databaseTextBox.Text = settings.DefaultDatabaseName;
        scriptsPathTextBox.Text = settings.SqlScriptsPath;
        outputDirectoryTextBox.Text = settings.OutputDirectory;
        identityTextBox.Text = settings.DefaultIdentityName;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 6
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        for (int i = 0; i < 5; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddTextRow(layout, 0, "Server instance name", serverTextBox);
        AddTextRow(layout, 1, "Default database name", databaseTextBox);
        AddPathRow(layout, 2, "SQL scripts path", scriptsPathTextBox);
        AddPathRow(layout, 3, "Output directory", outputDirectoryTextBox);
        AddTextRow(layout, 4, "Default identity name", identityTextBox);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };

        var saveButton = new Button { Text = "Save", Width = 90 };
        var cancelButton = new Button { Text = "Cancel", Width = 90 };

        saveButton.Click += (_, _) => Save();
        cancelButton.Click += (_, _) => Close();

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 1, 5);
        layout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(layout);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void AddTextRow(TableLayoutPanel layout, int row, string labelText, TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(textBox, 1, row);
        layout.SetColumnSpan(textBox, 2);
    }

    private void AddPathRow(TableLayoutPanel layout, int row, string labelText, TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        var browseButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        browseButton.Click += (_, _) => BrowseForDirectory(textBox);

        layout.Controls.Add(new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(textBox, 1, row);
        layout.Controls.Add(browseButton, 2, row);
    }

    private static void BrowseForDirectory(TextBox target)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = Directory.Exists(target.Text) ? target.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            UseDescriptionForTitle = true,
            Description = "Select directory"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            target.Text = dialog.SelectedPath;
    }

    private void Save()
    {
        settings.ServerInstanceName = serverTextBox.Text.Trim();
        settings.DefaultDatabaseName = databaseTextBox.Text.Trim();
        settings.SqlScriptsPath = scriptsPathTextBox.Text.Trim();
        settings.OutputDirectory = outputDirectoryTextBox.Text.Trim();
        settings.DefaultIdentityName = identityTextBox.Text.Trim();

        ConfigurationService.Save(settings);
        DialogResult = DialogResult.OK;
        Close();
    }
}
