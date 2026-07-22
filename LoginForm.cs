namespace Databasemanager;

public sealed class LoginForm : Form
{
    private readonly TextBox usernameTextBox = new();
    private readonly TextBox passwordTextBox = new();
    private readonly Label messageLabel = new();

    public LoginForm()
    {
        Text = "Databasemanager Login";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 210);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 5,
            ColumnCount = 2
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        usernameTextBox.Dock = DockStyle.Fill;
        passwordTextBox.Dock = DockStyle.Fill;
        passwordTextBox.UseSystemPasswordChar = true;

        usernameTextBox.Text = "root";
        passwordTextBox.Text = "pepper";

        var loginButton = new Button { Text = "Log in", Dock = DockStyle.Right, Width = 90 };
        loginButton.Click += (_, _) => TryLogin();

        messageLabel.AutoSize = true;
        messageLabel.ForeColor = Color.Firebrick;

        layout.Controls.Add(new Label { Text = "Username", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(usernameTextBox, 1, 0);
        layout.Controls.Add(new Label { Text = "Password", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(passwordTextBox, 1, 1);
        layout.Controls.Add(messageLabel, 1, 2);
        layout.Controls.Add(loginButton, 1, 3);

        Controls.Add(layout);
        AcceptButton = loginButton;
        Shown += (_, _) => passwordTextBox.Focus();

        /*
         * Shown += LoginForm_Shown;
        private void LoginForm_Shown(object? sender, EventArgs e)
        {
            passwordTextBox.Focus();
        }
        */
    }

    private void TryLogin()
    {
        if (usernameTextBox.Text == "root" && passwordTextBox.Text == "pepper")
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        messageLabel.Text = "Invalid username or password.";
        passwordTextBox.SelectAll();
        passwordTextBox.Focus();
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
        SuspendLayout();
        // 
        // LoginForm
        // 
        ClientSize = new Size(284, 261);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "LoginForm";
        ResumeLayout(false);

    }
}
