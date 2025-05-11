using QuickLaunch.Core.Actions;
using QuickLaunch.Core.Config;
using QuickLaunch.UI.Parsers;

namespace QuickLaunch.Tests.UI.Parsers;

[TestClass]
sealed public class TestActionRepresentation()
{

    [TestMethod]
    public void TestToString()
    {
        var type = RunProgram.ActionType;
        var parameters = type.Parameters;
        ActionRegistration ar = ActionRegistration.Create(
            RunProgram.ActionType,
            new[]
            {
                ActionParameter.Create(parameters[0], "Notepad++.exe"),
                ActionParameter.Create(parameters[1], new StringListParameter(new [] {"test["}))
            }
        );
        var converter = new ActionRepresentationConverter();
        var actionRepresentation = converter.ConvertFrom(ar);

        Assert.IsNotNull(actionRepresentation);
        Assert.AreEqual(@"RunProgram Arguments=[""test\[""] Executable=Notepad++.exe", actionRepresentation.ToString());
    }

    [TestMethod]
    public void TestFromString()
    {
        var actionRepresentation = new ActionRepresentation(@"RunProgram Arguments=[""test\[""] Executable=Notepad++.exe");
        var converter = new ActionRepresentationConverter();
        var actionRegistration = (ActionRegistration?)converter.ConvertTo(actionRepresentation, typeof(ActionRegistration));
        Assert.IsNotNull(actionRegistration);
        Assert.AreEqual("test[", ((StringListParameter?)actionRegistration.Parameters[0].Value)?.List[0]);
    }

}

