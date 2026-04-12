using CastSeen.Commands;
using CastSeen.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace CastSeen.ViewModels
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;


        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel != value)
                {
                    _currentViewModel = value;
                    OnPropertyChanged(nameof(CurrentViewModel));
                }
            }
        }


        private readonly Stack<object> _history = new Stack<object>();

        public class NavItem
        {
            public string Label { get; set; }
            public string Icon { get; set; }
            public ICommand Command { get; set; }
        }


        public ObservableCollection<NavItem> NavItems { get; }


        public MainViewModel()
        {
            NavToHomeCommand = new RelayCommand(() => Navigate(() => new HomeView()));
            NavToMoviesCommand = new RelayCommand(() => Navigate(() => new MoviesView()));
            NavToActorsCommand = new RelayCommand(() => Navigate(() => new ActorsView()));
            ReturnCommand = new RelayCommand(() => Return());
            ExitCommand = new RelayCommand(() => App.Current.Shutdown());

            NavItems = new ObservableCollection<NavItem>
            {
                new NavItem
                {
                    Label = "Home",
                    Icon = "/Resources/home.png",
                    Command = NavToHomeCommand
                },
                new NavItem
                {
                    Label = "Movies",
                    Icon = "/Resources/movies.png",
                    Command = NavToMoviesCommand
                },
                new NavItem
                {
                    Label = "Actors",
                    Icon = "/Resources/actors.png",
                    Command = NavToActorsCommand
                }
            };

            CurrentViewModel = new HomeView();
        }

        public ICommand NavToHomeCommand { get; }
        public ICommand NavToMoviesCommand { get; }
        public ICommand NavToActorsCommand { get; }
        public ICommand ReturnCommand { get; }
        public ICommand ExitCommand { get; }


        private void Navigate(Func<object> createView)
        {
            if (CurrentViewModel != null)
                _history.Push(CurrentViewModel);

            CurrentViewModel = createView();
        }

        private void Return()
        {
            if (_history.Count > 0)
                CurrentViewModel = _history.Pop();
            else
                CurrentViewModel = new HomeView();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
