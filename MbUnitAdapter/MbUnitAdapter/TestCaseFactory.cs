using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
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
            string displayName;

            var fullName = testData.FullName;

            var pos = fullName.LastIndexOf('/');

            if (pos == -1)
            {
                displayName = testData.CodeReference.MemberName;
            }
            else
            {
                displayName = fullName.Substring(pos + 1);
            }



            var testCase = new TestCase(fullName, new Uri(GallioAdapter.ExecutorUri), GetSource(testData))
            {
                CodeFilePath = testData.CodeLocation.Path,
                LineNumber = testData.CodeLocation.Line,
                DisplayName = displayName
            };

            testCase.SetPropertyValue(testIdProperty, testData.Id);

            return testCase;
        }

        private string GetSource(TestData testData)
        {
            var assemblyName = new AssemblyName(testData.CodeReference.AssemblyName);
            var pathToSourceAssembly = sources.Single(s => s.Contains(assemblyName.Name));
            return pathToSourceAssembly;
        }

        public void AddSources(IEnumerable<string> sources)
        {
            this.sources = new List<string>(sources);
        }
    }
}