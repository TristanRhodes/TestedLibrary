using FluentAssertions;

namespace TestedLibrary.UnitTests;

public class MyClassTests
{
    MyClass subject = new MyClass();

    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(2, 2, 4)]
    [InlineData(5, 10, 50)]
    public void MultiplicationTest(int x, int y, int expected)
    {
        var result = subject.Multiply(x, y);
        result.Should().Be(expected);
    }
}