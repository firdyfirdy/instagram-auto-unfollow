using instagram_auto_unfollow.Controller;
using instagram_auto_unfollow.Helpers;
using instagram_auto_unfollow.Model;
using InstagramApiSharp;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using System;
using System.Threading.Tasks;

namespace instagram_auto_unfollow
{
  class Program
  {
    private static bool isLogin = false;
    static async Task Main(string[] args)
    {
      var consoleRed = ConsoleColor.Red;
      var consoleGreen = ConsoleColor.Green;

      HelpersApi.WriteLine("Instagram Auto Unfollow!", consoleGreen);
      HelpersApi.WriteLine("Contact me: me@firdy.dev", consoleGreen);
      Console.WriteLine();

      Console.Write("Username: ");
      string username = Console.ReadLine();
      Console.Write("Password: ");
      string password = Console.ReadLine();

      Console.WriteLine("1.) Unfollow All");
      Console.WriteLine("2.) Unfollow Not Following");
      Console.Write("Select Options (1/2) : ");
      string options = Console.ReadLine();
      Console.Write("Delay (in miliseconds): ");
      if (!int.TryParse(Console.ReadLine(), out int delay))
      {
        HelpersApi.WriteLine($"Error, Not valid arguments.", consoleRed);
        return;
      }

      /* Set Instagram Session */
      UserSessionData sessionData = new UserSessionData()
      {
        UserName = username,
        Password = password
      };

      Actions instaActions = new Actions(sessionData);
      ActionModel login = await instaActions.DoLogin();

      HelpersApi.WriteLine("Trying to login ...", consoleGreen);

      /* Login */
      HelpersApi.WriteLine(login.Response);

      /* Login Success */
      if (login.Status == 1)
      {
        isLogin = true;
      }

      /* Login Challange */
      if (login.Status == 2)
      {
        await instaActions.SendCode();
        HelpersApi.WriteLine("Put your code: ");
        string code = Console.ReadLine();

        ActionModel verifyCode = await instaActions.VerifyCode(code);
        HelpersApi.WriteLine(verifyCode.Response);
        if (verifyCode.Status == 1)
          isLogin = true;
      }

      if (isLogin)
      {
        /* Get Your Instagram Informations */
        IResult<InstaUserInfo> targetInfo = await HelpersApi.InstaApi.UserProcessor
          .GetUserInfoByUsernameAsync(sessionData.UserName);
        
        string LatestMaxId = "";
        int i = 0;
        
        if (targetInfo.Succeeded)
        {
          HelpersApi.WriteLine($"[+] Username: {targetInfo.Value.Username} " +
            $"| Followers: {targetInfo.Value.FollowerCount} | Followings: {targetInfo.Value.FollowingCount}");
          Console.WriteLine();

          while (LatestMaxId != null)
          {
            var getFollowings = await HelpersApi.InstaApi.UserProcessor
                .GetUserFollowingAsync(sessionData.UserName, PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(LatestMaxId));

            if (getFollowings.Succeeded)
            {
              LatestMaxId = getFollowings.Value.NextMaxId;
              foreach (InstaUserShort following in getFollowings.Value)
              {
                // Unfollow All
                if (options == "1")
                {
                  var unfollow = await instaActions.DoUnfollow(following.Pk);
                  if (unfollow.Status == 1)
                  {
                    HelpersApi.WriteLine($"[{i}] Username: {following.UserName} | Unfollow Success.", consoleGreen);
                  }
                  else
                  {
                    HelpersApi.WriteLine($"[{i}] Username: {following.UserName} | Unfollow Failed.", consoleRed);
                  }
                }
                else
                {
                  /* Get Friendship status */
                  var getFriendshipStatus = await HelpersApi.InstaApi.UserProcessor.GetFriendshipStatusAsync(following.Pk);
                  if (getFriendshipStatus.Succeeded)
                  {
                    /* if they follow us */
                    if (getFriendshipStatus.Value.FollowedBy)
                    {
                      HelpersApi.WriteLine($"[{i}] Username: {following.UserName} | Skipped.", consoleRed);
                    }
                    else
                    {
                      var unfollow = await instaActions.DoUnfollow(following.Pk);
                      if (unfollow.Status == 1)
                      {
                        HelpersApi.WriteLine($"[{i}] Username: {following.UserName} | Unfollow Success.", consoleGreen);
                      }
                      else
                      {
                        HelpersApi.WriteLine($"[{i}] Username: {following.UserName} | Unfollow Failed.", consoleRed);
                      }
                    }
                  }
                }
                HelpersApi.WriteLine($"[+] Sleep for {delay} ms");
                await Task.Delay(delay);
                i++;
              }
            }
          }
        }
      }
    }
  }
}
