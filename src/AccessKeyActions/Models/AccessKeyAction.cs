namespace AccessKeyActions.Models;

public class AccessKeyAction
{
    public string AccessKeyId { get; set; }
    
    public ActionType Action { get; set; }
}

public enum ActionType
{
    Rotate,
    
    Deactivate,
    
    Delete,
}