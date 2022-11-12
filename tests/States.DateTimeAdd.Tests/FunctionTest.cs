using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

namespace States.DateTimeAdd.Tests;

public class FunctionTest
{
    private readonly Function _classUnderTest;
    
    public FunctionTest()
    {
        _classUnderTest = new Function();
    }
    
    [Fact]
    public void TestFunctionHandler_WhenNoUnitIsSet_ThenTicksAreUsed()
    {
        var epoch = DateTime.UnixEpoch;
        var input = new DateTimeAddRequest { Step = 1, Value = epoch };
        
        var result = _classUnderTest.FunctionHandler(input, new TestLambdaContext());
        Assert.Equal(epoch.AddTicks(1L), result);
    }

    [Theory]
    [InlineData(TimeSpanUnit.Ticks)]
    [InlineData(TimeSpanUnit.Milliseconds)]
    [InlineData(TimeSpanUnit.Seconds)]
    [InlineData(TimeSpanUnit.Minutes)]
    [InlineData(TimeSpanUnit.Hours)]
    [InlineData(TimeSpanUnit.Days)]
    [InlineData(TimeSpanUnit.Months)]
    [InlineData(TimeSpanUnit.Years)]
    public void TestFunctionHandler_WhenStepIsZero_ThenResultIsEqualToInput(TimeSpanUnit unit)
    {
        var epoch = DateTime.UnixEpoch;
        var input = new DateTimeAddRequest { Step = 0L, Unit = unit, Value = epoch };
        
        var result = _classUnderTest.FunctionHandler(input, new TestLambdaContext());
        Assert.Equal(epoch, result);
    }
}
