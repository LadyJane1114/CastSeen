using CastSeen.Commands;
using CastSeen.Data;
using CastSeen.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CastSeen.ViewModels
{
    internal class MoviesViewModel
    {
        public ObservableCollection<MovieDisplay> Movies { get; } = new ObservableCollection<MovieDisplay>();

        private int _currentPage = 0;
        private const int PageSize = 50;

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand OpenMovieCommand { get; }

        public MoviesViewModel()
        {
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);
            OpenMovieCommand = new RelayCommand<MovieDisplay>(OpenMovie);

            LoadData();
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

        private void LoadData()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            using var context = new ImdbContext();
            var movies = context.Titles
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
                .ToList();

            Movies.Clear();
            foreach (var movie in movies)
            {
                Movies.Add(movie);
            }
        }

        private void NextPage()
        {
            _currentPage++;
            LoadData();
        }

        private bool CanNextPage() => true;

        private void PreviousPage()
        {
            _currentPage--;
            LoadData();
        }

        private bool CanPreviousPage() => _currentPage > 0;

        private void OpenMovie(MovieDisplay movie)
        {
            if (movie == null) return;

            // TEMP: just prove it's working
            MessageBox.Show($"Clicked: {movie.PrimaryTitle}");

            // TODO: replace this with real navigation
        }
    }
}
