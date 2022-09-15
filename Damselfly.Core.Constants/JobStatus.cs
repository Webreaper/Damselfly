namespace Damselfly.Core.Constants;

public enum JobStatus
{
    Idle,
    Running,
    Paused,
    Disabled,
    Error
}

public class ServiceStatus
{
    public string StatusText { get; set; } = "Initialising";
    public JobStatus Status { get; set; } = JobStatus.Idle;
    public int CPULevel { get; set; }
}