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
using System.Windows.Media;

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
            public Geometry Icon { get; set; }
            public ICommand Command { get; set; }
        }


        public ObservableCollection<NavItem> NavItems { get; }


        public MainViewModel()
        {
            NavToHomeCommand = new RelayCommand(() => Navigate(() => new HomeViewModel(this)));
            NavToMoviesCommand = new RelayCommand(() => Navigate(() => new MoviesViewModel(this)));
            NavToActorsCommand = new RelayCommand(() => Navigate(() => new ActorsViewModel(this)));
            ReturnCommand = new RelayCommand(() => Return());
            ExitCommand = new RelayCommand(() => App.Current.Shutdown());

            NavItems = new ObservableCollection<NavItem>
            {
                new NavItem
                {
                    Label = "Home",
                    Icon = Geometry.Parse("M264-216h96v-240h240v240h96v-348L480-726 264-564v348Zm-72 72v-456l288-216 288 216v456H528v-240h-96v240H192Zm288-327Z"),
                    Command = NavToHomeCommand
                },
                new NavItem
                {
                    Label = "Movies",
                    Icon = Geometry.Parse("m216-768 72 144h96l-72-144h72l72 144h96l-72-144h72l72 144h96l-72-144h144q29.7 0 50.85 21.5Q864-725 864-696v432q0 29-21.15 50.5T792-192H168q-29 0-50.5-21.5T96-264v-432q0-29 20.5-50.5T168-768h48Zm-48 216v288h624v-288H168Zm0 0v288-288Z"),
                    Command = NavToMoviesCommand
                },
                new NavItem
                {
                    Label = "Actors",
                    Icon = Geometry.Parse("M0-240v-59q0-51 45-80t123-29q15 0 30 1.5t30 4.5q-17 20-26.5 45t-9.5 50.56V-240H0Zm240 0v-61q0-27.86 14.5-50.93T293-387q44-22 91-33.5t95.53-11.5Q529-432 576-420.5t91 33.5q24 12 38.5 35.07T720-301v61H240Zm528 0v-67.37q0-26.95-9.5-50.79T732-402q17-3 31.5-4.5T792-408q78 0 123 29t45 80v59H768Zm-454-72h332q-7-17-59.5-32.5T480-360q-54 0-106.5 15.5T314-312ZM167.79-456Q138-456 117-477.03q-21-21.02-21-50.55Q96-558 117.03-579q21.02-21 50.55-21Q198-600 219-579.24t21 51.45Q240-498 219.24-477t-51.45 21Zm624 0Q762-456 741-477.03q-21-21.02-21-50.55Q720-558 741.03-579q21.02-21 50.55-21Q822-600 843-579.24t21 51.45Q864-498 843.24-477t-51.45 21ZM479.5-480q-49.5 0-84.5-35t-35-85q0-50 35-85t85-35q50 0 85 35t35 85.5q0 49.5-35 84.5t-85.5 35Zm.5-72q20.4 0 34.2-13.8Q528-579.6 528-600q0-20.4-13.8-34.2Q500.4-648 480-648q-20.4 0-34.2 13.8Q432-620.4 432-600q0 20.4 13.8 34.2Q459.6-552 480-552Zm0 240Zm0-288Z"),
                    Command = NavToActorsCommand
                }
            };

            CurrentViewModel = new HomeViewModel(this);
        }

        public ICommand NavToHomeCommand { get; }
        public ICommand NavToMoviesCommand { get; }
        public ICommand NavToActorsCommand { get; }
        public ICommand ReturnCommand { get; }
        public ICommand ExitCommand { get; }

        public void NavigateToDetails(DetailsNavigationRequest request)
        {
            Navigate(() => new DetailsViewModel(this, request));
        }

        internal void Navigate(Func<object> createView)
        {
            if (CurrentViewModel != null)
                _history.Push(CurrentViewModel);

            CurrentViewModel = createView();
        }
        private NavItem _selectedNavItem;
        public NavItem SelectedNavItem
        {
            get => _selectedNavItem;
            set
            {
                _selectedNavItem = value;
                OnPropertyChanged(nameof(SelectedNavItem));
                _selectedNavItem?.Command?.Execute(null);
            }
        }

        internal void Return()
        {
            if (_history.Count > 0)
                CurrentViewModel = _history.Pop();
            else
                CurrentViewModel = new HomeViewModel(this);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
