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
                SessionId = sessionId,
                Users = new List<User>() { new User() { ConnectionId = Context.ConnectionId, IsHost = true } }
            });
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.SendAsync("SessionStarted", sessionId);
        }

        public async Task JoinSession(string sessionId, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            var user = new User() { ConnectionId = Context.ConnectionId, UserName = userName };
            _sessions.Single(s => s.SessionId == sessionId)?.Users.Add(user);

            await Clients.Group(sessionId).SendAsync("UserJoined", user);
        }

        public async Task SelectWinner()
        {
            var session = GetCurrentUserSession();
            if (session?.SessionId != null)
            {
                var random = new Random();
                var winner = session.Users.Where(u => !u.IsHost && session.Winners.All(w => w.ConnectionId != u.ConnectionId)).MinBy(u => Guid.NewGuid().ToString());
                if (winner != null)
                {
                    winner.HasBeenWinner = true;
                    await Clients.Group(session.SessionId).SendAsync("WinnerSelected", winner);
                    session.Winners.Add(winner);
                }
            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var session = GetCurrentUserSession();
            if (session?.SessionId != null)
            {
                var user = session.Users.Single(u => u.ConnectionId == Context.ConnectionId);
                session.Users.Remove(user);
                await Clients.Group(session.SessionId).SendAsync("UserLeft", user);
            }
            await base.OnDisconnectedAsync(exception);
        }

        private Session? GetCurrentUserSession()
        {
            var session = _sessions.SingleOrDefault(s => s.Users.Any(u => u.ConnectionId == Context.ConnectionId));
            return session;
        }
    }

    public class Session
    {
        public string? SessionId { get; set; }
        public List<User> Users { get; set; } = new();
        public List<User> Winners { get; set; } = new();
    }

    public class User
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public bool HasBeenWinner { get; set; }
        public bool IsHost { get; set; }
    }
}
