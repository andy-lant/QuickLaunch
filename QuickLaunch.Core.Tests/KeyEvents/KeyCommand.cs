using System.Text;
using System.Windows;
using System.Windows.Input;
using Moq;
using QuickLaunch.Core.KeyEvents;

namespace QuickLaunch.Core.Tests.KeyEvents;

[STATestClass]
public sealed class TestWpfSequenceKeyCommandParser
{
    private static WpfSequenceKeyCommandParser NewTestInstance()
    {
        var parser = new WpfSequenceKeyCommandParser();
        parser.RegisterSequence("s", "s");
        parser.RegisterSequence("t", "t");
        parser.RegisterSequence("multi", "multi");
        return parser;
    }

    private static KeyEventArgs CreateKeyEvent(Key key)
    {
        // Use Moq to create a mock PresentationSource.  This avoids
        // the complexities of creating a real PresentationSource in a unit test.
        var mockPresentationSource = new Mock<PresentationSource>();

        // Create a mock KeyboardDevice.  Again, this simplifies the test setup.
        var mockKeyboardDevice = new Mock<KeyboardDevice>(InputManager.Current);

        return new KeyEventArgs(mockKeyboardDevice.Object, mockPresentationSource.Object, 0, key);
    }


    [TestMethod]
    public void TestEscapePressedEvent()
    {
        var parser = NewTestInstance();

        bool escapePressed = false;

        parser.EscapePressed += (s, o) =>
        {
            escapePressed = true;
        };

        escapePressed = false;
        parser.ProcessKeyDown(CreateKeyEvent(Key.D1));
        parser.ProcessKeyDown(CreateKeyEvent(Key.A));
        parser.ProcessKeyDown(CreateKeyEvent(Key.F1));
        Assert.IsFalse(escapePressed);
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsTrue(escapePressed);
        escapePressed = false;
        parser.ProcessKeyDown(CreateKeyEvent(Key.S));
        Assert.IsFalse(escapePressed);
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsTrue(escapePressed);
        escapePressed = false;

        parser.ProcessKeyDown(CreateKeyEvent(Key.M));
        parser.ProcessKeyDown(CreateKeyEvent(Key.U));
        parser.ProcessKeyDown(CreateKeyEvent(Key.L));
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsFalse(escapePressed);
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsTrue(escapePressed);
        escapePressed = false;
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsTrue(escapePressed);
    }


    [TestMethod]
    public void TestSequenceCompleteEvent()
    {
        var parser = NewTestInstance();

        string? tag = null;
        uint? num = null;

        parser.SequenceComplete += (object? s, SequenceCompleteEventArgs e) =>
        {
            num = e.NumericArg;
            tag = e.Tag;
        };

        parser.ProcessKeyDown(CreateKeyEvent(Key.D1));
        parser.ProcessKeyDown(CreateKeyEvent(Key.A));
        parser.ProcessKeyDown(CreateKeyEvent(Key.F1));
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsNull(tag);
        Assert.IsNull(num);

        num = 42;
        parser.ProcessKeyDown(CreateKeyEvent(Key.S));
        Assert.AreEqual("s", tag);
        Assert.IsNull(num);

        tag = null;
        num = null;
        parser.ProcessKeyDown(CreateKeyEvent(Key.D1));
        parser.ProcessKeyDown(CreateKeyEvent(Key.A));
        parser.ProcessKeyDown(CreateKeyEvent(Key.F1));
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsNull(tag);
        Assert.IsNull(num);

        num = 42;
        parser.ProcessKeyDown(CreateKeyEvent(Key.T));
        Assert.AreEqual("t", tag);
        Assert.IsNull(num);
        num = 42;
        parser.ProcessKeyDown(CreateKeyEvent(Key.S));
        Assert.AreEqual("s", tag);
        Assert.IsNull(num);

        tag = null;
        num = null;
        parser.ProcessKeyDown(CreateKeyEvent(Key.M));
        parser.ProcessKeyDown(CreateKeyEvent(Key.U));
        parser.ProcessKeyDown(CreateKeyEvent(Key.L));
        parser.ProcessKeyDown(CreateKeyEvent(Key.T));
        Assert.IsNull(tag);
        Assert.IsNull(num);
        parser.ProcessKeyDown(CreateKeyEvent(Key.M));
        parser.ProcessKeyDown(CreateKeyEvent(Key.U));
        parser.ProcessKeyDown(CreateKeyEvent(Key.L));
        parser.ProcessKeyDown(CreateKeyEvent(Key.T));
        Assert.IsNull(tag);
        Assert.IsNull(num);
        num = 42;
        parser.ProcessKeyDown(CreateKeyEvent(Key.I));
        Assert.AreEqual("multi", tag);
        Assert.IsNull(num);

        tag = null;
        num = 42;
        parser.ProcessKeyDown(CreateKeyEvent(Key.M));
        parser.ProcessKeyDown(CreateKeyEvent(Key.U));
        parser.ProcessKeyDown(CreateKeyEvent(Key.L));
        parser.ProcessKeyDown(CreateKeyEvent(Key.T));
        parser.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsNull(tag);
        Assert.AreEqual(42u, num);
        num = 42;
        parser.ProcessKeyDown(CreateKeyEvent(Key.I));
        Assert.IsNull(tag);
        Assert.AreEqual(42u, num);
    }

