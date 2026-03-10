using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class HelloTest
{

    [TestCase]
    public void TestHelloWorld()
    {
        AssertBool(true);
    }
}