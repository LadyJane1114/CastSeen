using CastSeen.Commands;
using CastSeen.Data;
using CastSeen.Models;
using CastSeen.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;

namespace CastSeen_UnitTest;

[TestClass]
public sealed class CastSeenUnitTests
{
    [TestMethod]
    // Verifies that `RelayCommand` executes its action.
    public void RelayCommand_Execute_CallsAction()
    {
        var called = false;

        var command = new RelayCommand(() => called = true);

        command.Execute(null);

        Assert.IsTrue(called);
    }

    [TestMethod]
    // Verifies that `RelayCommand` respects the `canExecute` predicate.
    public void RelayCommand_CanExecute_UsesPredicate()
    {
        var command = new RelayCommand(() => { }, () => false);

        Assert.IsFalse(command.CanExecute(null));
    }

    [TestMethod]
    // Verifies that `RelayCommand<T>` passes the input parameter to the action.
    public void RelayCommandOfT_Execute_PassesParameter()
    {
        string? received = null;

        var command = new RelayCommand<string>(value => received = value);

        command.Execute("abc");

        Assert.AreEqual("abc", received);
    }

    [TestMethod]
    // Verifies that `RelayCommand<T>` respects the typed `canExecute` predicate.
    public void RelayCommandOfT_CanExecute_UsesPredicate()
    {
        var command = new RelayCommand<string>(_ => { }, value => value == "ok");

        Assert.IsTrue(command.CanExecute("ok"));
        Assert.IsFalse(command.CanExecute("no"));
    }

    [TestMethod]
    // Verifies that `ImdbContext` maps the main entities to the expected tables and key names.
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
    // Verifies that `ImdbContext` configures the expected many-to-many join tables.
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
    // Verifies that `ImdbContext` can be created with explicit options without needing a connection string.
    public void ImdbContext_WithOptions_DoesNotRequireConnectionString()
    {
        var options = new DbContextOptionsBuilder<ImdbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ImdbContext(options);

        Assert.IsNotNull(context.Model);
    }

    [TestMethod]
    // Verifies that a single-name actor produces one initial.
    public void ActorDisplay_Initials_SingleName_ReturnsFirstLetter()
    {
        var display = new ActorsViewModel.ActorDisplay
        {
            Name = "Madonna"
        };

        Assert.AreEqual("M", display.Initials);
    }

    [TestMethod]
    // Verifies that a multi-part actor name produces first and last initials.
    public void ActorDisplay_Initials_MultiPartName_ReturnsFirstAndLastInitials()
    {
        var display = new ActorsViewModel.ActorDisplay
        {
            Name = "Robert Downey"
        };

        Assert.AreEqual("RD", display.Initials);
    }

