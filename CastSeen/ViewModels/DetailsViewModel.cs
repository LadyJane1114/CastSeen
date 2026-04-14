using CastSeen.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CastSeen.ViewModels
{
    internal class DetailsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public DetailsViewModel(MainViewModel mainViewModel, DetailsNavigationRequest request)
        {
            Request = request;
            BackCommand = mainViewModel.ReturnCommand;
            Identifier = request.Id;

            if (request.TargetType == DetailsTargetType.Actor)
            {
                ActorSectionVisibility = Visibility.Visible;
                MovieSectionVisibility = Visibility.Collapsed;
            }
            else
            {
                ActorSectionVisibility = Visibility.Collapsed;
                MovieSectionVisibility = Visibility.Visible;
            }

            _ = LoadDetailsAsync(request);
        }

        public DetailsNavigationRequest Request { get; }
        public ICommand BackCommand { get; }

        private string _header = "Details";
        public string Header
        {
            get => _header;
            private set => SetProperty(ref _header, value);
        }

        public string Identifier { get; }
        public Visibility ActorSectionVisibility { get; }
        public Visibility MovieSectionVisibility { get; }

        private Visibility _loadingVisibility = Visibility.Visible;
        public Visibility LoadingVisibility
        {
            get => _loadingVisibility;
            private set => SetProperty(ref _loadingVisibility, value);
        }

        private string _actorName = "";
        public string ActorName
        {
            get => _actorName;
            private set => SetProperty(ref _actorName, value);
        }

        private string _actorLifeYears = "";
        public string ActorLifeYears
        {
            get => _actorLifeYears;
            private set => SetProperty(ref _actorLifeYears, value);
        }

        private string _actorProfessions = "";
        public string ActorProfessions
        {
            get => _actorProfessions;
            private set => SetProperty(ref _actorProfessions, value);
        }

        private int _actorProjectCount;
        public int ActorProjectCount
        {
            get => _actorProjectCount;
            private set => SetProperty(ref _actorProjectCount, value);
        }

        private List<string> _actorKnownFor = new();
        public List<string> ActorKnownFor
        {
            get => _actorKnownFor;
            private set => SetProperty(ref _actorKnownFor, value);
        }

        private List<string> _actorCredits = new();
        public List<string> ActorCredits
        {
            get => _actorCredits;
            private set => SetProperty(ref _actorCredits, value);
        }

        private string _movieTitle = "";
        public string MovieTitle
        {
            get => _movieTitle;
            private set => SetProperty(ref _movieTitle, value);
        }

        private string _movieSubtitle = "";
        public string MovieSubtitle
        {
            get => _movieSubtitle;
            private set => SetProperty(ref _movieSubtitle, value);
        }

        private string _movieRatingText = "No rating";
        public string MovieRatingText
        {
            get => _movieRatingText;
            private set => SetProperty(ref _movieRatingText, value);
        }

        private List<string> _movieGenres = new();
        public List<string> MovieGenres
        {
            get => _movieGenres;
            private set => SetProperty(ref _movieGenres, value);
        }

        private List<string> _movieTopCast = new();
        public List<string> MovieTopCast
        {
            get => _movieTopCast;
            private set => SetProperty(ref _movieTopCast, value);
        }

        private List<string> _movieDirectors = new();
        public List<string> MovieDirectors
        {
            get => _movieDirectors;
            private set => SetProperty(ref _movieDirectors, value);
        }

        private List<string> _movieWriters = new();
        public List<string> MovieWriters
        {
            get => _movieWriters;
            private set => SetProperty(ref _movieWriters, value);
        }

        private async System.Threading.Tasks.Task LoadDetailsAsync(DetailsNavigationRequest request)
        {
            try
            {
                if (request.TargetType == DetailsTargetType.Actor)
                {
                    await LoadActorDetailsAsync(request.Id);
                }
                else
                {
                    await LoadMovieDetailsAsync(request.Id);
                }
            }
            catch
            {
                Header = request.TargetType == DetailsTargetType.Actor ? "Actor Details" : "Movie Details";

                if (request.TargetType == DetailsTargetType.Actor)
                {
                    ActorName = "Unable to load actor details";
                }
                else
                {
                    MovieTitle = "Unable to load movie details";
                }
            }
            finally
            {
                LoadingVisibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task LoadActorDetailsAsync(string nameId)
        {
            using var context = new ImdbContext();

            var actor = await context.Names
                .AsNoTracking()
                .Where(n => n.NameId == nameId)
                .Select(n => new
                {
                    n.PrimaryName,
                    n.BirthYear,
                    n.DeathYear,
                    n.PrimaryProfession
                })
                .FirstOrDefaultAsync();

            if (actor == null)
            {
                Header = "Actor Details";
                ActorName = "Actor not found";
                return;
            }

            Header = "Actor Details";
            ActorName = actor.PrimaryName ?? "Unknown";
            ActorLifeYears = actor.BirthYear == null && actor.DeathYear == null
                ? "Years unknown"
                : $"{actor.BirthYear?.ToString() ?? "?"} - {actor.DeathYear?.ToString() ?? "Present"}";
            ActorProfessions = string.IsNullOrWhiteSpace(actor.PrimaryProfession) ? "Unknown" : actor.PrimaryProfession;

            ActorProjectCount = await context.Principals
                .AsNoTracking()
                .CountAsync(p => p.NameId == nameId);

            var actorCreditsRaw = await context.Principals
                .AsNoTracking()
                .Where(p => p.NameId == nameId)
                .Join(context.Titles.AsNoTracking(),
                    p => p.TitleId,
                    t => t.TitleId,
                    (p, t) => new { t.PrimaryTitle, t.StartYear })
                .Where(x => x.PrimaryTitle != null)
                .OrderByDescending(x => x.StartYear)
                .Take(60)
                .ToListAsync();

            ActorKnownFor = actorCreditsRaw
                .Select(x => x.PrimaryTitle!)
                .Distinct()
                .Take(6)
                .ToList();

            ActorCredits = actorCreditsRaw
                .Select(x => x.StartYear == null ? x.PrimaryTitle! : $"{x.PrimaryTitle} ({x.StartYear})")
                .Distinct()
                .Take(20)
                .ToList();
        }

        private async System.Threading.Tasks.Task LoadMovieDetailsAsync(string titleId)
        {
            using var context = new ImdbContext();

            var movie = await context.Titles
                .AsNoTracking()
                .Where(t => t.TitleId == titleId)
                .Select(t => new
                {
                    t.PrimaryTitle,
                    t.StartYear,
                    t.RuntimeMinutes,
                    t.TitleType,
                    Genres = t.Genres.Select(g => g.Name).ToList(),
                    Directors = t.Names.Select(n => n.PrimaryName).Where(n => n != null).ToList(),
                    Writers = t.Names1.Select(n => n.PrimaryName).Where(n => n != null).ToList(),
                    Rating = t.Rating != null ? t.Rating.AverageRating : null,
                    Votes = t.Rating != null ? t.Rating.NumVotes : null
                })
                .FirstOrDefaultAsync();

            if (movie == null)
            {
                Header = "Movie Details";
                MovieTitle = "Movie not found";
                return;
            }

            Header = "Movie Details";
            MovieTitle = movie.PrimaryTitle ?? "Unknown";
            MovieSubtitle = $"{movie.TitleType ?? "title"} • {movie.StartYear?.ToString() ?? "year unknown"} • {movie.RuntimeMinutes?.ToString() ?? "?"} min";
            MovieGenres = movie.Genres;
            MovieDirectors = movie.Directors.Select(n => n!).Distinct().ToList();
            MovieWriters = movie.Writers.Select(n => n!).Distinct().ToList();

            MovieRatingText = movie.Rating == null
                ? "No rating"
                : $"{movie.Rating:0.0} ({movie.Votes?.ToString("N0") ?? "0"} votes)";

            MovieTopCast = await context.Principals
                .AsNoTracking()
                .Where(p => p.TitleId == titleId)
                .Join(context.Names.AsNoTracking(),
                    p => p.NameId,
                    n => n.NameId,
                    (p, n) => new { p.Ordering, n.PrimaryName })
                .OrderBy(x => x.Ordering)
                .Select(x => x.PrimaryName)
                .Where(n => n != null)
                .Distinct()
                .Take(12)
                .Select(n => n!)
                .ToListAsync();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            OnPropertyChanged(propertyName);
        }
    }
}
