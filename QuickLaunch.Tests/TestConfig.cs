using QuickLaunch.Core.Config;

namespace QuickLaunch.Config;

[TestClass]
sealed public class TestLoader()
{
    const string testFile = "test.toml";
    [TestMethod]
    public void TestLoad()
    {
        File.WriteAllText(testFile, """

            [[dispatcher]]
            name = "QuickOpen"

            [[dispatcher]]
            name = "Notepad++"

            [[action]]
            dispatcher = "QuickOpen"
            type = "OpenFile"
            index = 1
            [action.params]
            Path = "D:\\_NOBACKUP\\_del\\pyBsw.txt"

            [[action]]
            dispatcher = "Notepad++"
            type = "RunProgram"
            index = 1
            [action.params]
            Executable = "Notepad++.exe"
            Arguments = "test"

            [[action]]
            dispatcher = "Notepad++"
            type = "RunProgram"
            index = 2
            [action.params]
            Executable = "Notepad++.exe"
            Arguments = []

            [[action]]
            dispatcher = "Notepad++"
            type = "RunProgram"
            index = 3
            [action.params]
            Executable = "Notepad++.exe"
            Arguments = ["single"]

            [[action]]
            dispatcher = "Notepad++"
            type = "RunProgram"
            index = 4
            [action.params]
            Executable = "Notepad++.exe"
            Arguments = ["multiple", "args"]

            [[command]]
            sequence = "Q"
            name = "QuickOpen"
            dispatcher = "QuickOpen"

            [[command]]
            sequence = "N"
            name = "Notepad++"
            dispatcher = "Notepad++"

        """);

        var loader = new ConfigurationLoader(testFile);
        var config = loader.LoadConfig();


    }
}

