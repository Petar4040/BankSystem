namespace BankQueueWinForms;

public class AdminForm : Form
{
    readonly QueueService _qs;

    readonly ListBox lstCash    = new();
    readonly ListBox lstAccount = new();
    readonly ListBox lstLoan    = new();

    readonly Label lblCashInfo    = new();
    readonly Label lblAccountInfo = new();
    readonly Label lblLoanInfo    = new();

    readonly TextBox  txtDoneId   = new();
    readonly TextBox  txtSkipId   = new();
    readonly Label    lblActionMsg = new();

    readonly Label lblTotal   = new();
    readonly Label lblWaiting = new();
    readonly Label lblServed  = new();
    readonly Label lblSkipped = new();

    readonly Label lblCallResult = new();

    readonly System.Windows.Forms.Timer _timer = new();

    static readonly Color Blue   = Color.FromArgb(26, 115, 232);
    static readonly Color DarkBg = Color.FromArgb(26, 29, 35);
    static readonly Color Green  = Color.FromArgb(27, 94, 32);
    static readonly Color Orange = Color.FromArgb(230, 81, 0);

    public AdminForm(QueueService qs)
    {
        _qs = qs;
        BuildUI();
        _timer.Interval = 3000;
        _timer.Tick += (_, _) => RefreshAll();
        _timer.Start();
        RefreshAll();
    }

