using CastSeen.Commands;
using CastSeen.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CastSeen.ViewModels
{
    internal class MoviesViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _mainViewModel;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<MovieDisplay> Movies { get; } = new ObservableCollection<MovieDisplay>();

        private int _currentPage = 0;
        private const int PageSize = 50;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(LoadingVisibility));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand OpenMovieCommand { get; }

        public MoviesViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);
            OpenMovieCommand = new RelayCommand<MovieDisplay>(OpenMovie);

            _ = LoadDataAsync();
        }

        public class MovieDisplay
        {
            public string TitleId { get; set; }
            public string PrimaryTitle { get; set; }
            public int? StartYear { get; set; }
            public List<string> Genres { get; set; } = new();
            public List<string> TopActors { get; set; } = new();
            public double? Rating { get; set; }

            public string TopActorsDisplay =>
                TopActors == null ? "" : string.Join(", ", TopActors);
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            IsLoading = true;
            try
            {
                using var context = new ImdbContext();
                var movies = await context.Titles
                    .AsNoTracking()
                    .Where(t => t.TitleType == "movie")
                    .OrderBy(t => t.TitleId)
                    .Skip(_currentPage * PageSize)
                    .Take(PageSize)
                    .Select(t => new MovieDisplay
                    {
                        TitleId = t.TitleId,
                        PrimaryTitle = t.PrimaryTitle,
                        StartYear = t.StartYear,
                        Genres = t.Genres
                            .Select(g => g.Name)
                            .ToList(),
                        Rating = context.Ratings
                            .Where(r => r.TitleId == t.TitleId)
                            .Select(r => (double?)r.AverageRating)
                            .FirstOrDefault(),
                        TopActors = context.Principals
                            .Where(p => p.TitleId == t.TitleId)
                            .Join(context.Names,
                                p => p.NameId,
                                n => n.NameId,
                                (p, n) => n.PrimaryName)
                            .Distinct()
                            .Take(3)
                            .ToList()
                            .ToList()
                    })
                    .ToListAsync();

                Movies.Clear();
                foreach (var movie in movies)
                {
                    Movies.Add(movie);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NextPage()
        {
            _currentPage++;
            _ = LoadDataAsync();
        }

        private bool CanNextPage() => !IsLoading;

        private void PreviousPage()
        {
            _currentPage--;
            _ = LoadDataAsync();
        }

        private bool CanPreviousPage() => _currentPage > 0 && !IsLoading;

        private void OpenMovie(MovieDisplay movie)
        {
            if (movie == null || string.IsNullOrWhiteSpace(movie.TitleId)) return;

            _mainViewModel.NavigateToDetails(new DetailsNavigationRequest(DetailsTargetType.Movie, movie.TitleId));
        }
    }
}
