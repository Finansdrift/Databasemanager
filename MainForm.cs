namespace Databasemanager;

public sealed class MainForm : Form
{
    private readonly AppSettings settings;
    private readonly SqlServerService sqlServerService = new();
    private readonly ScriptActionService scriptActionService = new();
    private readonly ComboBox serverComboBox = new();
    private readonly TextBox databaseTextBox = new();
    private readonly TextBox identityTextBox = new();
    private readonly TextBox scriptsPathTextBox = new();
    private readonly TextBox outputDirectoryTextBox = new();
    private readonly ListView databaseListView = new();
    private readonly TextBox logTextBox = new();
    private readonly Button restoreButton = new();
    private readonly Button refreshButton = new();
    private readonly Button configButton = new();

    public MainForm()
    {
        settings = ConfigurationService.Load();

        Text = "Databasemanager";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1100;
        Height = 720;
        MinimumSize = new Size(900, 560);

        BuildLayout();
        ApplySettingsToControls();
        Load += async (_, _) => await RefreshDatabasesAsync();
        FormClosing += (_, _) => PersistTopBarToSettings();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildTopBar(), 0, 0);
        root.Controls.Add(BuildContent(), 0, 1);
        Controls.Add(root);
    }

    private Control BuildTopBar()
    {
        var topBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 10,
            RowCount = 2
        };

        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        topBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        topBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

        serverComboBox.Dock = DockStyle.Fill;
        serverComboBox.DropDownStyle = ComboBoxStyle.DropDown;

        databaseTextBox.Dock = DockStyle.Fill;
        identityTextBox.Dock = DockStyle.Fill;
        scriptsPathTextBox.Dock = DockStyle.Fill;
        outputDirectoryTextBox.Dock = DockStyle.Fill;
        databaseTextBox.TextChanged += (_, _) => UpdateRestoreButtonState();

        restoreButton.Text = "Restore";
        restoreButton.Dock = DockStyle.Fill;
        restoreButton.Click += async (_, _) => await ExecuteMissingDatabaseRestoreAsync();

        refreshButton.Text = "Refresh";
        refreshButton.Dock = DockStyle.Fill;
        refreshButton.Click += async (_, _) => await RefreshDatabasesAsync();

        configButton.Text = "Configuration";
        configButton.Dock = DockStyle.Fill;
        configButton.Click += (_, _) => OpenConfiguration();

        AddLabel(topBar, "Server instance", 0, 0);
        topBar.Controls.Add(serverComboBox, 1, 0);
        AddLabel(topBar, "Default database", 2, 0);
        topBar.Controls.Add(databaseTextBox, 3, 0);
        AddLabel(topBar, "Identity", 4, 0);
        topBar.Controls.Add(identityTextBox, 5, 0);
        topBar.Controls.Add(restoreButton, 6, 0);
        topBar.Controls.Add(refreshButton, 7, 0);
        topBar.Controls.Add(configButton, 8, 0);

        AddLabel(topBar, "SQL scripts", 0, 1);
        topBar.Controls.Add(scriptsPathTextBox, 1, 1);
        topBar.SetColumnSpan(scriptsPathTextBox, 3);
        topBar.Controls.Add(MakeBrowseButton(scriptsPathTextBox), 4, 1);
        AddLabel(topBar, "Output", 5, 1);
        topBar.Controls.Add(outputDirectoryTextBox, 6, 1);
        topBar.SetColumnSpan(outputDirectoryTextBox, 3);
        topBar.Controls.Add(MakeBrowseButton(outputDirectoryTextBox), 9, 1);

        return topBar;
    }

    private Control BuildContent()
    {
        var splitter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 340,
            FixedPanel = FixedPanel.None
        };

        databaseListView.Dock = DockStyle.Fill;
        databaseListView.View = View.Details;
        databaseListView.FullRowSelect = true;
        databaseListView.MultiSelect = false;
        databaseListView.Columns.Add("Database", 300);
        databaseListView.ContextMenuStrip = BuildDatabaseContextMenu();
        databaseListView.DoubleClick += async (_, _) => await ExecuteSelectedActionAsync(DatabaseAction.BackupDatabase);

        logTextBox.Dock = DockStyle.Fill;
        logTextBox.Multiline = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.ReadOnly = true;
        logTextBox.Font = new Font(FontFamily.GenericMonospace, 9);

        splitter.Panel1.Controls.Add(databaseListView);
        splitter.Panel2.Controls.Add(logTextBox);

        return splitter;
    }

    private ContextMenuStrip BuildDatabaseContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Backup database", null, async (_, _) => await ExecuteSelectedActionAsync(DatabaseAction.BackupDatabase));
        menu.Items.Add("Restore database", null, async (_, _) => await ExecuteSelectedActionAsync(DatabaseAction.RestoreDatabase));
        menu.Items.Add("Set identity", null, async (_, _) => await ExecuteSelectedActionAsync(DatabaseAction.SetIdentity));
        menu.Items.Add("Delete database", null, async (_, _) => await ExecuteSelectedActionAsync(DatabaseAction.DeleteDatabase));
        menu.Opening += (sender, e) => e.Cancel = databaseListView.SelectedItems.Count == 0;
        return menu;
    }

    private static void AddLabel(TableLayoutPanel layout, string text, int column, int row)
    {
        layout.Controls.Add(new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left }, column, row);
    }

    private static Button MakeBrowseButton(TextBox target)
    {
        var button = new Button { Text = "Browse", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = Directory.Exists(target.Text) ? target.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                UseDescriptionForTitle = true,
                Description = "Select directory"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                target.Text = dialog.SelectedPath;
        };

        return button;
    }

    private void ApplySettingsToControls()
    {
        serverComboBox.Items.Clear();
        foreach (string server in sqlServerService.GetLikelyServerInstances(settings.ServerInstanceName))
            serverComboBox.Items.Add(server);

        serverComboBox.Text = settings.ServerInstanceName;
        databaseTextBox.Text = settings.DefaultDatabaseName;
        identityTextBox.Text = settings.DefaultIdentityName;
        scriptsPathTextBox.Text = settings.SqlScriptsPath;
        outputDirectoryTextBox.Text = settings.OutputDirectory;
    }

    private async Task RefreshDatabasesAsync()
    {
        SetBusy(true);
        databaseListView.Items.Clear();

        try
        {
            string server = serverComboBox.Text.Trim();
            AppendLog($"Loading databases from {server}...");
            var databases = await sqlServerService.GetDatabasesAsync(server);

            foreach (string database in databases)
                databaseListView.Items.Add(new ListViewItem(database));

            AppendLog($"Loaded {databases.Count} database(s).");
            UpdateRestoreButtonState();
        }
        catch (Exception ex)
        {
            AppendLog("Failed to load databases: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Database load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateRestoreButtonState();
            SetBusy(false);
        }
    }

    private async Task ExecuteSelectedActionAsync(DatabaseAction action)
    {
        if (databaseListView.SelectedItems.Count == 0)
            return;

        string databaseName = databaseListView.SelectedItems[0].Text;
        string displayName = ScriptActionService.GetDisplayName(action);

        var result = MessageBox.Show(
            this,
            $"{displayName} for '{databaseName}'?",
            displayName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        SetBusy(true);
        var progress = new Progress<string>(AppendLog);

        try
        {
            PersistTopBarToSettings();
            await scriptActionService.ExecuteAsync(
                action,
                settings,
                serverComboBox.Text.Trim(),
                databaseTextBox.Text.Trim(),
                identityTextBox.Text.Trim(),
                databaseName,
                scriptsPathTextBox.Text.Trim(),
                outputDirectoryTextBox.Text.Trim(),
                progress);
            if (action is DatabaseAction.DeleteDatabase or DatabaseAction.RestoreDatabase) await RefreshDatabasesAsync();
        }
        catch (Exception ex)
        {
            AppendLog(displayName + " failed: " + ex.Message);
            MessageBox.Show(this, ex.Message, displayName + " failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ExecuteMissingDatabaseRestoreAsync()
    {
        string databaseName = databaseTextBox.Text.Trim();
        string identityName = identityTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            MessageBox.Show(this, "Enter the database name to restore.", "Restore", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (IsDatabaseListed(databaseName))
        {
            MessageBox.Show(
                this,
                $"'{databaseName}' is already listed. Use the database list context menu for listed databases.",
                "Restore",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            UpdateRestoreButtonState();
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Run Restore.sql for missing database '{databaseName}'?",
            "Restore",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        SetBusy(true);
        var progress = new Progress<string>(AppendLog);

        try
        {
            PersistTopBarToSettings();
            await scriptActionService.ExecuteRestoreSqlAsync(
                settings,
                serverComboBox.Text.Trim(),
                databaseName,
                identityName,
                scriptsPathTextBox.Text.Trim(),
                outputDirectoryTextBox.Text.Trim(),
                progress);

            await RefreshDatabasesAsync();
        }
        catch (Exception ex)
        {
            AppendLog("Restore failed: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Restore failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            UpdateRestoreButtonState();
        }
    }

    private void OpenConfiguration()
    {
        PersistTopBarToSettings();

        using var configurationForm = new ConfigurationForm(settings);
        if (configurationForm.ShowDialog(this) == DialogResult.OK)
        {
            ApplySettingsToControls();
            AppendLog($"Configuration saved to {ConfigurationService.SettingsFilePath}.");
        }
    }

    private void PersistTopBarToSettings()
    {
        settings.ServerInstanceName = serverComboBox.Text.Trim();
        settings.DefaultDatabaseName = databaseTextBox.Text.Trim();
        settings.DefaultIdentityName = identityTextBox.Text.Trim();
        settings.SqlScriptsPath = scriptsPathTextBox.Text.Trim();
        settings.OutputDirectory = outputDirectoryTextBox.Text.Trim();
        ConfigurationService.Save(settings);
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        restoreButton.Enabled = !busy && ShouldEnableRestoreButton();
        refreshButton.Enabled = !busy;
        configButton.Enabled = !busy;
        databaseListView.Enabled = !busy;
    }

    private void UpdateRestoreButtonState()
    {
        restoreButton.Enabled = ShouldEnableRestoreButton();
    }

    private bool ShouldEnableRestoreButton()
    {
        string databaseName = databaseTextBox.Text.Trim();
        return !string.IsNullOrWhiteSpace(databaseName) && !IsDatabaseListed(databaseName);
    }

    private bool IsDatabaseListed(string databaseName)
    {
        foreach (ListViewItem item in databaseListView.Items)
        {
            if (item.Text.Equals(databaseName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        SuspendLayout();
        // 
        // MainForm
        // 
        ClientSize = new Size(284, 261);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "MainForm";
        ResumeLayout(false);

    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(AppendLog), message);
            return;
        }

        logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}
