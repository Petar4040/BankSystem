namespace BankQueueWinForms;

public class CustomerForm : Form
{
    readonly QueueService _qs;
    int _currentTicketId = -1;

    readonly TextBox  txtName        = new();
    readonly ComboBox cmbService     = new();
    readonly Panel    pnlTicket      = new();
    readonly Label    lblTicketNum   = new();
    readonly Label    lblTicketInfo  = new();
    readonly Panel    pnlNotif       = new();
    readonly ListBox  lstNotif       = new();
    readonly TextBox  txtCheckId     = new();
    readonly Label    lblCheckResult = new();
    readonly System.Windows.Forms.Timer _timer = new();

    static readonly Color Blue     = Color.FromArgb(26, 115, 232);
    static readonly Color DarkBg   = Color.FromArgb(26, 29, 35);
    static readonly Color TicketBg = Color.FromArgb(232, 240, 253);
    static readonly Color NotifBg  = Color.FromArgb(255, 243, 224);

    public CustomerForm(QueueService qs)
    {
        _qs = qs;
        BuildUI();
        _timer.Interval = 4000;
        _timer.Tick += (_, _) => { if (_currentTicketId >= 0) RefreshStatus(); };
        _timer.Start();
    }

    void BuildUI()
    {
        Text            = "Bank Queue — Customer";
        Size            = new Size(460, 580);
        MinimumSize     = new Size(460, 580);
        MaximumSize     = new Size(460, 580);
        BackColor       = Color.White;
        Font            = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;

        var header  = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = DarkBg };
        var lblHead = new Label {
            Text = "🏦  Bank Queue System",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0)
        };
        header.Controls.Add(lblHead);

        var grpJoin = MakeGroup("Join a Queue", 12, 62, 428, 142);

        var lblName = MakeLabel("Name:", 10, 26, 70);
        txtName.SetBounds(86, 24, 320, 26); txtName.Font = new Font("Segoe UI", 10f);

        var lblSvc = MakeLabel("Service:", 10, 62, 70);
        cmbService.SetBounds(86, 59, 210, 26);
        cmbService.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbService.Font          = new Font("Segoe UI", 10f);
        cmbService.Items.AddRange(new[] { "Cash Transaction", "Account Services", "Loan Consultation" });
        cmbService.SelectedIndex = 0;

        var btnJoin = MakeBlueButton("Join Queue", 86, 97, 130, 32);
        btnJoin.Click += BtnJoin_Click;

        grpJoin.Controls.AddRange(new Control[] { lblName, txtName, lblSvc, cmbService, btnJoin });

        pnlTicket.SetBounds(12, 214, 428, 148);
        pnlTicket.BackColor   = TicketBg;
        pnlTicket.BorderStyle = BorderStyle.FixedSingle;
        pnlTicket.Visible     = false;

        lblTicketNum.SetBounds(0, 8, 428, 72);
        lblTicketNum.Font      = new Font("Segoe UI", 46, FontStyle.Bold);
        lblTicketNum.ForeColor = Blue;
        lblTicketNum.TextAlign = ContentAlignment.MiddleCenter;

        lblTicketInfo.SetBounds(8, 82, 412, 36);
        lblTicketInfo.Font      = new Font("Segoe UI", 9f);
        lblTicketInfo.ForeColor = Color.FromArgb(60, 60, 60);
        lblTicketInfo.TextAlign = ContentAlignment.MiddleCenter;

        var btnOpen = MakeBlueButton("Open HTML Ticket", 144, 118, 140, 26);
        btnOpen.Font  = new Font("Segoe UI", 8.5f);
        btnOpen.Click += BtnOpenTicket_Click;

        pnlTicket.Controls.AddRange(new Control[] { lblTicketNum, lblTicketInfo, btnOpen });

        pnlNotif.SetBounds(12, 370, 428, 72);
        pnlNotif.BackColor   = NotifBg;
        pnlNotif.BorderStyle = BorderStyle.FixedSingle;
        pnlNotif.Visible     = false;

        var lblNotifHdr = new Label {
            Text      = "🔔  Notifications",
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(230, 81, 0),
            Bounds    = new Rectangle(8, 5, 410, 20)
        };
        lstNotif.SetBounds(8, 26, 410, 40);
        lstNotif.BorderStyle = BorderStyle.None;
        lstNotif.BackColor   = NotifBg;
        lstNotif.Font        = new Font("Segoe UI", 9f);
        pnlNotif.Controls.AddRange(new Control[] { lblNotifHdr, lstNotif });

        var grpCheck = MakeGroup("Check My Position", 12, 450, 428, 112);

        var lblChk = MakeLabel("Ticket #:", 10, 28, 72);
        txtCheckId.SetBounds(88, 25, 100, 26); txtCheckId.Font = new Font("Segoe UI", 10f);

        var btnCheck = MakeBlueButton("Check", 198, 24, 80, 28);
        btnCheck.Click += BtnCheck_Click;

        lblCheckResult.SetBounds(10, 62, 408, 42);
        lblCheckResult.Font      = new Font("Segoe UI", 9f);
        lblCheckResult.ForeColor = Color.FromArgb(80, 80, 80);
        lblCheckResult.Text      = "Enter your ticket number above to check your position.";

        grpCheck.Controls.AddRange(new Control[] { lblChk, txtCheckId, btnCheck, lblCheckResult });

        Controls.AddRange(new Control[] { header, grpJoin, pnlTicket, pnlNotif, grpCheck });
    }


    void BtnJoin_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text)) {
            MessageBox.Show("Please enter your name.", "Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var svc    = (ServiceType)(cmbService.SelectedIndex + 1);
        var result = _qs.JoinQueue(txtName.Text.Trim(), svc);
        _currentTicketId = result.id;

        lblTicketNum.Text  = $"#{result.id}";
        lblTicketInfo.Text = $"{result.service}  ·  {result.counter}  ·  Position {result.position}  ·  ~{result.wait} min";
        pnlTicket.Visible  = true;
        txtCheckId.Text = result.id.ToString();
        lblCheckResult.Text = $"Ticket #{result.id}  [Waiting]  —  Position {result.position}  ·  Est. wait ~{result.wait} min";
        txtName.Clear();
    }

    void BtnOpenTicket_Click(object? sender, EventArgs e)
    {
        if (_currentTicketId < 0) return;
        string path = _qs.GetTicketPath(_currentTicketId);
        if (File.Exists(path))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        else
            MessageBox.Show("Ticket file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    void BtnCheck_Click(object? sender, EventArgs e)
    {
        if (!int.TryParse(txtCheckId.Text.Trim(), out int id)) {
            MessageBox.Show("Enter a valid ticket number.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _currentTicketId = id;
        RefreshStatus();
    }

    void RefreshStatus()
    {
        var (status, pos, wait, notifications) = _qs.GetStatus(_currentTicketId);

        if (status == "NotFound") {
            lblCheckResult.Text      = $"Ticket #{_currentTicketId} not found.";
            lblCheckResult.ForeColor = Color.Gray;
            return;
        }

        if (notifications.Count > 0) {
            foreach (var notification in notifications)
                lstNotif.Items.Insert(0, notification);
            pnlNotif.Visible = true;
        }

        lblCheckResult.ForeColor = status == "Called" ? Color.FromArgb(27, 94, 32) : Color.FromArgb(60, 60, 60);
        lblCheckResult.Text = status switch {
            "Called"  => $"Ticket #{_currentTicketId}  [Called]  →  Please proceed to the counter NOW!",
            "Done"    => $"Ticket #{_currentTicketId}  [Done]  —  Service complete. Thank you!",
            "Skipped" => $"Ticket #{_currentTicketId}  [Skipped]",
            _         => $"Ticket #{_currentTicketId}  [Waiting]  —  Position {pos}  ·  Est. wait ~{wait} min"
        };
    }


    static GroupBox MakeGroup(string title, int x, int y, int w, int h) =>
        new() { Text = title, Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

    static Label MakeLabel(string text, int x, int y, int w) =>
        new() { Text = text, Bounds = new Rectangle(x, y, w, 24), TextAlign = ContentAlignment.MiddleRight };

    static Button MakeBlueButton(string text, int x, int y, int w, int h)
    {
        var btn = new Button {
            Text = text, Bounds = new Rectangle(x, y, w, h),
            BackColor = Color.FromArgb(26, 115, 232), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}

