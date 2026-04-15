using CastSeen.Commands;
using CastSeen.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
        public ObservableCollection<string> Genres { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> TopGenres { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> OtherGenres { get; } = new ObservableCollection<string>();

        private int _currentPage = 0;
        private const int PageSize = 50;
        private int _searchVersion;
        /*Actors count and reducers*/
        public int MatchingMovies => Movies.Count;
        private int _totalMovies = 0;
        public int TotalMovies
        {
            get => _totalMovies;
            set { _totalMovies = value; OnPropertyChanged(nameof(TotalMovies)); }
        }
        
        private string _searchTerm = string.Empty;
        private string? _selectedGenre;
        private string _sortColumn = "TitleId";
        private bool _sortDescending = false;

        public string SortColumn
        {
            get => _sortColumn;
            set { _sortColumn = value; OnPropertyChanged(nameof(SortColumn)); }
        }

        public bool SortDescending
        {
            get => _sortDescending;
            set { _sortDescending = value; OnPropertyChanged(nameof(SortDescending)); }
        }

        public string? SelectedGenre
        {
            get => _selectedGenre;
            set
            {
                if (_selectedGenre == value) return;
                _selectedGenre = value;
                OnPropertyChanged(nameof(SelectedGenre));
                _currentPage = 0;
                _ = SearchAsync(SearchQuery);
            }
        }
        
        public string SearchQuery
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm == value) return;
                _searchTerm = value;
                OnPropertyChanged(nameof(SearchQuery));
                _ = SearchAsync(value);
            }
        }

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
        public ICommand SelectGenreCommand { get; }
        public ICommand SortCommand { get; }

        public MoviesViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);
            OpenMovieCommand = new RelayCommand<MovieDisplay>(OpenMovie);
            SelectGenreCommand = new RelayCommand<string>(SelectGenre);
            SortCommand = new RelayCommand<string>(Sort);

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

            if (!Genres.Any())
            {
                try
                {
                    using var context = new ImdbContext();
                    var genreList = await context.Genres
                        .OrderBy(g => g.Name)
                        .Select(g => g.Name)
                        .ToListAsync();

                    Genres.Clear();
                    TopGenres.Clear();
                    OtherGenres.Clear();

                    Genres.Add("All Genres");
                    foreach (var g in genreList) Genres.Add(g);

                    var top = genreList.Take(4).ToList();
                    foreach (var g in top) TopGenres.Add(g);

                    var other = genreList.Skip(4).ToList();
                    OtherGenres.Add("Other Genres");
                    foreach (var g in other) OtherGenres.Add(g);
                    
                    _selectedGenre = "All Genres";
                    OnPropertyChanged(nameof(SelectedGenre));
                }
                catch
                {
                    //TODO: Should probably be better
                    MessageBox.Show("Failed to load genres");
                }
            }

            _ = SearchAsync(SearchQuery);
        }

      
        private void SelectGenre(string genre)
        {
            SelectedGenre = genre;
        }

        private void Sort(string column)
        {
            if (SortColumn == column)
            {
                SortDescending = !SortDescending;
            }
            else
            {
                SortColumn = column;
                SortDescending = false;
                if (column == "Rating") SortDescending = true; // Default rating to descending (highest first)
            }
            _currentPage = 0;
            _ = SearchAsync(SearchQuery);
        }

        public async Task SearchAsync(string searchTerm)
        {
            var version = Interlocked.Increment(ref _searchVersion);
            _searchTerm = searchTerm;
            IsLoading = true;
            Movies.Clear();
            TotalMovies = 0;
            OnPropertyChanged(nameof(MatchingMovies));

            try
            {
                await Task.Delay(250);
                if (version != _searchVersion)
                    return;

                using var context = new ImdbContext();
                var query = context.Titles
                    .AsNoTracking()
                    .Where(t => (string.IsNullOrWhiteSpace(searchTerm) || t.PrimaryTitle!.Contains(searchTerm)) && t.TitleType == "movie");

                if (!string.IsNullOrEmpty(SelectedGenre) && SelectedGenre != "All Genres" && SelectedGenre != "Other Genres")
                {
                    query = query.Where(t => t.Genres.Any(g => g.Name == SelectedGenre));
                }

                // Apply Sorting
                query = SortColumn switch
                {
                    "Rating" => SortDescending 
                        ? query.OrderByDescending(t => t.Rating.AverageRating).ThenBy(t => t.PrimaryTitle)
                        : query.OrderBy(t => t.Rating.AverageRating).ThenBy(t => t.PrimaryTitle),
                    "Year" => SortDescending 
                        ? query.OrderByDescending(t => t.StartYear).ThenBy(t => t.PrimaryTitle)
                        : query.OrderBy(t => t.StartYear).ThenBy(t => t.PrimaryTitle),
                    _ => SortDescending 
                        ? query.OrderByDescending(t => t.PrimaryTitle)
                        : query.OrderBy(t => t.PrimaryTitle)
                };

                TotalMovies = await query.CountAsync();
                var movies = await query
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
                    })
                    .ToListAsync();

                if (version != _searchVersion)
                    return;

                foreach (var movie in movies)
                {
                    Movies.Add(movie);
                }
                OnPropertyChanged(nameof(MatchingMovies));
            }
            finally
            {
                if (version == _searchVersion)
                    IsLoading = false;
            }
        }

        internal void NextPage()
        {
            _currentPage++;
            _ = LoadDataAsync();
        }

        internal bool CanNextPage() => !IsLoading && MatchingMovies >= PageSize;

        internal void PreviousPage()
        {
            _currentPage--;
            _ = LoadDataAsync();
        }

        internal bool CanPreviousPage() => _currentPage > 0 && !IsLoading;

        internal void OpenMovie(MovieDisplay movie)
        {
            if (movie == null || string.IsNullOrWhiteSpace(movie.TitleId)) return;

            _mainViewModel.NavigateToDetails(new DetailsNavigationRequest(DetailsTargetType.Movie, movie.TitleId));
        }
    }
}
