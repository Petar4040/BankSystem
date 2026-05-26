using System.Text.Json;

namespace BankQueueWinForms;

public class QueueService
{
    static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "AppData");
    static readonly string TicketDirectory = Path.Combine(DataDirectory, "Tickets");
    static readonly string DataFile = Path.Combine(DataDirectory, "customers.json");
    static readonly string NotifFile = Path.Combine(DataDirectory, "notifications.json");
    const int    DefaultMins = 5;
    static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    readonly List<Customer>                      _customers = new();
    readonly Dictionary<ServiceType, Queue<int>> _queues    = new() {
        [ServiceType.Cash]    = new(),
        [ServiceType.Account] = new(),
        [ServiceType.Loan]    = new()
    };
    Dictionary<int, List<string>> _notifications = new();
    int _nextId = 1;

    public QueueService()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(TicketDirectory);
        LoadNotifications();
        LoadCustomers();
    }


    void LoadCustomers()
    {
        if (!File.Exists(DataFile)) return;
        foreach (var c in JsonSerializer.Deserialize<List<Customer>>(File.ReadAllText(DataFile)) ?? new())
        {
            _customers.Add(c);
            if (c.Status == Status.Waiting) _queues[c.Service].Enqueue(c.Id);
            if (c.Id >= _nextId) _nextId = c.Id + 1;
        }
    }

    void Save() => File.WriteAllText(DataFile, JsonSerializer.Serialize(_customers, Opts));

    void SaveNotifications() => File.WriteAllText(NotifFile, JsonSerializer.Serialize(_notifications, Opts));

    void LoadNotifications()
    {
        if (!File.Exists(NotifFile)) return;
        _notifications = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(
            File.ReadAllText(NotifFile)) ?? new();
    }

    void PushNotification(int id, string msg)
    {
        if (!_notifications.ContainsKey(id)) _notifications[id] = new();
        _notifications[id].Add(msg);
        SaveNotifications();
    }


    Customer? Find(int id) => _customers.Find(c => c.Id == id);

    IEnumerable<Customer> WaitingFor(ServiceType svc) => _queues[svc]
        .Select(Find)
        .Where(c => c?.Status == Status.Waiting)
        .Select(c => c!);

    int AvgServiceTime(ServiceType svc)
    {
        var done = _customers.Where(c => c.Service == svc && c.ServedMins > 0).ToList();
        return done.Count > 0 ? (int)done.Average(c => c.ServedMins) : DefaultMins;
    }

    int PositionInQueue(int id, ServiceType svc)
    {
        int pos = 0;
        foreach (var customer in WaitingFor(svc))
        {
            pos++;
            if (customer.Id == id) return pos;
        }
        return 0;
    }


    public string GetTicketPath(int id) => Path.Combine(TicketDirectory, $"ticket_{id}.html");

    public string GenerateTicket(int id, string name, string service, string counter, string time, int pos, int wait)
    {
        string qrData    = Uri.EscapeDataString($"TICKET:{id} NAME:{name} SERVICE:{service} COUNTER:{counter}");
        string filename  = GetTicketPath(id);
        File.WriteAllText(filename, $$"""
            <!DOCTYPE html><html><head><meta charset='utf-8'><title>Ticket #{{id}}</title>
            <style>body{font-family:Arial,sans-serif;max-width:420px;margin:40px auto;border:2px solid #222;
            border-radius:8px;padding:30px;text-align:center}h1{font-size:56px;margin:0}
            h2{color:#555;margin:4px 0 20px;font-weight:normal}
            .info{text-align:left;margin:16px 0;line-height:2.2}
            .badge{display:inline-block;background:#1a73e8;color:#fff;border-radius:20px;padding:4px 14px;font-size:13px;margin-bottom:12px}
            img{margin-top:8px;border:1px solid #eee;border-radius:4px}
            .note{color:#999;font-size:12px;margin-top:16px}</style></head><body>
            <div class='badge'>{{service}}</div><h1>#{{id}}</h1><h2>{{counter}}</h2>
            <div class='info'><b>Name:</b> {{name}}<br><b>Arrived:</b> {{time}}<br>
            <b>Position:</b> {{pos}}<br><b>Est. wait:</b> ~{{wait}} min</div>
            <img src='https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={{qrData}}' alt='QR'/>
            <p class='note'>Your queue ticket</p></body></html>
            """);
        return filename;
    }


    public (int id, string service, string counter, int position, int wait) JoinQueue(string name, ServiceType svc)
    {
        var c = new Customer {
            Id = _nextId++, Name = name,
            ArrivalTime = DateTime.Now.ToString("HH:mm"), Service = svc
        };
        _customers.Add(c);
        _queues[svc].Enqueue(c.Id);
        Save();
        int pos  = PositionInQueue(c.Id, svc);
        int wait = pos * AvgServiceTime(svc);
        GenerateTicket(c.Id, name, Labels.Service(svc), Labels.Counter(svc), c.ArrivalTime, pos, wait);
        return (c.Id, Labels.Service(svc), Labels.Counter(svc), pos, wait);
    }

    public (string status, int position, int wait, List<string> notifications) GetStatus(int id)
    {
        var c = Find(id);
        if (c == null) return ("NotFound", 0, 0, new());
        _notifications.TryGetValue(id, out var msgs);
        if (msgs != null)
        {
            _notifications.Remove(id);
            SaveNotifications();
        }
        int pos  = PositionInQueue(id, c.Service);
        int wait = pos * AvgServiceTime(c.Service);
        return (c.Status.ToString(), pos, wait, msgs ?? new());
    }


    public (bool success, int ticketId, string name, string counter, string? approaching) CallNext(ServiceType svc)
    {
        var q = _queues[svc];
        while (q.Count > 0)
        {
            var c = Find(q.Dequeue());
            if (c?.Status != Status.Waiting) continue;
            c.Status   = Status.Called;
            c.CalledAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PushNotification(c.Id, $"You have been called! Proceed to {Labels.Counter(c.Service)} now.");
            Save();
            string? approaching = null;
            var nxt = WaitingFor(svc).FirstOrDefault();
            if (nxt != null)
            {
                approaching = $"#{nxt.Id} {nxt.Name} — you are next!";
                PushNotification(nxt.Id, $"You are next! Get ready for {Labels.Counter(nxt.Service)}.");
            }
            return (true, c.Id, c.Name, Labels.Counter(c.Service), approaching);
        }
        return (false, 0, "", "", null);
    }

    public (bool success, string message) MarkDone(int id)
    {
        var c = Find(id);
        if (c == null || c.Status == Status.Done || c.Status == Status.Skipped)
            return (false, "Ticket not found or already closed.");
        if (c.Status != Status.Called)
            return (false, "Call the ticket before marking it done.");
        if (c.CalledAt > 0)
            c.ServedMins = Math.Max(1, (int)((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - c.CalledAt) / 60));
        c.Status = Status.Done;
        Save();
        return (true, "Done. Service time recorded.");
    }

    public (bool success, string message) SkipTicket(int id)
    {
        var c = Find(id);
        if (c?.Status != Status.Waiting) return (false, "Ticket not found or not in Waiting state.");
        c.Status = Status.Skipped;
        Save();
        return (true, "Ticket skipped.");
    }


    public List<(int id, string name, int position, int estWait)> GetQueue(ServiceType svc)
    {
        int avg = AvgServiceTime(svc), pos = 0;
        return WaitingFor(svc)
            .Select(c => { pos++; return (c.Id, c.Name, pos, pos * avg); })
            .ToList();
    }

    public int QueueCount(ServiceType svc) => WaitingFor(svc).Count();
    public int AvgFor(ServiceType svc)     => AvgServiceTime(svc);

    public (int total, int waiting, int called, int served, int skipped) GetAnalytics()
    {
        int[] cnt = new int[4];
        foreach (var c in _customers) cnt[(int)c.Status]++;
        return (_customers.Count, cnt[0], cnt[1], cnt[2], cnt[3]);
    }
}



