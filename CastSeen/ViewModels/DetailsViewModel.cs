using CastSeen.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CastSeen.ViewModels
{
    internal class DetailsViewModel
    {
        public DetailsViewModel(MainViewModel mainViewModel, DetailsNavigationRequest request)
        {
            Request = request;
            BackCommand = mainViewModel.ReturnCommand;
            Identifier = request.Id;

            if (request.TargetType == DetailsTargetType.Actor)
            {
                ActorSectionVisibility = Visibility.Visible;
                MovieSectionVisibility = Visibility.Collapsed;
                LoadActorDetails(request.Id);
            }
            else
            {
                ActorSectionVisibility = Visibility.Collapsed;
                MovieSectionVisibility = Visibility.Visible;
                LoadMovieDetails(request.Id);
            }
        }

        public DetailsNavigationRequest Request { get; }
        public ICommand BackCommand { get; }

        public string Header { get; private set; } = "Details";
        public string Identifier { get; }
        public Visibility ActorSectionVisibility { get; }
        public Visibility MovieSectionVisibility { get; }

        public string ActorName { get; private set; } = "";
        public string ActorLifeYears { get; private set; } = "";
        public string ActorProfessions { get; private set; } = "";
        public int ActorProjectCount { get; private set; }
        public List<string> ActorKnownFor { get; private set; } = new();
        public List<string> ActorCredits { get; private set; } = new();

        public string MovieTitle { get; private set; } = "";
        public string MovieSubtitle { get; private set; } = "";
        public string MovieRatingText { get; private set; } = "No rating";
        public List<string> MovieGenres { get; private set; } = new();
        public List<string> MovieTopCast { get; private set; } = new();
        public List<string> MovieDirectors { get; private set; } = new();
        public List<string> MovieWriters { get; private set; } = new();

        private void LoadActorDetails(string nameId)
        {
            using var context = new ImdbContext();

            var actor = context.Names
                .Where(n => n.NameId == nameId)
                .Select(n => new
                {
                    n.PrimaryName,
                    n.BirthYear,
                    n.DeathYear,
                    n.PrimaryProfession,
                    KnownFor = n.TitlesNavigation
                        .Select(t => t.PrimaryTitle)
                        .Where(t => t != null)
                        .Take(6)
                        .ToList()
                })
                .FirstOrDefault();

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

            ActorProjectCount = context.Principals.Count(p => p.NameId == nameId);

            ActorKnownFor = actor.KnownFor;

            var actorCreditsRaw = context.Principals
                .Where(p => p.NameId == nameId)
                .Join(context.Titles,
                    p => p.TitleId,
                    t => t.TitleId,
                    (p, t) => new { t.PrimaryTitle, t.StartYear })
                .Where(x => x.PrimaryTitle != null)
                .OrderByDescending(x => x.StartYear)
                .Take(40)
                .ToList();

            ActorCredits = actorCreditsRaw
                .Select(x => x.StartYear == null ? x.PrimaryTitle! : $"{x.PrimaryTitle} ({x.StartYear})")
                .Distinct()
                .Take(20)
                .ToList();
        }

        private void LoadMovieDetails(string titleId)
        {
            using var context = new ImdbContext();

            var movie = context.Titles
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
                .FirstOrDefault();

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

            MovieTopCast = context.Principals
                .Where(p => p.TitleId == titleId)
                .Join(context.Names,
                    p => p.NameId,
                    n => n.NameId,
                    (p, n) => new { p.Ordering, n.PrimaryName })
                .OrderBy(x => x.Ordering)
                .Select(x => x.PrimaryName)
                .Where(n => n != null)
                .Distinct()
                .Take(12)
                .Select(n => n!)
                .ToList();
        }
    }
}
