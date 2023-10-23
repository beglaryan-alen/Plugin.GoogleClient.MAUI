using System;
using System.Threading.Tasks;
using System.Linq;
#if ANDROID
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Tasks;
using Java.Interop;
using Android.Gms.Auth;
using Object = Java.Lang.Object;
using Android.Gms.Auth.Api.Credentials;

#elif IOS
using Foundation;
using Google.SignIn;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
#endif

namespace Plugin.GoogleClient.MAUI
{
#if ANDROID
    /// <summary>
    /// Implementation for GoogleClient
    /// </summary>
    public partial class GoogleClientManager : Java.Lang.Object, IGoogleClientManager, IOnCompleteListener
    {
        // Class Debug Tag
        static string Tag = typeof(GoogleClientManager).FullName;
        static int AuthActivityID = 9637;
        public static Activity CurrentActivity { get; set; }
        static TaskCompletionSource<GoogleResponse<GoogleUser>> _loginTcs;
        static string _serverClientId;
        static string _clientId;
        static string[] _initScopes = new string[0];

        GoogleSignInClient mGoogleSignInClient;

        public GoogleUser CurrentUser
        {
            get
            {
                GoogleSignInAccount userAccount = GoogleSignIn.GetLastSignedInAccount(CurrentActivity);
                return userAccount != null ? new GoogleUser
                {
                    Id = userAccount.Id,
                    Name = userAccount.DisplayName,
                    GivenName = userAccount.GivenName,
                    FamilyName = userAccount.FamilyName,
                    Email = userAccount.Email,
                    Picture = new Uri((userAccount.PhotoUrl != null ? $"{userAccount.PhotoUrl}" : $"https://autisticdating.net/imgs/profile-placeholder.jpg"))
                } : null;
            }
        }

        static readonly string[] DefaultScopes = new[]
        {
            Scopes.Profile
        };

        internal GoogleClientManager()
        {
            if (CurrentActivity == null)
            {
                throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
            }

            var gopBuilder = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                    .RequestEmail();

            if (!string.IsNullOrWhiteSpace(_serverClientId))
            {
                gopBuilder.RequestServerAuthCode(_serverClientId, false);
            }

            if (!string.IsNullOrWhiteSpace(_clientId))
            {
                gopBuilder.RequestIdToken(_clientId);
            }

            foreach (var s in _initScopes)
            {
                gopBuilder.RequestScopes(new Scope(s));
            }

            GoogleSignInOptions googleSignInOptions = gopBuilder.Build();

            // Build a GoogleSignInClient with the options specified by gso.
            mGoogleSignInClient = GoogleSignIn.GetClient(CurrentActivity, googleSignInOptions);
        }

        public static void Initialize(
            Activity activity,
            string serverClientId = null,
            string clientId = null,
            string[] scopes = null,
            int requestCode = 9637)
        {
            CurrentActivity = activity;
            _serverClientId = serverClientId;
            _clientId = clientId;
            _initScopes = DefaultScopes.Concat(scopes ?? new string[0]).ToArray();
            AuthActivityID = requestCode;
        }

        EventHandler<GoogleClientResultEventArgs<GoogleUser>> _onLogin;
        public event EventHandler<GoogleClientResultEventArgs<GoogleUser>> OnLogin
        {
            add => _onLogin += value;
            remove => _onLogin -= value;
        }

        public async Task<GoogleResponse<GoogleUser>> LoginAsync()
        {
            if (CurrentActivity == null || mGoogleSignInClient == null)
            {
                throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
            }

            _loginTcs = new TaskCompletionSource<GoogleResponse<GoogleUser>>();

            GoogleSignInAccount account = GoogleSignIn.GetLastSignedInAccount(CurrentActivity);


            if (account != null)
            {
                OnSignInSuccessful(account);
            }
            else
            {
                Intent intent = mGoogleSignInClient.SignInIntent;
                CurrentActivity?.StartActivityForResult(intent, AuthActivityID);
            }

            return await _loginTcs.Task;
        }

        public async Task<GoogleResponse<GoogleUser>> SilentLoginAsync()
        {

            if (CurrentActivity == null || mGoogleSignInClient == null)
            {
                throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
            }

            _loginTcs = new TaskCompletionSource<GoogleResponse<GoogleUser>>();

            GoogleSignInAccount account = GoogleSignIn.GetLastSignedInAccount(CurrentActivity);

            if (account != null)
            {
                OnSignInSuccessful(account);
            }
            else
            {
                GoogleSignInAccount userAccount = await mGoogleSignInClient.SilentSignInAsync();
                OnSignInSuccessful(userAccount);
            }

            return await _loginTcs.Task;
        }


