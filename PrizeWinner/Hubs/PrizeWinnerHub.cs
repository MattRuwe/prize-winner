using Microsoft.AspNetCore.SignalR;

namespace PrizeWinner.Hubs
{
    public class PrizeWinnerHub : Hub
    {
        private static List<Session> _sessions = new List<Session>();

        public async Task CreateSession(string? sessionName)
        {
            var sessionId = Guid.NewGuid().ToString();
            var session = new Session
            {
                Name = sessionName,
                SessionId = sessionId,
                Users = new List<User>() { new User() { ConnectionId = Context.ConnectionId, IsHost = true } }
            };
            _sessions.Add(session);
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await Clients.Caller.SendAsync("SessionStarted", session);
        }

        public async Task JoinSession(string sessionId, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            var user = new User() { ConnectionId = Context.ConnectionId, UserName = userName };
            var session = _sessions.Single(s => s.SessionId == sessionId);
            session?.Users.Add(user);

            await Clients.Group(sessionId).SendAsync("UserJoined", session);
        }

        public async Task SelectWinner()
        {
            var session = GetCurrentUserSession();
            if (session?.SessionId != null)
            {
                if (session.GetEligibleUsers().All(u => session.Winners.Any(sw => sw.ConnectionId == u.ConnectionId)))
                {
                    session.Winners.Clear();
                }

                var random = new Random();
                var winner = session.GetEligibleUsers().Where(u => session.Winners.All(w => w.ConnectionId != u.ConnectionId)).MinBy(u => Guid.NewGuid().ToString());
                if (winner != null)
                {
                    session.Winners.Add(winner);
                    await Clients.Group(session.SessionId).SendAsync("WinnerSelected", session, winner);
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
                if (session.Users.Count == 0)
                {
                    _sessions.Remove(session);
                }
                else
                {
                    await Clients.Group(session.SessionId).SendAsync("UserLeft", session, user);
                }
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
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
        public List<User> Winners { get; set; } = new();

        public List<User> GetEligibleUsers()
        {
            var eligibleUsers = Users.Where(u => !u.IsHost).ToList();
            return eligibleUsers ?? Enumerable.Empty<User>().ToList();
        }
    }

    public class User
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public bool IsHost { get; set; }
    }
}
