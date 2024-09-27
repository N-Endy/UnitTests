using CodingTracker.Models;
using CodingTracker.IDataRepository;
using CodingTracker.Utilities;
using CodingTracker.ConsoleInteraction;
using System.Diagnostics;

namespace CodingTracker.Services
{
    public class CodingSessionService : ICodingSessionService
    {
        private readonly ICodingSessionRepository _sessionRepository;
        private readonly IUserInteraction _userInteraction;
        private readonly IUtils _utils;
        private Stopwatch _stopwatch;

        public CodingSessionService(ICodingSessionRepository sessionRepository, IUtils utils, IUserInteraction userInteraction)
        {
            _sessionRepository = sessionRepository;
            _userInteraction = userInteraction;
            _utils = utils;
            _stopwatch = new Stopwatch();
        }

        public void DeleteSession()
        {
            GetAllSessions();

            _userInteraction.ShowMessageTimeout("\n\n[Red]Please type the ID of the session to delete or 0 to return to Main Menu: [/]");
            int outId;
            bool isValid;
            do
            {
                _userInteraction.ShowMessage("[Green]Please enter a number (must be an integer and >= 0):[/]");
                string id = _userInteraction.GetUserInput();

                // Try to parse the input to an integer
                isValid = int.TryParse(id, out outId);

                // Check if the input is a valid integer and greater than or equal to 0
                if (!isValid || outId < 0)
                {
                    _userInteraction.ShowMessage("[Red]Invalid input. Please enter a valid integer that is >= 0.[/]");
                    isValid = false;
                }

            } while (!isValid);
            if (outId == 0) return;

            _sessionRepository.DeleteSessionFromDatabase(outId);
        }

        public void GetAllSessions()
        {
            _sessionRepository.GetFromDatabase();
        }

        public void InsertSession(CodingSession session)
        {
            var listOfTimes = _utils.ValidatedTimes();
            session.StartTime = listOfTimes[0];
            session.EndTime = listOfTimes[1];
            session.Duration = _utils.GetSessionDuration(session.StartTime, session.EndTime);

            _sessionRepository.InsertSessionToDatabase(session.StartTime, session.EndTime, session.Duration);
        }

        public void UpdateSession()
        {
            // Show table of sessions
            GetAllSessions();

            _userInteraction.ShowMessageTimeout("\n\n[Yellow]Please type the ID of the session you would like to update. Type 0 to return to Main Menu: [/]");

            int outId;
            bool isValid;
            do
            {
                _userInteraction.ShowMessage("[Green]Please enter a number (must be an integer and >= 0):[/]");
                string id = _userInteraction.GetUserInput();

                // Try to parse the input to an integer
                isValid = int.TryParse(id, out outId);

                // Check if the input is a valid integer and greater than or equal to 0
                if (!isValid || outId < 0)
                {
                    _userInteraction.ShowMessage("[Red]Invalid input. Please enter a valid integer that is >= 0.[/]");
                    isValid = false;
                }

            } while (!isValid);
            if (outId == 0) return;

            _userInteraction.ShowMessage("\n[Yellow]Which property would you like to update[/]");
            _userInteraction.ShowMessage("\n[DarkGreen]1. Start Time[/]");
            _userInteraction.ShowMessage("\n[DarkGreen]2. End Time[/]");
            _userInteraction.ShowMessage("\n[Green]Input: [/]");

            switch (_userInteraction.GetUserInput())
            {
                case "1":
                    DateTime startTime = _utils.ValidatedStartTime();
                    _sessionRepository.UpdateStartTimeInDatabase(outId, startTime);
                    break;
                case "2":
                    DateTime endTime = _utils.ValidatedEndTime();
                    _sessionRepository.UpdateEndTimeInDatabase(outId, endTime);
                    break;
                default:
                    _userInteraction.ShowMessageTimeout("\n[Red]You have inputted a wrong choice. Going back to Menu[/]\n");
                    break;
            }
        }

        public void StartCodingSession()
        {
            _stopwatch.Start();
            _userInteraction.ShowMessageTimeout("\n[Green]Starting Coding Session...[/]\n");
            _userInteraction.ShowMessageTimeout("\n[Red]Press any key to stop session...[/]\n");
            Console.ReadKey();
            StopCodingSession();
        }

        public void StopCodingSession()
        {
            _stopwatch.Stop();

            var session = new CodingSession
            {
                StartTime = DateTime.Now.Subtract(_stopwatch.Elapsed),
                EndTime = DateTime.Now,
                Duration = _utils.GetSessionDuration(DateTime.Now.Subtract(_stopwatch.Elapsed), DateTime.Now),
            };

            _sessionRepository.InsertSessionToDatabase(session.StartTime, session.EndTime, session.Duration);
            _userInteraction.ShowMessageTimeout("\n[Green]Session Stopped.[/]\n");
            _userInteraction.ShowMessageTimeout($"\n[Green]Start Time: { session.StartTime }[/]\n");
            _userInteraction.ShowMessageTimeout($"\n[Green]End Time: { session.EndTime }[/]\n");
            _userInteraction.ShowMessageTimeout($"\n[Green]Duration: { session.Duration }[/]\n");
        }

        public void CodingSessionsByPeriod()
        {
            var sessions = _sessionRepository.GetAllFromDatabase();
            IEnumerable<CodingSession> filteredSessions = [];

            _userInteraction.ShowMessage("\n[Green]Which period do you want to filter by? (days, hours, minutes)[/]. Press '0' to exit: ");

            string period = "";

            do
            {
                period = _userInteraction.GetUserInput().ToLower();
                if (period == "0") return; // Exit if '0' is pressed

                if (period != "days" && period != "hours" && period != "minutes")
                {
                    _userInteraction.ShowMessage("\n[Red]Invalid input. Please enter 'days', 'hours', or 'minutes': [/]");
                }

            } while (period != "days" && period != "hours" && period != "minutes");

            switch (period)
            {
                case "days":
                    filteredSessions = sessions.GroupBy(s => (s.EndTime - s.StartTime.Date).Days).Select(g => g.First());
                    break;
                case "hours":
                    filteredSessions = sessions.GroupBy(s => (s.EndTime - s.StartTime.Date).Hours).Select(g => g.First());
                    break;
                case "minutes":
                    filteredSessions = sessions.GroupBy(s => (s.EndTime - s.StartTime.Date).Minutes).Select(g => g.First());
                    break;
                default:
                    break;
            }

            _sessionRepository.GetFromDatabaseOrdered([.. filteredSessions.OrderBy(s => s.Duration)]);
        }
    }
}