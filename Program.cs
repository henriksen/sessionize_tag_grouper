using Newtonsoft.Json;
using Spectre.Console;

string url = Environment.GetEnvironmentVariable("SESSIONIZE_API_URL") ?? throw new ArgumentException("API_URL environment variable is not set");

while (true) // Run indefinitely
{
    var sessions = await FetchSessionsAsync(url);
    var groupedSessions = GroupSessionsByMainTag(sessions);
    Console.Clear(); // Clear the console for a fresh display
    DisplayGroupedSessions(groupedSessions);
    await Task.Delay(5000); // Wait for 5 seconds before the next update
}

static async Task<List<SessionGroup>> FetchSessionsAsync(string apiUrl)
{
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync(apiUrl);
    var sessionGroups = JsonConvert.DeserializeObject<List<SessionGroup>>(response) ?? [];
    return sessionGroups;
}

static Dictionary<string, Dictionary<string, int>> GroupSessionsByMainTag(List<SessionGroup> sessionGroups)
{
    var mainTagCounts = new Dictionary<string, Dictionary<string, int>>();

    foreach (var group in sessionGroups)
    {
        foreach (var session in group.Sessions.Where(s => s.Status != "Declined" && s.Status != "Decline_Queue"))
        {
            var hasDesiredCategory = session.Categories
                .Any(c => c.CategoryItems.Any(ci => ci.Name == "Session (60 min)" || ci.Name == "Geek Out (60 Min)" || ci.Name == "Keynote (60 min)"));

            if (!hasDesiredCategory)
            {
                continue; // Skip this session if it doesn't match the category or status criteria
            }

            var mainTag = session.Categories
                .FirstOrDefault(c => c.Id == 67576)?.CategoryItems.FirstOrDefault()?.Name;

            if (mainTag != null)
            {
                if (!mainTagCounts.ContainsKey(mainTag))
                {
                    mainTagCounts[mainTag] = new Dictionary<string, int> {
                                {"Nominated", 0},
                                {"Accept_Queue", 0},
                                {"Accepted", 0},
                                {"Total", 0}
                            };
                }

                if (mainTagCounts[mainTag].ContainsKey(session.Status))
                {
                    mainTagCounts[mainTag][session.Status]++;
                    mainTagCounts[mainTag]["Total"]++;
                }
            }
        }
    }

    return mainTagCounts;
}

static void DisplayGroupedSessions(Dictionary<string, Dictionary<string, int>> groupedSessions)
{
    var table = new Table();
    table.AddColumn(new TableColumn("Main Tag").RightAligned());
    table.AddColumn(new TableColumn("Nominated").RightAligned());
    table.AddColumn(new TableColumn("Accept_Queue").RightAligned());
    table.AddColumn(new TableColumn("Accepted").RightAligned());
    table.AddColumn(new TableColumn("Row Total").RightAligned());

    foreach (var kvp in groupedSessions)
    {
        table.AddRow(kvp.Key,
                      kvp.Value["Nominated"].ToString(),
                      kvp.Value["Accept_Queue"].ToString(),
                      kvp.Value["Accepted"].ToString(),
                      kvp.Value["Total"].ToString());

    }

    table.AddEmptyRow();
    table.AddRow("Grand Total",
    groupedSessions.Sum(kvp => kvp.Value["Nominated"]).ToString(),
    groupedSessions.Sum(kvp => kvp.Value["Accept_Queue"]).ToString(),
    groupedSessions.Sum(kvp => kvp.Value["Accepted"]).ToString(),
    groupedSessions.Sum(kvp => kvp.Value["Total"]).ToString());

    AnsiConsole.Write(table);
}

public class SessionGroup
{
    public string GroupName { get; set; } = "";
    public List<Session> Sessions { get; set; } = [];
}

public class Session
{
    public string Title { get; set; } = "";
    public List<Category> Categories { get; set; } = [];
    public string Status { get; set; } = "";
}

public class Category
{
    public int Id { get; set; }
    public List<CategoryItem> CategoryItems { get; set; } = [];
}

public class CategoryItem
{
    public string Name { get; set; } = "";
}

