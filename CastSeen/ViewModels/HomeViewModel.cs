using System;
using System.Collections.Generic;
using System.Text;
using CastSeen.Data;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace CastSeen.ViewModels
{
    internal class HomeViewModel
    {
        private readonly MainViewModel _mainVM;

        public int TotalMovies { get; }
        public int TotalActors { get; }
        public int TotalGenres { get; }


        public ICommand GoToMoviesCommand { get; }
        public ICommand GoToActorsCommand { get; }

        public HomeViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            GoToMoviesCommand = _mainVM.NavToMoviesCommand;
            GoToActorsCommand = _mainVM.NavToActorsCommand;

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            using var context = new ImdbContext();
            TotalMovies = context.Titles.Count(t => t.TitleType == "movie");
            TotalActors = context.Names.Count();
            TotalGenres = context.Genres.Count();
        }


    }
}
