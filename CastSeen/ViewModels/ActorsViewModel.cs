using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CastSeen.Commands;
using CastSeen.Data;
using Microsoft.EntityFrameworkCore;

namespace CastSeen.ViewModels
{
    internal class ActorsViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<ActorDisplay> Actors { get; } = new ObservableCollection<ActorDisplay>();
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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
        public ICommand OpenActorCommand { get; }

        public ActorsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);
            OpenActorCommand = new RelayCommand<ActorDisplay>(OpenActor);

            _ = LoadDataAsync();
        }

        public class ActorDisplay
        {
            public string NameId { get; set; }
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

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            IsLoading = true;
            try
            {
                using var context = new ImdbContext();
                var actors = await context.Names
                    .AsNoTracking()
                    .Where(n => n.PrimaryProfession != null && n.PrimaryProfession.Contains("actor"))
                    .OrderBy(n => n.NameId)
                    .Skip(_currentPage * PageSize)
                    .Take(PageSize)
                    .Select(n => new ActorDisplay
                    {
                        NameId = n.NameId,
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
                    .ToListAsync();

                Actors.Clear();
                foreach (var actor in actors)
                {
                    Actors.Add(actor);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        internal void NextPage()
        {
            _currentPage++;
            _ = LoadDataAsync();
        }

        internal bool CanNextPage() => !IsLoading;

        internal void PreviousPage()
        {
            _currentPage--;
            _ = LoadDataAsync();
        }

        internal bool CanPreviousPage() => _currentPage > 0 && !IsLoading;

        internal void OpenActor(ActorDisplay actor)
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.NameId)) return;

            _mainViewModel.NavigateToDetails(new DetailsNavigationRequest(DetailsTargetType.Actor, actor.NameId));
        }
    }
}
