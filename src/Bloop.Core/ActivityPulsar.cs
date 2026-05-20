namespace Bloop.Core;

public static class ActivityPulsar
{     
     public static event EventHandler? ActivityStarted;

     public static void Pulse()
     {
          ActivityStarted?.Invoke(null, EventArgs.Empty);
     }
}