    [TestMethod]
    // Verifies that previous-page navigation is disabled on the first actor page.
    public void ActorsViewModel_CanPreviousPage_ReturnsFalseOnFirstPage()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 0);

        Assert.IsFalse(viewModel.CanPreviousPage());
    }

    [TestMethod]
    // Verifies that previous-page navigation is enabled after moving forward.
    public void ActorsViewModel_CanPreviousPage_ReturnsTrueAfterPagingBack()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 1);

        Assert.IsTrue(viewModel.CanPreviousPage());
    }

    [TestMethod]
    // Verifies that next-page navigation is disabled while actors are loading.
    public void ActorsViewModel_CanNextPage_ReturnsFalseWhenLoading()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_isLoading", true);

        Assert.IsFalse(viewModel.CanNextPage());
    }

    [TestMethod]
    // Verifies that next-page navigation is enabled when actors are not loading.
    public void ActorsViewModel_CanNextPage_ReturnsTrueWhenNotLoading()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_isLoading", false);

        Assert.IsTrue(viewModel.CanNextPage());
    }

    [TestMethod]
    // Verifies that opening an actor with a null input does not throw.
    public void ActorsViewModel_OpenActor_WithNullActor_DoesNotThrow()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();

        viewModel.OpenActor(null);
    }

    [TestMethod]
    // Verifies that opening an actor with an empty ID is ignored safely.
    public void ActorsViewModel_OpenActor_WithEmptyNameId_DoesNotThrow()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<ActorsViewModel>();
        var actor = new ActorsViewModel.ActorDisplay { NameId = "" };

        viewModel.OpenActor(actor);
    }

    [TestMethod]
    // Verifies that `DetailsNavigationRequest` stores actor target data correctly.
    public void DetailsNavigationRequest_StoresActorTargetAndId()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Actor, "nm123");

        Assert.AreEqual(DetailsTargetType.Actor, request.TargetType);
        Assert.AreEqual("nm123", request.Id);
    }

    [TestMethod]
    // Verifies that `DetailsNavigationRequest` stores movie target data correctly.
    public void DetailsNavigationRequest_StoresMovieTargetAndId()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Movie, "tt456");

        Assert.AreEqual(DetailsTargetType.Movie, request.TargetType);
        Assert.AreEqual("tt456", request.Id);
    }

    [TestMethod]
    // Verifies that the `DetailsTargetType` enum has the expected values.
    public void DetailsTargetType_HasExpectedValues()
    {
        Assert.AreEqual(0, (int)DetailsTargetType.Actor);
        Assert.AreEqual(1, (int)DetailsTargetType.Movie);
    }

    [TestMethod]
    // Verifies that `MainViewModel.Return` restores the previous page from history.
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
    // Verifies that `MainViewModel.Navigate` pushes the current page and replaces it.
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
    // Verifies that the movie cast display joins names with commas.
    public void MovieDisplay_TopActorsDisplay_FormatsCommaSeparatedList()
    {
        var display = new MoviesViewModel.MovieDisplay
        {
            TopActors = new List<string> { "A", "B", "C" }
        };

        Assert.AreEqual("A, B, C", display.TopActorsDisplay);
    }

    [TestMethod]
    // Verifies that the movie cast display is empty when there are no actors.
    public void MovieDisplay_TopActorsDisplay_EmptyList_ReturnsEmptyString()
    {
        var display = new MoviesViewModel.MovieDisplay();

        Assert.AreEqual(string.Empty, display.TopActorsDisplay);
    }

    [TestMethod]
    // Verifies that previous-page navigation is disabled on the first movie page.
    public void MoviesViewModel_CanPreviousPage_ReturnsFalseOnFirstPage()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MoviesViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 0);

        Assert.IsFalse(viewModel.CanPreviousPage());
    }

    [TestMethod]
    // Verifies that previous-page navigation is enabled after moving forward.
    public void MoviesViewModel_CanPreviousPage_ReturnsTrueAfterPagingForward()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MoviesViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_currentPage", 1);

        Assert.IsTrue(viewModel.CanPreviousPage());
    }

    [TestMethod]
    // Verifies that next-page navigation is disabled while movies are loading.
    public void MoviesViewModel_CanNextPage_ReturnsFalseWhenLoading()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MoviesViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_isLoading", true);

        Assert.IsFalse(viewModel.CanNextPage());
    }

    [TestMethod]
    // Verifies that next-page navigation is enabled when movies are not loading.
    public void MoviesViewModel_CanNextPage_ReturnsTrueWhenNotLoading()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MoviesViewModel>();
        UnitTestHelpers.SetPrivateField(viewModel, "_isLoading", false);

        Assert.IsTrue(viewModel.CanNextPage());
    }

    [TestMethod]
    // Verifies that opening a movie with an empty ID is ignored safely.
    public void MoviesViewModel_OpenMovie_WithEmptyTitleId_DoesNotThrow()
    {
        var viewModel = UnitTestHelpers.CreateUninitialized<MoviesViewModel>();
        var movie = new MoviesViewModel.MovieDisplay { TitleId = "" };

        viewModel.OpenMovie(movie);
    }

    [TestMethod]
    // Verifies that `DetailsViewModel` stores the incoming request and identifier.
    public void DetailsViewModel_StoresRequestAndIdentifier()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Actor, "nm123");
        var mainViewModel = UnitTestHelpers.CreateMainViewModelWithReturnCommand();

        var viewModel = new DetailsViewModel(mainViewModel, request, false);

        Assert.AreSame(request, viewModel.Request);
        Assert.AreEqual("nm123", viewModel.Identifier);
    }

    [TestMethod]
    // Verifies that an actor details request shows the actor section only.
    public void DetailsViewModel_ActorRequest_SetsActorVisibility()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Actor, "nm123");
        var mainViewModel = UnitTestHelpers.CreateMainViewModelWithReturnCommand();

        var viewModel = new DetailsViewModel(mainViewModel, request, false);

        Assert.AreEqual(Visibility.Visible, viewModel.ActorSectionVisibility);
        Assert.AreEqual(Visibility.Collapsed, viewModel.MovieSectionVisibility);
    }

    [TestMethod]
    // Verifies that a movie details request shows the movie section only.
    public void DetailsViewModel_MovieRequest_SetsMovieVisibility()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Movie, "tt456");
        var mainViewModel = UnitTestHelpers.CreateMainViewModelWithReturnCommand();

        var viewModel = new DetailsViewModel(mainViewModel, request, false);

        Assert.AreEqual(Visibility.Collapsed, viewModel.ActorSectionVisibility);
        Assert.AreEqual(Visibility.Visible, viewModel.MovieSectionVisibility);
    }

    [TestMethod]
    // Verifies that `DetailsViewModel.BackCommand` uses `MainViewModel.ReturnCommand`.
    public void DetailsViewModel_BackCommand_IsWiredToMainViewModelReturnCommand()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Actor, "nm123");
        var mainViewModel = UnitTestHelpers.CreateUninitialized<MainViewModel>();
        var returnCommand = new RelayCommand(() => { });
        UnitTestHelpers.SetPrivateField(mainViewModel, "<ReturnCommand>k__BackingField", returnCommand);

        var viewModel = new DetailsViewModel(mainViewModel, request, false);

        Assert.AreSame(returnCommand, viewModel.BackCommand);
    }

    [TestMethod]
    // Verifies that details loading starts in the visible/loading state.
    public void DetailsViewModel_LoadingVisibility_StartsVisible()
    {
        var request = new DetailsNavigationRequest(DetailsTargetType.Movie, "tt456");
        var mainViewModel = UnitTestHelpers.CreateUninitialized<MainViewModel>();
        UnitTestHelpers.SetPrivateField(mainViewModel, "<ReturnCommand>k__BackingField", new RelayCommand(() => { }));

        var viewModel = new DetailsViewModel(mainViewModel, request, false);

        Assert.AreEqual(Visibility.Visible, viewModel.LoadingVisibility);
    }

}
