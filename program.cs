using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class EventLogMonitor
{
    static HttpClient httpClient = new HttpClient();
    static string webhookUrl = "https://hooks.slack.com/services/PUTHERE/YOURSLACK/WEBHOOKURL";

    static void Main(string[] args)
    {
        // Start log monitoring on a separate thread.
        Thread monitoringThread = new Thread(new ThreadStart(MonitorEventLog));
        monitoringThread.Start();

        // Start listening for 't' key, in a separate thread.
        Thread inputThread = new Thread(new ThreadStart(CreateTestErrorEntry));
        inputThread.Start();

        // The main loop.
        Console.WriteLine("Application is running. Press 'Ctrl + C', to finish.");
        monitoringThread.Join();
        inputThread.Join();
    }

    static void MonitorEventLog()
    {
        EventLog eventLog = new EventLog("Application");
        eventLog.EntryWritten += new EntryWrittenEventHandler(OnEntryWritten);
        eventLog.EnableRaisingEvents = true;

        // A loop that prevents program termination.
        while (true)
        {
            Thread.Sleep(10000); // Wait 10 seconds.
        }
    }

    static async void OnEntryWritten(object source, EntryWrittenEventArgs e)
    {
        if (e.Entry.EntryType == EventLogEntryType.Error || e.Entry.EntryType == EventLogEntryType.FailureAudit)
        {
            string message = $"New event: {e.Entry.EntryType} - {e.Entry.Message}";
            await SendSlackNotification(message);
        }
    }

    static async Task SendSlackNotification(string message)
    {
        // Removing special characters that could disrupt the JSON.
        string cleanMessage = message.Replace("\"", "\\\"");

        // Construct JSON payload directly as a string.
        string jsonPayload = $"{{\"text\": \"{cleanMessage}\"}}";
        var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(webhookUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error sending notification to Slack: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Slack response content: {responseContent}");
        }
    }

    public static void CreateTestErrorEntry()
    {
        Console.WriteLine("Press 't' key, to generate a test error communicate into the Slack.");

        while (true)
        {
            if (Console.ReadKey(true).Key == ConsoleKey.T)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry("this is a test error communicate send to Slack.", EventLogEntryType.Error);
                }

                Console.WriteLine("A test error message was generated.");
            }
            Thread.Sleep(100);
        }
    }
}