    [TestMethod]
    public void TestSequenceEvent()
    {
        var instance = NewTestInstance();

        SequenceEventArgs? ev = null;
        instance.SequenceProgress += (object? sender, SequenceEventArgs e) =>
        {
            ev = e;
        };

        instance.ResetSequence();
        Assert.IsNull(ev);

        StringBuilder b = new();
        for (int i = 0; i < 10; i++)
        {
            b.Append("1");
            ev = null;
            instance.ProcessKeyDown(CreateKeyEvent(Key.D1));
            AssertSequenceProgress(ev, b.ToString());
        }
        ev = null;
        instance.ProcessKeyDown(CreateKeyEvent(Key.D1));
        AssertSequenceAborted(ev, "11111111111");
        ev = null;
        instance.ProcessKeyDown(CreateKeyEvent(Key.D1));
        AssertSequenceProgress(ev, "1");
        ev = null;
        instance.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        AssertSequenceAborted(ev, "1");
        ev = null;
        instance.ProcessKeyDown(CreateKeyEvent(Key.Escape));
        Assert.IsNull(ev);
        instance.ProcessKeyDown(CreateKeyEvent(Key.D1));
        AssertSequenceProgress(ev, "1");
        ev = null;
        instance.ProcessKeyDown(CreateKeyEvent(Key.I));
        AssertSequenceAborted(ev, "1I");
        ev = null;
        instance.ProcessKeyDown(CreateKeyEvent(Key.I));
        AssertSequenceAborted(ev, "I");
        ev = null;
        instance.ProcessKeyDown(CreateKeyEvent(Key.S));
        AssertSequenceCompleted(ev, "S");

        //instance.ProcessKeyDown(CreateKeyEvent(Key.I));
        //Assert.IsNotNull(ev);
        //Assert.AreEqual("I", ev.SequenceDescription);
        //ev = null;
        //instance.ProcessKeyDown(CreateKeyEvent(Key.I));
        //Assert.IsNotNull(ev);
        //Assert.AreEqual("I", ev.SequenceDescription);
        //ev = null;

    }

    private static void AssertSequenceAborted(SequenceEventArgs? ev, string expectedSequence)
    {
        Assert.IsNotNull(ev);
        Assert.IsTrue(ev.IsAborted);
        Assert.IsFalse(ev.IsCompleted);
        Assert.AreEqual(expectedSequence, ev.SequenceDescription);
    }
    private static void AssertSequenceProgress(SequenceEventArgs? ev, string expectedSequence)
    {
        Assert.IsNotNull(ev);
        Assert.IsFalse(ev.IsAborted);
        Assert.IsFalse(ev.IsCompleted);
        Assert.AreEqual(expectedSequence, ev.SequenceDescription);
    }
    private static void AssertSequenceCompleted(SequenceEventArgs? ev, string expectedSequence)
    {
        Assert.IsNotNull(ev);
        Assert.IsFalse(ev.IsAborted);
        Assert.IsTrue(ev.IsCompleted);
        Assert.AreEqual(expectedSequence, ev.SequenceDescription);
    }
}
