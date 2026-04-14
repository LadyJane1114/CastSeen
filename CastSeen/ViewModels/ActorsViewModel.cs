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
    internal class ActorsViewModel :INotifyPropertyChanged
    {
        public ObservableCollection<ActorDisplay> Actors { get; } = new ObservableCollection<ActorDisplay>();
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int _currentPage = 0;
        private const int PageSize = 50;

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand OpenActorCommand { get; }

        public ActorsViewModel()
        {
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);
            OpenActorCommand = new RelayCommand<Name>(OpenActor);

            LoadData();
        }
        public class ActorDisplay
        {
            public string Name { get; set; }
            public int? BirthYear { get; set; }
            public int ProjectCount { get; set; }
            public List<string> SampleProjects { get; set; }
            public string Initials
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(Name))
                        return "?";

                    var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 1)
                        return parts[0][0].ToString().ToUpper();

                    return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
                }
            }

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
                .Select(n => new ActorDisplay
                {
                    Name = n.PrimaryName,
                    BirthYear = n.BirthYear,
                    ProjectCount = context.Principals.Count(tp => tp.NameId == n.NameId),
                    SampleProjects = context.Principals
                    .Where(tp => tp.NameId == n.NameId)
                    .Join(context.Titles,
                    tp => tp.TitleId,
                    tb => tb.TitleId,
                    (tp, tb) => tb.PrimaryTitle)
                    .Take(1)
                    .ToList()
                })
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

        private void OpenActor(Name actor)
        {
            if (actor == null) return;

            // TEMP: just prove it's working
            MessageBox.Show($"Clicked: {actor.PrimaryName}");

            // TODO: replace this with real navigation
        }
    }
}
