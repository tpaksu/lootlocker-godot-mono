using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

[AttributeUsage(AttributeTargets.Method)]
public class CoroutineTestAttribute : CombiningStrategyAttribute, ISimpleTestBuilder, IImplyFixture
{
    public CoroutineTestAttribute() : base(new CombinatorialStrategy(), new ParameterDataSourceProvider()) { }

    private readonly NUnitTestCaseBuilder _builder = new NUnitTestCaseBuilder();

    TestMethod ISimpleTestBuilder.BuildFrom(IMethodInfo method, Test suite)
    {
        TestCaseParameters parms = new TestCaseParameters
        {
            ExpectedResult = new object(),
            HasExpectedResult = true
        };

        var t = _builder.BuildTestMethod(method, suite, parms);

        if (t.Properties["HasExpectedResult"] != null)
            t.Properties["HasExpectedResult"].Add(false);
        return t;
    }
}
