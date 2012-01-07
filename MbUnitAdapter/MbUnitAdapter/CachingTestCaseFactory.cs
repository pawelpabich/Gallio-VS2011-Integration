using System.Collections.Generic;
using Gallio.Common;
using Gallio.Model.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestPlatform.Gallio
{
    public class CachingTestCaseFactory : ITestCaseFactory
    {
        private readonly ITestCaseFactory testCaseFactory;
        private readonly TestProperty testIdProperty;
        private KeyedMemoizer<string, TestCase> testCases;

        public CachingTestCaseFactory(ITestCaseFactory testCaseFactory, TestProperty testIdProperty)
        {
            this.testCaseFactory = testCaseFactory;
            this.testIdProperty = testIdProperty;
            testCases = new KeyedMemoizer<string, TestCase>();
        }

        public TestCase GetTestCase(TestData testData)
        {
            return testCases.Memoize(testData.Id, () => testCaseFactory.GetTestCase(testData));
        }

        public void AddTestCases(IEnumerable<TestCase> cases)
        {
            foreach (var testCase in cases)
            {
                var hoistedTestCase = testCase;
                var testId = testCase.GetPropertyValue(testIdProperty, "");
                testCases.Memoize(testId, () => hoistedTestCase);
            }
        }
    }
}