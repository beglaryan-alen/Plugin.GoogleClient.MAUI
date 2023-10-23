﻿using System;
using System.Threading.Tasks;

namespace Plugin.GoogleClient.MAUI
{
    public enum GoogleClientErrorType
    {
        SignInUnknownError,
        SignInKeychainError,
        NoSignInHandlersInstalledError,
        SignInHasNoAuthInKeychainError,
        SignInCanceledError,
        SignInDefaultError,
        SignInApiNotConnectedError,
        SignInInvalidAccountError,
        SignInNetworkError,
        SignInInternalError,
        SignInRequiredError,
        SignInFailedError
    }
    public class GoogleClientErrorEventArgs : EventArgs
    {
        public GoogleClientErrorType Error { get; set; }
        public string Message { get; set; }
    }

    public enum GoogleActionStatus
    {
        Canceled,
        Unauthorized,
        Completed,
        Error
    }

    public class GoogleClientResultEventArgs<T> : EventArgs
    {
        public T Data { get; set; }
        public GoogleActionStatus Status { get; set; }
        public string Message { get; set; }

        public GoogleClientResultEventArgs(T data, GoogleActionStatus status, string msg = "")
        {
            Data = data;
            Status = status;
            Message = msg;
        }
    }

    public class GoogleResponse<T>
    {
        public T Data { get; set; }
        public GoogleActionStatus Status { get; set; }
        public string Message { get; set; }

        public GoogleResponse(GoogleClientResultEventArgs<T> evtArgs)
        {
            Data = evtArgs.Data;
            Status = evtArgs.Status;
            Message = evtArgs.Message;
        }

        public GoogleResponse(T user, GoogleActionStatus status, string msg = "")
        {
            Data = user;
            Status = status;
            Message = msg;
        }
    }

    /// <summary>
    /// Interface for GoogleClientManager
    /// </summary>
    public interface IGoogleClientManager
    {
        event EventHandler<GoogleClientResultEventArgs<GoogleUser>> OnLogin;
        event EventHandler OnLogout;
        event EventHandler<GoogleClientErrorEventArgs> OnError;
        Task<GoogleResponse<GoogleUser>> LoginAsync();
        Task<GoogleResponse<GoogleUser>> SilentLoginAsync();
        void Logout();
        string IdToken { get; }
        string AccessToken { get; }
        GoogleUser CurrentUser { get; }
        bool IsLoggedIn { get; }
        //DateTime TokenExpirationDate { get; }
    }
}
