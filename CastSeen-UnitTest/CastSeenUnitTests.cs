using CastSeen.Commands;
using CastSeen.Data;
using CastSeen.Models;
using CastSeen.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Runtime.Serialization;

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

    [TestMethod]
    public void ActorDisplay_Initials_SingleName_ReturnsFirstLetter()
    {
        var display = new ActorsViewModel.ActorDisplay
        {
            Name = "Madonna"
        };

        Assert.AreEqual("M", display.Initials);
    }

    [TestMethod]
    public void ActorDisplay_Initials_MultiPartName_ReturnsFirstAndLastInitials()
    {
        var display = new ActorsViewModel.ActorDisplay
        {
            Name = "Robert Downey"
        };

        Assert.AreEqual("RD", display.Initials);
    }

    [TestMethod]
    public void ActorsViewModel_CanPreviousPage_ReturnsFalseOnFirstPage()
    {
        var viewModel = CreateUninitializedActorsViewModel();
        SetPrivateField(viewModel, "_currentPage", 0);

        var result = InvokePrivateBoolMethod(viewModel, "CanPreviousPage");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ActorsViewModel_CanPreviousPage_ReturnsTrueAfterPagingBack()
    {
        var viewModel = CreateUninitializedActorsViewModel();
        SetPrivateField(viewModel, "_currentPage", 1);

        var result = InvokePrivateBoolMethod(viewModel, "CanPreviousPage");

        Assert.IsTrue(result);
    }

    private static ActorsViewModel CreateUninitializedActorsViewModel()
        => (ActorsViewModel)FormatterServices.GetUninitializedObject(typeof(ActorsViewModel));

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(instance, value);
    }

    private static bool InvokePrivateBoolMethod(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        return (bool)method.Invoke(instance, null)!;
    }
}
