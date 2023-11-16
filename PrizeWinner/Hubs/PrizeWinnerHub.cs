using Microsoft.AspNetCore.SignalR;

namespace PrizeWinner.Hubs
{
    public class PrizeWinnerHub : Hub
    {
        private static List<Session> _sessions = new List<Session>();

        public async Task CreateSession()
        {
            var sessionId = Guid.NewGuid().ToString();
            _sessions.Add(new Session
            {
                SessionId = sessionId
            });
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.SendAsync("SessionStarted", sessionId);
        }

        public async Task JoinSession(string sessionId, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            var user = new User() {ConnectionId = Context.ConnectionId, UserName = userName};
            _sessions.Single(s => s.SessionId == sessionId)?.Users.Add(user);

            await Clients.Group(sessionId).SendAsync("UserJoined", user);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var session = _sessions.SingleOrDefault(s => s.Users.Any(u => u.ConnectionId == Context.ConnectionId));
            if (session?.SessionId != null)
            {
                var user = session.Users.Single(u => u.ConnectionId == Context.ConnectionId);
                session.Users.Remove(user);
                await Clients.Group(session.SessionId).SendAsync("UserLeft", user);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }

    public class Session
    {
        public string? SessionId { get; set; }
        public List<User> Users { get; set; } = new List<User>();
    }

    public class User
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
    }
}
