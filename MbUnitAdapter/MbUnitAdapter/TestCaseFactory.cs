using System;
using Gallio.Common.Reflection;
using Gallio.Model.Schema;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.Gallio
{
    public class TestCaseFactory : ITestCaseFactory
    {
        private readonly TestProperty testIdProperty;
        private readonly TestProperty filePathProperty;

        public TestCaseFactory(TestProperty testIdProperty, TestProperty filePathProperty)
        {
            this.testIdProperty = testIdProperty;
            this.filePathProperty = filePathProperty;
        }

        public TestCase GetTestCase(TestData testData)
        {
            var testCase = new TestCase(testData.FullName, new Uri(GallioAdapter.ExecutorUri))
            {
                Source = testData.CodeReference.AssemblyName,
                CodeFilePath = testData.CodeLocation.Path,
                LineNumber = testData.CodeLocation.Line
            };

            if (testData.CodeElement != null)
            {
                testCase.DisplayName = testData.CodeElement.Name;
                var assembly = ReflectionUtils.GetAssembly(testData.CodeElement);
                testCase.SetPropertyValue(filePathProperty, assembly.Path);
            }

            testCase.SetPropertyValue(testIdProperty, testData.Id);

            return testCase;
        } 
    }
}