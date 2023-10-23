using GoogleClientSample.Models;
using Plugin.GoogleClient.MAUI;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace GoogleClientSample.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public UserProfile User { get; set; } = new UserProfile();
        public string Name
        {
            get => User.Name;
            set => User.Name = value;
        }

        public string Email
        {
            get => User.Email;
            set => User.Email = value;
        }

        public Uri Picture
        {
            get => User.Picture;
            set => User.Picture = value;
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        private string _token;
        public string Token
        {
            get => _token;
            set => SetProperty(ref _token, value);
        }

        public ICommand LoginCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public MainPageViewModel()
        {
            LoginCommand = new Command(LoginAsync);
            LogoutCommand = new Command(Logout);

            IsLoggedIn = false;
        }

        public async void LoginAsync()
        {
            CrossGoogleClient.Current.OnLogin += OnLoginCompleted;
            try
            {
                await CrossGoogleClient.Current.LoginAsync();
            }
            catch (GoogleClientSignInNetworkErrorException e)
            {
                await App.Current.MainPage.DisplayAlert("Error", e.Message, "OK");
            }
            catch (GoogleClientSignInCanceledErrorException e)
            {
                await App.Current.MainPage.DisplayAlert("Error", e.Message, "OK");
            }
            catch (GoogleClientSignInInvalidAccountErrorException e)
            {
                await App.Current.MainPage.DisplayAlert("Error", e.Message, "OK");
            }
            catch (GoogleClientSignInInternalErrorException e)
            {
                await App.Current.MainPage.DisplayAlert("Error", e.Message, "OK");
            }
            catch (GoogleClientNotInitializedErrorException e)
            {
                await App.Current.MainPage.DisplayAlert("Error", e.Message, "OK");
            }
            catch (GoogleClientBaseException e)
            {
                await App.Current.MainPage.DisplayAlert("Error", e.Message, "OK");
            }

        }


        private void OnLoginCompleted(object sender, GoogleClientResultEventArgs<GoogleUser> loginEventArgs)
        {
            if (loginEventArgs.Data != null)
            {
                GoogleUser googleUser = loginEventArgs.Data;
                User.Name = googleUser.Name;
                User.Email = googleUser.Email;
                User.Picture = googleUser.Picture;
                var GivenName = googleUser.GivenName;
                var FamilyName = googleUser.FamilyName;


                // Log the current User email
                Debug.WriteLine(User.Email);
                IsLoggedIn = true;

                var token = CrossGoogleClient.Current.AccessToken;
                Token = token;
            }
            else
            {
                App.Current.MainPage.DisplayAlert("Error", loginEventArgs.Message, "OK");
            }

            CrossGoogleClient.Current.OnLogin -= OnLoginCompleted;

        }

        public void Logout()
        {
            CrossGoogleClient.Current.OnLogout += OnLogoutCompleted;
            CrossGoogleClient.Current.Logout();
        }

        private void OnLogoutCompleted(object sender, EventArgs loginEventArgs)
        {
            IsLoggedIn = false;
            User.Email = "Offline";
            CrossGoogleClient.Current.OnLogout -= OnLogoutCompleted;
        }
    }
}
