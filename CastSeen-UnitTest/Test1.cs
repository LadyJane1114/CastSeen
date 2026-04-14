using CastSeen.Commands;

namespace CastSeen_UnitTest;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void RelayCommand_Execute_CallsAction()
    {
        var called = false;

        var command = new RelayCommand(() => called = true);

        command.Execute(null);

        Assert.IsTrue(called);
    }

    [TestMethod]
    public void RelayCommand_CanExecute_UsesPredicate()
    {
        var command = new RelayCommand(() => { }, () => false);

        Assert.IsFalse(command.CanExecute(null));
    }

    [TestMethod]
    public void RelayCommandOfT_Execute_PassesParameter()
    {
        string? received = null;

        var command = new RelayCommand<string>(value => received = value);

        command.Execute("abc");

        Assert.AreEqual("abc", received);
    }

    [TestMethod]
    public void RelayCommandOfT_CanExecute_UsesPredicate()
    {
        var command = new RelayCommand<string>(_ => { }, value => value == "ok");

        Assert.IsTrue(command.CanExecute("ok"));
        Assert.IsFalse(command.CanExecute("no"));
    }
}
