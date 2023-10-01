namespace AzureMonitorAlertToTeams.Models;
using AzureMonitorAlertToTeams.Models;

public class AlertProcessingResult
{
    public Alert ProcessedAlert { get; set; }
    public bool SendTeamsMessage { get; set; }
    public bool SendJiraMessage { get; set; }
    public bool SendEmailMessage { get; set; }

}