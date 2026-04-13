using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CastSeen.Data;
using CastSeen.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CastSeen.Commands;

namespace CastSeen.ViewModels
{
    internal class MoviesViewModel
    {
        public ObservableCollection<Title> Movies { get; } = new ObservableCollection<Title>();

        private int _currentPage = 0;
        private const int PageSize = 50;

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        public MoviesViewModel()
        {
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);

            LoadData();
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
    }
}
