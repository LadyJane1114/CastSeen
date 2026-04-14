using CastSeen.Commands;
using CastSeen.Data;
using CastSeen.Models;
using Microsoft.EntityFrameworkCore;

namespace CastSeen_UnitTest;

[TestClass]
public sealed class CastSeenUnitTests
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

    [TestMethod]
    public void ImdbContext_WithOptions_BuildsExpectedModel()
    {
        var options = new DbContextOptionsBuilder<ImdbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ImdbContext(options);

        var title = context.Model.FindEntityType(typeof(Title));
        Assert.IsNotNull(title);
        Assert.AreEqual("Titles", title.GetTableName());

        var genre = context.Model.FindEntityType(typeof(Genre));
        Assert.IsNotNull(genre);
        Assert.AreEqual("Genres", genre.GetTableName());
        Assert.AreEqual(nameof(Genre.GenreId), genre.FindPrimaryKey()!.Properties.Single().Name);

        var titleAlias = context.Model.FindEntityType(typeof(TitleAlias));
        Assert.IsNotNull(titleAlias);
        Assert.AreEqual("PK_Title_AKAs", titleAlias.FindPrimaryKey()!.GetName());
    }

    [TestMethod]
    public void ImdbContext_ConfiguresExpectedJoinTables()
    {
        var options = new DbContextOptionsBuilder<ImdbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ImdbContext(options);

        var titleEntity = context.Model.FindEntityType(typeof(Title));
        Assert.IsNotNull(titleEntity);

        var joinTables = titleEntity.GetSkipNavigations()
            .Select(n => n.JoinEntityType.GetTableName())
            .Where(name => name != null)
            .ToList();

        CollectionAssert.AreEquivalent(
            new[] { "Title_Genres", "Directors", "Writers", "Known_For" },
            joinTables);
    }

    [TestMethod]
    public void ImdbContext_WithOptions_DoesNotRequireConnectionString()
    {
        var options = new DbContextOptionsBuilder<ImdbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ImdbContext(options);

        Assert.IsNotNull(context.Model);
    }
}
