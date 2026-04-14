using CastSeen.Commands;
using CastSeen.Data;
using CastSeen.Models;
using CastSeen.ViewModels;
using Microsoft.EntityFrameworkCore;
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
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 0);

        Assert.IsFalse(viewModel.CanPreviousPage());
    }

    [TestMethod]
    public void ActorsViewModel_CanPreviousPage_ReturnsTrueAfterPagingBack()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 1);

        Assert.IsTrue(viewModel.CanPreviousPage());
    }

    [TestMethod]
    public void ActorsViewModel_OpenActor_WithNullActor_DoesNotThrow()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();

        viewModel.OpenActor(null);
    }

    [TestMethod]
    public void ActorsViewModel_OpenActor_WithEmptyNameId_DoesNotThrow()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        var actor = new ActorsViewModel.ActorDisplay { NameId = "" };

        viewModel.OpenActor(actor);
    }

    [TestMethod]
    public void DetailsNavigationRequest_StoresActorTargetAndId()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Actor, "nm123");

        Assert.AreEqual(DetailsTargetType.Actor, request.TargetType);
        Assert.AreEqual("nm123", request.Id);
    }

    [TestMethod]
    public void DetailsNavigationRequest_StoresMovieTargetAndId()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Movie, "tt456");

        Assert.AreEqual(DetailsTargetType.Movie, request.TargetType);
        Assert.AreEqual("tt456", request.Id);
    }

    [TestMethod]
    public void DetailsTargetType_HasExpectedValues()
    {
        Assert.AreEqual(0, (int)DetailsTargetType.Actor);
        Assert.AreEqual(1, (int)DetailsTargetType.Movie);
    }

    [TestMethod]
    public void MainViewModel_Return_PopsHistory()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MainViewModel>();
        var history = new Stack<object>();
        var previous = new object();
        history.Push(previous);

        UnitTestHelpers.SetPrivateField(viewModel, "_history", history);
        UnitTestHelpers.SetPrivateField(viewModel, "_currentViewModel", new object());

        viewModel.Return();

        Assert.AreSame(previous, viewModel.CurrentViewModel);
    }

    [TestMethod]
    public void MainViewModel_Navigate_PushesCurrentViewModelAndReplacesIt()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MainViewModel>();
        var history = new Stack<object>();
        var current = new object();
        var next = new object();

        UnitTestHelpers.SetPrivateField(viewModel, "_history", history);
        UnitTestHelpers.SetPrivateField(viewModel, "_currentViewModel", current);

        viewModel.Navigate(() => next);

        Assert.AreSame(current, history.Peek());
        Assert.AreSame(next, viewModel.CurrentViewModel);
    }

    [TestMethod]
    public void MovieDisplay_TopActorsDisplay_FormatsCommaSeparatedList()
    {
        var display = new MoviesViewModel.MovieDisplay
        {
            TopActors = new List<string> { "A", "B", "C" }
        };

        Assert.AreEqual("A, B, C", display.TopActorsDisplay);
    }

    [TestMethod]
    public void MovieDisplay_TopActorsDisplay_EmptyList_ReturnsEmptyString()
    {
        var display = new MoviesViewModel.MovieDisplay();

        Assert.AreEqual(string.Empty, display.TopActorsDisplay);
    }

    [TestMethod]
    public void MoviesViewModel_CanPreviousPage_ReturnsFalseOnFirstPage()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MoviesViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 0);

        Assert.IsFalse(viewModel.CanPreviousPage());
    }

    [TestMethod]
    public void MoviesViewModel_CanPreviousPage_ReturnsTrueAfterPagingForward()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MoviesViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 1);

        Assert.IsTrue(viewModel.CanPreviousPage());
    }
}
