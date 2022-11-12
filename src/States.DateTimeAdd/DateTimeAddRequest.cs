namespace States.DateTimeAdd;

public class DateTimeAddRequest
{
    public DateTime Value { get; set; }
    
    public long Step { get; set; }

    public TimeSpanUnit Unit { get; set; } = TimeSpanUnit.Ticks;
}