        EventHandler _onLogout;
        public event EventHandler OnLogout
        {
            add => _onLogout += value;
            remove => _onLogout -= value;
        }

        protected virtual void OnLogoutCompleted(EventArgs e) => _onLogout?.Invoke(this, e);

        public void Logout()
        {
            if (CurrentActivity == null || mGoogleSignInClient == null)
            {
                throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
            }

            if (GoogleSignIn.GetLastSignedInAccount(CurrentActivity) != null)
            {
                //Auth.GoogleSignInApi.SignOut(GoogleApiClient);
                _idToken = null;
                _accessToken = null;
                mGoogleSignInClient.SignOut();
                //GoogleApiClient.Disconnect();

                // Log the state of the client
                //Debug.WriteLine(Tag + ": The user has logged out succesfully? " + !GoogleApiClient.IsConnected);

                // Send the logout result to the receivers
                OnLogoutCompleted(EventArgs.Empty);
            }
        }

        public bool IsLoggedIn
        {
            get
            {
                if (CurrentActivity == null)
                {
                    throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
                }
                return GoogleSignIn.GetLastSignedInAccount(CurrentActivity) != null;
            }
        }

        public string IdToken { get { return _idToken; } }
        public string AccessToken { get { return _accessToken; } }
        static string _idToken { get; set; }
        static string _accessToken { get; set; }

        EventHandler<GoogleClientErrorEventArgs> _onError;
        public event EventHandler<GoogleClientErrorEventArgs> OnError
        {
            add => _onError += value;
            remove => _onError -= value;
        }

        protected virtual void OnGoogleClientError(GoogleClientErrorEventArgs e) => _onError?.Invoke(this, e);

        public static void OnAuthCompleted(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode != AuthActivityID)
            {
                return;
            }
            GoogleSignIn.GetSignedInAccountFromIntent(data).AddOnCompleteListener(CrossGoogleClient.Current as IOnCompleteListener);

        }

        async void OnSignInSuccessful(GoogleSignInAccount userAccount)
        {
            GoogleUser googleUser = new GoogleUser
            {
                Id = userAccount.Id,
                Name = userAccount.DisplayName,
                GivenName = userAccount.GivenName,
                FamilyName = userAccount.FamilyName,
                Email = userAccount.Email,
                Picture = new Uri((userAccount.PhotoUrl != null ? $"{userAccount.PhotoUrl}" : $"https://autisticdating.net/imgs/profile-placeholder.jpg"))
            };

            _idToken = userAccount.IdToken;
            System.Console.WriteLine($"Id Token: {_idToken}");


            if (userAccount.GrantedScopes != null && userAccount.GrantedScopes.Count > 0)
            {
                var scopes = $"oauth2:{string.Join(' ', userAccount.GrantedScopes.Select(s => s.ScopeUri).ToArray())}";
                System.Console.WriteLine($"Scopes: {scopes}");
                var tcs = new TaskCompletionSource<string>();
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        tcs.TrySetResult(GoogleAuthUtil.GetToken(Android.App.Application.Context, userAccount.Account, scopes));
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetResult(string.Empty);
                        System.Console.WriteLine($"Ex: {ex}");
                    }
                });

                _accessToken = await tcs.Task;

                System.Console.WriteLine($"Access Token: {_accessToken}");
            }

            var googleArgs =
                new GoogleClientResultEventArgs<GoogleUser>(googleUser, GoogleActionStatus.Completed);

            // Send the result to the receivers
            _onLogin?.Invoke(CrossGoogleClient.Current, googleArgs);
            _loginTcs.TrySetResult(new GoogleResponse<GoogleUser>(googleArgs));
        }

        void OnSignInFailed(ApiException apiException)
        {
            GoogleClientErrorEventArgs errorEventArgs = new GoogleClientErrorEventArgs();
            Exception exception = null;

            switch (apiException.StatusCode)
            {
                case GoogleSignInStatusCodes.InternalError:
                    errorEventArgs.Error = GoogleClientErrorType.SignInInternalError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInInternalErrorMessage;
                    exception = new GoogleClientSignInInternalErrorException();
                    break;
                case GoogleSignInStatusCodes.ApiNotConnected:
                    errorEventArgs.Error = GoogleClientErrorType.SignInApiNotConnectedError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInApiNotConnectedErrorMessage;
                    exception = new GoogleClientSignInApiNotConnectedErrorException();
                    break;
                case GoogleSignInStatusCodes.NetworkError:
                    errorEventArgs.Error = GoogleClientErrorType.SignInNetworkError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInNetworkErrorMessage;
                    exception = new GoogleClientSignInNetworkErrorException();
                    break;
                case GoogleSignInStatusCodes.InvalidAccount:
                    errorEventArgs.Error = GoogleClientErrorType.SignInInvalidAccountError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInInvalidAccountErrorMessage;
                    exception = new GoogleClientSignInInvalidAccountErrorException();
                    break;
                case GoogleSignInStatusCodes.SignInRequired:
                    errorEventArgs.Error = GoogleClientErrorType.SignInRequiredError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInRequiredErrorMessage;
                    exception = new GoogleClientSignInRequiredErrorErrorException();
                    break;
                case GoogleSignInStatusCodes.SignInFailed:
                    errorEventArgs.Error = GoogleClientErrorType.SignInFailedError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInFailedErrorMessage;
                    exception = new GoogleClientSignInFailedErrorException();
                    break;
                case GoogleSignInStatusCodes.SignInCancelled:
                    errorEventArgs.Error = GoogleClientErrorType.SignInCanceledError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInCanceledErrorMessage;
                    exception = new GoogleClientSignInCanceledErrorException();
                    break;
                default:
                    errorEventArgs.Error = GoogleClientErrorType.SignInDefaultError;
                    errorEventArgs.Message = apiException.Message;
                    exception = new GoogleClientBaseException(
                        string.IsNullOrEmpty(apiException.Message)
                            ? GoogleClientBaseException.SignInDefaultErrorMessage
                            : apiException.Message
                        );
                    break;
            }

            _onError?.Invoke(CrossGoogleClient.Current, errorEventArgs);
            _loginTcs.TrySetException(exception);
        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (!task.IsSuccessful)
            {
                //Failed
                OnSignInFailed(task.Exception.JavaCast<ApiException>());
            }
            else
            {
                var userAccount = task.Result.JavaCast<GoogleSignInAccount>();

                OnSignInSuccessful(userAccount);

            }


        }
    }

