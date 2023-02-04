using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(States.DateTimeAdd.Serialization.CustomLambdaSerializer))]

namespace States.DateTimeAdd;

public class Function
{
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public DateTime FunctionHandler(DateTimeAddRequest input, ILambdaContext context) => input.Unit switch
    {
        TimeSpanUnit.Ticks => input.Value.AddTicks(input.Step),
        TimeSpanUnit.Milliseconds => input.Value.AddMilliseconds(input.Step),
        TimeSpanUnit.Seconds => input.Value.AddSeconds(input.Step),
        TimeSpanUnit.Minutes => input.Value.AddMinutes(input.Step),
        TimeSpanUnit.Hours => input.Value.AddHours(input.Step),
        TimeSpanUnit.Days => input.Value.AddDays(input.Step),
        TimeSpanUnit.Months => input.Value.AddMonths((int)input.Step),
        TimeSpanUnit.Years => input.Value.AddYears((int)input.Step),
        _ => throw new ArgumentException()
    };
}
