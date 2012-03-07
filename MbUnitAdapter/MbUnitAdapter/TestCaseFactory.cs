using System;
using System.Collections.Generic;
using Gallio.Common.Reflection;
using Gallio.Model.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestPlatform.Gallio
{
    public class TestCaseFactory : ITestCaseFactory
    {
        private readonly TestProperty testIdProperty;

        private IList<string> sources; 

        public TestCaseFactory(TestProperty testIdProperty)
        {
            this.testIdProperty = testIdProperty;
        }

        public TestCase GetTestCase(TestData testData)
        {
            var testCase = new TestCase(testData.FullName, new Uri(GallioAdapter.ExecutorUri), GetSource(testData))
            {
                CodeFilePath = testData.CodeLocation.Path,
                LineNumber = testData.CodeLocation.Line,
                DisplayName = testData.CodeReference.MemberName
            };

            testCase.SetPropertyValue(testIdProperty, testData.Id);

            return testCase;
        }

        private string GetSource(TestData testData)
        {
            if (testData.CodeElement != null)
            {
                var assembly = ReflectionUtils.GetAssembly(testData.CodeElement);
                return assembly.Path;
            }
            if (sources.Count == 1)
            {
                return sources[0];
            }
            else
            {
                // TODO: match assembly name to source? really? :(
                return null;
            }
        }

        public void AddSources(IEnumerable<string> sources)
        {
            this.sources = new List<string>(sources);
        }
    }
}