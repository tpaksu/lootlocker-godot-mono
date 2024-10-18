using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

#nullable enable
[AttributeUsage(AttributeTargets.Method)]
public class CoroutineTestAttribute : CombiningStrategyAttribute, IImplyFixture
{
    public CoroutineTestAttribute() : base(new CombinatorialStrategy(), new ParameterDataSourceProvider()) { }

    private readonly NUnitTestCaseBuilder _tcBuilder = new();

    new public TestMethod BuildFrom(IMethodInfo method, Test? suite)
    {
        TestCaseParameters parms = new TestCaseParameters
        {
            ExpectedResult = new object(),
            HasExpectedResult = true
        };

        var t = _tcBuilder.BuildTestMethod(method, suite, parms);

        if (t.Properties["HasExpectedResult"] != null)
            t.Properties["HasExpectedResult"].Add(false);
        return t;
    }
}