#elif IOS
public partial class GoogleClientManager : NSObject, IGoogleClientManager, ISignInDelegate
    {
        // Class Debug Tag
        private String Tag = typeof(GoogleClientManager).FullName;

        public string IdToken { get { return _idToken; } }
        public string AccessToken { get { return _accessToken; } }
        static string _idToken { get; set; }
        static string _accessToken { get; set; }
        static string _clientId { get; set; }

        public GoogleUser CurrentUser
        {
            get
            {
                if (SignIn.SharedInstance.HasPreviousSignIn)
                    SignIn.SharedInstance.RestorePreviousSignIn();

                var user = SignIn.SharedInstance.CurrentUser;
                return user != null ? new GoogleUser
                {
                    Id = user.UserId,
                    Name = user.Profile.Name,
                    GivenName = user.Profile.GivenName,
                    FamilyName = user.Profile.FamilyName,
                    Email = user.Profile.Email,
                    Picture = user.Profile.HasImage
                        ? new Uri(user.Profile.GetImageUrl(500).ToString())
                        : new Uri(string.Empty)
                } : null;
            }
        }

        public bool IsLoggedIn
        {
            get
            {
                return SignIn.SharedInstance.HasPreviousSignIn;
            }
        }

        /*
        public DateTime TokenExpirationDate { get { return _tokenExpirationDate; } }
        DateTime _tokenExpirationDate { get; set; }
        */
        static TaskCompletionSource<GoogleResponse<GoogleUser>> _loginTcs;

        public static void Initialize(
            string clientId = null,
            params string[] scopes
        )
        {
            SignIn.SharedInstance.Delegate = CrossGoogleClient.Current as ISignInDelegate;
            if (scopes != null && scopes.Length > 0)
            {

                var currentScopes = SignIn.SharedInstance.Scopes;
                var initScopes = currentScopes
                    .Concat(scopes)
                    .Distinct()
                    .ToArray();


                SignIn.SharedInstance.Scopes = initScopes;
            }
            SignIn.SharedInstance.ClientId = string.IsNullOrWhiteSpace(clientId)
                ? GetClientIdFromGoogleServiceDictionary()
                : clientId;
            //SignIn.SharedInstance.ShouldFetchBasicProfile = true;
        }

        static string GetClientIdFromGoogleServiceDictionary()
        {
            var googleServiceDictionary = NSMutableDictionary.FromFile("GoogleService-Info.plist");
            _clientId = googleServiceDictionary["CLIENT_ID"].ToString();
            return googleServiceDictionary["CLIENT_ID"].ToString();
        }

        EventHandler<GoogleClientResultEventArgs<GoogleUser>> _onLogin;
        event EventHandler<GoogleClientResultEventArgs<GoogleUser>> IGoogleClientManager.OnLogin
        {
            add => _onLogin += value;
            remove => _onLogin -= value;
        }

        EventHandler _onLogout;
        public event EventHandler OnLogout
        {
            add => _onLogout += value;
            remove => _onLogout -= value;
        }

        public void Login()
        {
            UpdatePresentedViewController();

            SignIn.SharedInstance.SignInUser();
        }

        public async Task<GoogleResponse<GoogleUser>> LoginAsync()
        {
            if (SignIn.SharedInstance.ClientId == null)
            {
                throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
            }


            _loginTcs = new TaskCompletionSource<GoogleResponse<GoogleUser>>();

            UpdatePresentedViewController();
            if (CurrentUser == null)
            {

                SignIn.SharedInstance.SignInUser();
            }
            else
            {
                SignIn.SharedInstance.CurrentUser.Authentication.GetTokens(async (Authentication authentication, NSError error) =>
                {
                    if (error == null)
                    {
                        _accessToken = authentication.AccessToken;
                        _idToken = authentication.IdToken;
                        System.Console.WriteLine($"Id Token: {_idToken}");
                        System.Console.WriteLine($"Access Token: {_accessToken}");
                    }

                });


                /* DateTime newDate = TimeZone.CurrentTimeZone.ToLocalTime(
                     new DateTime(2001, 1, 1, 0, 0, 0));
                 _tokenExpirationDate = newDate.AddSeconds(user.Authentication.AccessTokenExpirationDate.SecondsSinceReferenceDate);
                 */
                var googleArgs = new GoogleClientResultEventArgs<GoogleUser>(
                    CurrentUser,
                    GoogleActionStatus.Completed,
                    "the user is authenticated correctly"
                );

                // Log the result of the authentication
                Debug.WriteLine(Tag + ": Authentication " + GoogleActionStatus.Completed);

                // Send the result to the receivers
                _onLogin?.Invoke(this, googleArgs);
                _loginTcs.TrySetResult(new GoogleResponse<GoogleUser>(googleArgs));
            }

            return await _loginTcs.Task;
        }

        public async Task<GoogleResponse<GoogleUser>> SilentLoginAsync()
        {
            if (SignIn.SharedInstance.ClientId == null)
            {
                throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
            }

            //SignIn.SharedInstance.CurrentUser.Authentication.ClientId != _clientId
            _loginTcs = new TaskCompletionSource<GoogleResponse<GoogleUser>>();

            if (SignIn.SharedInstance.HasPreviousSignIn)
                SignIn.SharedInstance.RestorePreviousSignIn();

            var currentUser = SignIn.SharedInstance.CurrentUser;
            var isSuccessful = currentUser != null;

            if (isSuccessful)
            {
                OnSignInSuccessful(currentUser);
            }
            else
            {
                var errorEventArgs = new GoogleClientErrorEventArgs();
                errorEventArgs.Error = GoogleClientErrorType.SignInDefaultError;
                errorEventArgs.Message = GoogleClientBaseException.SignInDefaultErrorMessage;
                _onError?.Invoke(this, errorEventArgs);
                _loginTcs.TrySetException(new GoogleClientBaseException());
            }

            return await _loginTcs.Task;
        }

        public static bool OnOpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            var openUrlOptions = new UIApplicationOpenUrlOptions(options);
            return SignIn.SharedInstance.HandleUrl(url);
        }

        protected virtual void OnLoginCompleted(GoogleClientResultEventArgs<GoogleUser> e)
        {
            _onLogin?.Invoke(this, e);
        }

        public void Logout()
        {
            if (SignIn.SharedInstance.ClientId == null)
            {
                throw new GoogleClientNotInitializedErrorException(GoogleClientBaseException.ClientNotInitializedErrorMessage);
            }

            if (IsLoggedIn)
            {
                _idToken = null;
                _accessToken = null;
                SignIn.SharedInstance.SignOutUser();
                // Send the logout result to the receivers
                OnLogoutCompleted(EventArgs.Empty);
            }

        }

        protected virtual void OnLogoutCompleted(EventArgs e)
        {
            _onLogout?.Invoke(this, e);
        }

        EventHandler<GoogleClientErrorEventArgs> _onError;
        public event EventHandler<GoogleClientErrorEventArgs> OnError
        {
            add => _onError += value;
            remove => _onError -= value;
        }


        public void DidSignIn(SignIn signIn, Google.SignIn.GoogleUser user, NSError error)
        {
            var isSuccessful = user != null && error == null;

            if (isSuccessful)
            {
                OnSignInSuccessful(user);
                return;
            }

            GoogleClientErrorEventArgs errorEventArgs = new GoogleClientErrorEventArgs();
            Exception exception = null;
            switch (error.Code)
            {
                case -1:
                    errorEventArgs.Error = GoogleClientErrorType.SignInUnknownError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInUnknownErrorMessage;
                    exception = new GoogleClientSignInUnknownErrorException();
                    break;
                case -2:
                    errorEventArgs.Error = GoogleClientErrorType.SignInKeychainError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInKeychainErrorMessage;
                    exception = new GoogleClientSignInKeychainErrorException();
                    break;
                case -3:
                    errorEventArgs.Error = GoogleClientErrorType.NoSignInHandlersInstalledError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInNoSignInHandlersInstalledErrorMessage;
                    exception = new GoogleClientSignInNoSignInHandlersInstalledErrorException();
                    break;
                case -4:
                    errorEventArgs.Error = GoogleClientErrorType.SignInHasNoAuthInKeychainError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInUnknownErrorMessage;
                    exception = new GoogleClientSignInHasNoAuthInKeychainErrorException();
                    break;
                case -5:
                    errorEventArgs.Error = GoogleClientErrorType.SignInCanceledError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInCanceledErrorMessage;
                    exception = new GoogleClientSignInCanceledErrorException();
                    break;
                default:
                    errorEventArgs.Error = GoogleClientErrorType.SignInDefaultError;
                    errorEventArgs.Message = GoogleClientBaseException.SignInDefaultErrorMessage;
                    exception = new GoogleClientBaseException();
                    break;
            }

            _onError?.Invoke(this, errorEventArgs);
            _loginTcs.TrySetException(exception);
        }

        [Export("signIn:didDisconnectWithUser:withError:")]
        public void DidDisconnect(SignIn signIn, Google.SignIn.GoogleUser user, NSError error)
        {
            // Perform any operations when the user disconnects from app here.
        }


        void UpdatePresentedViewController()
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            var viewController = window.RootViewController;
            while (viewController.PresentedViewController != null)
            {
                viewController = viewController.PresentedViewController;
            }

            SignIn.SharedInstance.PresentingViewController = viewController;
        }


        void OnSignInSuccessful(Google.SignIn.GoogleUser user)
        {
            GoogleUser googleUser = new GoogleUser
            {
                Id = user.UserId,
                Name = user.Profile.Name,
                GivenName = user.Profile.GivenName,
                FamilyName = user.Profile.FamilyName,
                Email = user.Profile.Email,
                Picture = user.Profile.HasImage
                        ? new Uri(user.Profile.GetImageUrl(500).ToString())
                        : new Uri(string.Empty)
            };

            user.Authentication.GetTokens(async (Authentication authentication, NSError error) =>
            {
                if (error == null)
                {
                    _accessToken = authentication.AccessToken;
                    _idToken = authentication.IdToken;
                    System.Console.WriteLine($"Id Token: {_idToken}");
                    System.Console.WriteLine($"Access Token: {_accessToken}");
                }

            });


            /* DateTime newDate = TimeZone.CurrentTimeZone.ToLocalTime(
                 new DateTime(2001, 1, 1, 0, 0, 0));
             _tokenExpirationDate = newDate.AddSeconds(user.Authentication.AccessTokenExpirationDate.SecondsSinceReferenceDate);
             */
            var googleArgs = new GoogleClientResultEventArgs<GoogleUser>(
                googleUser,
                GoogleActionStatus.Completed,
                "the user is authenticated correctly"
            );

            // Log the result of the authentication
            Debug.WriteLine(Tag + ": Authentication " + GoogleActionStatus.Completed);

            // Send the result to the receivers
            _onLogin?.Invoke(this, googleArgs);
            _loginTcs.TrySetResult(new GoogleResponse<GoogleUser>(googleArgs));

        }
    }
#endif
}