    void BuildUI()
    {
        Text            = "Bank Queue — Admin Panel";
        Size            = new Size(560, 680);
        MinimumSize     = new Size(560, 680);
        MaximumSize     = new Size(560, 680);
        BackColor       = Color.White;
        Font            = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;

        var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = DarkBg };
        header.Controls.Add(new Label {
            Text = "🔧  Admin Panel",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.White, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0)
        });

        var grpQueues = MakeGroup("Live Queues", 12, 62, 526, 326);

        BuildCounterColumn(grpQueues, ServiceType.Cash,
            "Counter A — Cash", lstCash, lblCashInfo, 10, 22, 160);
        BuildCounterColumn(grpQueues, ServiceType.Account,
            "Counter B — Account", lstAccount, lblAccountInfo, 183, 22, 160);
        BuildCounterColumn(grpQueues, ServiceType.Loan,
            "Counter C — Loan", lstLoan, lblLoanInfo, 356, 22, 160);

        lblCallResult.SetBounds(10, 295, 506, 24);
        lblCallResult.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
        lblCallResult.ForeColor = Green;
        grpQueues.Controls.Add(lblCallResult);

        var grpMgmt = MakeGroup("Manage Tickets", 12, 396, 526, 116);

        grpMgmt.Controls.Add(MakeLabel("Mark Done:", 10, 30, 84));
        txtDoneId.SetBounds(100, 28, 80, 26); txtDoneId.Font = new Font("Segoe UI", 10f);
        var btnDone = MakeBlueButton("Done", 190, 27, 90, 28, Color.FromArgb(46, 125, 50));
        btnDone.Click += BtnDone_Click;

        grpMgmt.Controls.Add(MakeLabel("Skip Ticket:", 294, 30, 86));
        txtSkipId.SetBounds(386, 28, 60, 26); txtSkipId.Font = new Font("Segoe UI", 10f);
        var btnSkip = MakeBlueButton("Skip", 454, 27, 62, 28, Color.FromArgb(198, 40, 40));
        btnSkip.Click += BtnSkip_Click;

        lblActionMsg.SetBounds(10, 68, 506, 38);
        lblActionMsg.Font      = new Font("Segoe UI", 9f);
        lblActionMsg.ForeColor = Color.FromArgb(60, 60, 60);

        grpMgmt.Controls.AddRange(new Control[] { txtDoneId, btnDone, txtSkipId, btnSkip, lblActionMsg });

        var grpAnalytics = MakeGroup("Analytics", 12, 520, 526, 112);

        var statLabels = new[] { "Total Issued", "Currently Waiting", "Served", "Skipped" };
        var statValues = new[] { lblTotal, lblWaiting, lblServed, lblSkipped };
        for (int i = 0; i < 4; i++)
        {
            int x = 10 + i * 128;
            var lbl = new Label {
                Text = statLabels[i], Bounds = new Rectangle(x, 22, 120, 18),
                Font = new Font("Segoe UI", 8f), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter
            };
            statValues[i].SetBounds(x, 42, 120, 42);
            statValues[i].Font      = new Font("Segoe UI", 26f, FontStyle.Bold);
            statValues[i].ForeColor = Blue;
            statValues[i].TextAlign = ContentAlignment.MiddleCenter;
            statValues[i].Text      = "0";
            grpAnalytics.Controls.Add(lbl);
            grpAnalytics.Controls.Add(statValues[i]);
        }

        Controls.AddRange(new Control[] { header, grpQueues, grpMgmt, grpAnalytics });
    }

    void BuildCounterColumn(GroupBox parent, ServiceType svc,
        string title, ListBox lst, Label infoLbl, int x, int y, int w)
    {
        parent.Controls.Add(new Label {
            Text = title, Bounds = new Rectangle(x, y, w, 20),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Blue
        });

        lst.SetBounds(x, y + 22, w, 170);
        lst.BorderStyle = BorderStyle.FixedSingle;
        lst.Font        = new Font("Segoe UI", 9f);
        lst.SelectedIndexChanged += (_, _) => {
            if (lst.SelectedItem is QueueDisplayItem item) txtSkipId.Text = item.Id.ToString();
        };
        parent.Controls.Add(lst);

        infoLbl.SetBounds(x, y + 196, w, 32);
        infoLbl.Font      = new Font("Segoe UI", 8f);
        infoLbl.ForeColor = Color.Gray;
        parent.Controls.Add(infoLbl);

        var btn = MakeBlueButton($"Call Next", x, y + 232, w, 30, Blue);
        btn.Tag    = svc;
        btn.Click += BtnCallNext_Click;
        parent.Controls.Add(btn);
    }


    void BtnCallNext_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: ServiceType svc }) return;
        var result = _qs.CallNext(svc);
        if (result.success)
        {
            lblCallResult.ForeColor = Green;
            txtDoneId.Text = result.ticketId.ToString();
            lblCallResult.Text      = $"📢  Called: Ticket #{result.ticketId} — {result.name} → {result.counter}";
            ShowAction($"Ticket #{result.ticketId} is ready to be marked done after service.", true);
            if (result.approaching != null)
                lblCallResult.Text += $"   |   Next: {result.approaching}";
        }
        else
        {
            lblCallResult.ForeColor = Color.Gray;
            lblCallResult.Text      = $"No customers waiting at {Labels.Counter(svc)}.";
        }
        RefreshAll();
    }

    void BtnDone_Click(object? sender, EventArgs e)
    {
        if (!int.TryParse(txtDoneId.Text.Trim(), out int id)) {
            ShowAction("Enter a valid ticket number.", false); return;
        }
        var (ok, msg) = _qs.MarkDone(id);
        ShowAction(msg, ok);
        txtDoneId.Clear();
        RefreshAll();
    }

    void BtnSkip_Click(object? sender, EventArgs e)
    {
        if (!int.TryParse(txtSkipId.Text.Trim(), out int id)) {
            ShowAction("Enter a valid ticket number.", false); return;
        }
        var (ok, msg) = _qs.SkipTicket(id);
        ShowAction(msg, ok);
        txtSkipId.Clear();
        RefreshAll();
    }

    void ShowAction(string msg, bool success)
    {
        lblActionMsg.ForeColor = success ? Green : Orange;
        lblActionMsg.Text      = msg;
    }


    void RefreshAll()
    {
        RefreshCounter(ServiceType.Cash,    lstCash,    lblCashInfo);
        RefreshCounter(ServiceType.Account, lstAccount, lblAccountInfo);
        RefreshCounter(ServiceType.Loan,    lstLoan,    lblLoanInfo);
        RefreshAnalytics();
    }

    void RefreshCounter(ServiceType svc, ListBox lst, Label info)
    {
        var entries = _qs.GetQueue(svc);
        lst.Items.Clear();
        if (entries.Count == 0)
            lst.Items.Add("(empty)");
        else
            entries.ForEach(e => lst.Items.Add(new QueueDisplayItem(e.id, e.name, e.estWait)));

        int count = _qs.QueueCount(svc), avg = _qs.AvgFor(svc);
        info.Text = $"{count} waiting  ·  avg ~{avg} min  ·  clear ~{count * avg} min";
    }

    void RefreshAnalytics()
    {
        var (total, waiting, called, served, skipped) = _qs.GetAnalytics();
        lblTotal.Text   = total.ToString();
        lblWaiting.Text = (waiting + called).ToString();
        lblServed.Text  = served.ToString();
        lblSkipped.Text = skipped.ToString();
    }


    static GroupBox MakeGroup(string title, int x, int y, int w, int h) =>
        new() { Text = title, Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

    static Label MakeLabel(string text, int x, int y, int w) =>
        new() { Text = text, Bounds = new Rectangle(x, y, w, 26), TextAlign = ContentAlignment.MiddleRight };

    sealed class QueueDisplayItem
    {
        public QueueDisplayItem(int id, string name, int estWait)
        {
            Id = id;
            Name = name;
            EstWait = estWait;
        }

        public int Id { get; }
        public string Name { get; }
        public int EstWait { get; }

        public override string ToString() => $"#{Id}  {Name}  (~{EstWait} min)";
    }

    static Button MakeBlueButton(string text, int x, int y, int w, int h, Color color)
    {
        var btn = new Button {
            Text = text, Bounds = new Rectangle(x, y, w, h),
            BackColor = color, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}


