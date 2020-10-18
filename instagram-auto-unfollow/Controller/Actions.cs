﻿using instagram_auto_unfollow.Helpers;
using instagram_auto_unfollow.Model;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Logger;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace instagram_auto_unfollow.Controller
{
  public class Actions
  {
    private readonly UserSessionData userSession;

    public Actions(UserSessionData UserSession)
    {
      userSession = UserSession;
    }

    public async Task<ActionModel> DoLogin(HttpClientHandler httpClient = null)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "Login",
        Username = userSession.UserName,
        Status = 0
      };
      try
      {
        IInstaApiBuilder instaApiBuild = InstaApiBuilder.CreateBuilder()
            .SetUser(userSession)
            .UseLogger(new DebugLogger(LogLevel.Exceptions));

        if (httpClient != null)
        {
          instaApiBuild = instaApiBuild.UseHttpClientHandler(httpClient);
        }

        HelpersApi.InstaApi = instaApiBuild.Build();

        var loginResult = await HelpersApi.InstaApi.LoginAsync();
        if (loginResult.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success | Username: {userSession.UserName}";
        }
        else
        {
          switch (loginResult.Value)
          {
            case InstaLoginResult.InvalidUser:
              dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Username is invalid.";
              break;
            case InstaLoginResult.BadPassword:
              dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Password is wrong.";
              break;
            case InstaLoginResult.Exception:
              dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {loginResult.Info.Message}";
              break;
            case InstaLoginResult.LimitError:
              dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Limit error (you should wait 10 minutes).";
              break;
            case InstaLoginResult.ChallengeRequired:
              dataAction.Status = 2;
              dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Challenge Required.";
              HandleChallenge();
              break;
            case InstaLoginResult.TwoFactorRequired:
              dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error:  Factor Required. Disabled it first!";
              break;
            case InstaLoginResult.InactiveUser:
              dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error:  {loginResult.Info.Message}";
              break;
          }
        }
      }
      catch (Exception ex)
      {
        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error:  {ex.Message}";
      }
      return dataAction;
    }

    private static async void HandleChallenge()
    {
      try
      {
        IResult<InstaChallengeRequireVerifyMethod> challenge = null;
        challenge = await HelpersApi.InstaApi.GetChallengeRequireVerifyMethodAsync();
        if (challenge.Succeeded)
        {
          if (challenge.Value.StepData != null)
          {
            if (!string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
              Console.WriteLine($"[ 1 ] Challange: {challenge.Value.StepData.PhoneNumber}");
            if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
              Console.WriteLine($"[ 2 ] Challange: {challenge.Value.StepData.Email}");
          }
        }
        else
        {
          Console.WriteLine($"Challange Error: {challenge.Info.Message}");
          return;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Challange Error: {ex.Message}");
        return;
      }
    }

    public async Task SendCode()
    {
      Console.Write("Verify options [1 or 2]: ");
      var options = Console.ReadLine();
      if (HelpersApi.InstaApi == null)
        return;
      try
      {
        if (int.TryParse(options, out int option))
        {
          if (option == 1)
          {
            var phoneNumber = await HelpersApi.InstaApi.RequestVerifyCodeToSMSForChallengeRequireAsync();
            if (phoneNumber.Succeeded)
            {
              Console.WriteLine($"We sent verify code to this phone number: {phoneNumber.Value.StepData.ContactPoint}");
            }
            else
            {
              Console.WriteLine($"Challange Error: {phoneNumber.Info.Message}");
              return;
            }
          }
          else if (option == 2)
          {
            var email = await HelpersApi.InstaApi.RequestVerifyCodeToEmailForChallengeRequireAsync();
            if (email.Succeeded)
            {
              Console.WriteLine($"We sent verify code to this Email: {email.Value.StepData.ContactPoint}");
            }
            else
            {
              Console.WriteLine($"Challange Error: {email.Info.Message}");
              return;
            }
          }
          else
          {
            Console.WriteLine("Invalid input!");
            return;
          }
        }
        else
        {
          Console.WriteLine("Invalid input , Enter only number");
          return;
        }
      }
      catch (Exception ex)
      {

        Console.WriteLine($"Challange Error: {ex.Message}");
        return;
      }
    }

    public async Task<ActionModel> VerifyCode(string code)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "VerifyCode",
        Username = userSession.UserName,
        Status = 0
      };
      try
      {
        var regex = new Regex(@"^-*[0-9,\.]+$");
        if (!regex.IsMatch(code))
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Verification code is numeric!";
        }
        if (code.Length != 6)
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Verification code must be 6 digits!";
        }
        var verify = await HelpersApi.InstaApi.VerifyCodeForChallengeRequireAsync(code);
        if (verify.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success | Username: {userSession.UserName} { Environment.NewLine}";
        }
        else
        {
          if (verify.Value == InstaLoginResult.TwoFactorRequired)
          {
            dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Two Factor Required. Disabled it first!";
          }
        }
      }
      catch (Exception ex)
      {
        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {ex.Message}!";
      }

      return dataAction;
    }

    public async Task<ActionModel> DoFollow(long userPk)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "Follow",
        Username = null,
        Status = 0
      };
      try
      {
        var follow = await HelpersApi.InstaApi.UserProcessor.FollowUserAsync(userPk);
        if (follow.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success";
        }
        else
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {follow.Info.Message}";
        }

      }
      catch (Exception ex)
      {

        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {ex.Message}";
      }
      return dataAction;
    }

    public async Task<ActionModel> DoUnfollow(long userPk)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "Unfollow",
        Username = null,
        Status = 0
      };
      try
      {
        var unfollow = await HelpersApi.InstaApi.UserProcessor.UnFollowUserAsync(userPk);
        if (unfollow.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success";
        }
        else
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {unfollow.Info.Message}";
        }

      }
      catch (Exception ex)
      {

        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {ex.Message}";
      }
      return dataAction;
    }
  }
}
