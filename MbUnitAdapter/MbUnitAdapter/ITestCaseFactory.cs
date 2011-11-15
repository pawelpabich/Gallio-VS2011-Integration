using Gallio.Model.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    public interface ITestCaseFactory
    {
        TestCase GetTestCase(TestData testData);
    }
}