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
    internal class ActorsViewModel
    {
        public ObservableCollection<Name> Actors { get; } = new ObservableCollection<Name>();

        private int _currentPage = 0;
        private const int PageSize = 50;

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        public ActorsViewModel()
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
            var actors = context.Names
                .Where(n => n.PrimaryProfession != null && n.PrimaryProfession.Contains("actor"))
                .OrderBy(n => n.NameId)
                .Skip(_currentPage * PageSize)
                .Take(PageSize)
                .ToList();

            Actors.Clear();
            foreach (var actor in actors)
            {
                Actors.Add(actor);
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
