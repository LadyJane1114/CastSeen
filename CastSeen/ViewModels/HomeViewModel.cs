using System;
using System.Collections.Generic;
using System.Text;
using CastSeen.Data;
using System.Linq;
using System.ComponentModel;
using System.Windows;

namespace CastSeen.ViewModels
{
    internal class HomeViewModel
    {
        public int TotalMovies { get; }
        public int TotalActors { get; }

        public HomeViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            using var context = new ImdbContext();
            TotalMovies = context.Titles.Count(t => t.TitleType == "movie");
            TotalActors = context.Names.Count();
        }
    }
}
