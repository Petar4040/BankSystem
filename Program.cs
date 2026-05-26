namespace BankQueueWinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var qs           = new QueueService();
        var customerForm = new CustomerForm(qs);
        var adminForm    = new AdminForm(qs);

        customerForm.StartPosition = FormStartPosition.Manual;
        adminForm.StartPosition    = FormStartPosition.Manual;
        customerForm.Location      = new Point(20, 60);
        adminForm.Location         = new Point(490, 60);

        adminForm.Show();
        Application.Run(customerForm); // app exits when customer form is closed
    }
}
