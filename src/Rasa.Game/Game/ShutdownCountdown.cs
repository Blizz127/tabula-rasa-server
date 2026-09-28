using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Game
{
    using Data;

    /// <summary>
    /// The operator's shutdown countdown: "Server Shutting Down in 10..." and then the bare numbers 9 to 1
    /// as admin messages, then every connection closed, which each client shows as its own "You have been
    /// disconnected from the server" dialog over the still-rendered world.
    ///
    /// This is what two independent recordings of the EU server's last minute show (fxAtDpxypSw, English
    /// client, and CommanderGrog's _gwh1__XecI, German client; docs/evidence/shutdown-broadcast.json). The
    /// steps were not one a second: 6, 4, 3, 3, 3, 4, 4, 4 and 3 s, and the drop came 3.1 s after "1". That
    /// irregular spacing reads like someone typing, so the observed cadence is a measurement of one night,
    /// not a known server constant; it is the default, labelled measured, and a uniform cadence can be asked
    /// for instead.
    ///
    /// Nothing starts it but the operator's console command. It never runs on a timer, a date or a player
    /// count.
    /// </summary>
    public sealed class ShutdownCountdown
    {
        /// <summary>One admin message, at an offset from the start.</summary>
        public sealed class Line
        {
            public Line(long atMs, string text)
            {
                AtMs = atMs;
                Text = text;
            }

            public long AtMs { get; }
            public string Text { get; }
        }

        /// <summary>The lines in order, and when after the start everyone is disconnected.</summary>
        public sealed class Schedule
        {
            public Schedule(string description, IReadOnlyList<Line> lines, long disconnectAtMs)
            {
                if (lines == null || lines.Count == 0)
                    throw new ArgumentException("A countdown needs at least one line.", nameof(lines));

                for (var i = 1; i < lines.Count; i++)
                    if (lines[i].AtMs < lines[i - 1].AtMs)
                        throw new ArgumentException("Lines must be in time order.", nameof(lines));

                if (disconnectAtMs < lines[^1].AtMs)
                    throw new ArgumentException("The disconnect cannot come before the last line.", nameof(disconnectAtMs));

                Description = description;
                Lines = lines;
                DisconnectAtMs = disconnectAtMs;
            }

            public string Description { get; }
            public IReadOnlyList<Line> Lines { get; }
            public long DisconnectAtMs { get; }
        }

        /// <summary>
        /// The first line's wording. Observed at 1080p in _gwh1__XecI t=546 ("Server Shutting Down in 10...");
        /// fxAtDpxypSw t=122 reads the same at 480p, its trailing dots unresolved. Only 10 was ever seen; the
        /// same wording with another number is the uniform variant's own choice.
        /// </summary>
        public const string FirstLineFormat = "Server Shutting Down in {0}...";

        /// <summary>The number the observed countdown starts from.</summary>
        public const int ObservedFrom = 10;

        /// <summary>
        /// When each line arrived after the first, in ms: fxAtDpxypSw t = 122, 128, 132, 135, 138, 141, 145,
        /// 149, 153, 156 (first 1 fps frame showing it, +-1 s each). Measured.
        /// </summary>
        public static readonly IReadOnlyList<long> ObservedLineOffsetsMs =
            new long[] { 0, 6000, 10000, 13000, 16000, 19000, 23000, 27000, 31000, 34000 };

        /// <summary>
        /// The disconnect after the first line: fxAtDpxypSw t = 159.1 +- 0.1 s (10 fps), 37.1 +- 1 s after
        /// "Server Shutting Down in 10." and 3.1 +- 1 s after "1". _gwh1__XecI gives 38.9 +- 1 s. Measured.
        /// </summary>
        public const long ObservedDisconnectMs = 37100;

        public const int MinFrom = 1;
        public const int MaxFrom = 100;
        public const long MinStepMs = 100;
        public const long MaxStepMs = 600000;

        private readonly Rasa.Timer.Timer _timer;
        private readonly Action<string> _broadcast;
        private readonly Action _disconnectAll;
        private readonly Action _finished;
        private readonly List<string> _timerNames = new List<string>();
        private readonly object _lock = new object();
        private int _generation;

        public ShutdownCountdown(Rasa.Timer.Timer timer, Action<string> broadcast, Action disconnectAll, Action finished = null)
        {
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
            _broadcast = broadcast ?? throw new ArgumentNullException(nameof(broadcast));
            _disconnectAll = disconnectAll ?? throw new ArgumentNullException(nameof(disconnectAll));
            _finished = finished;
        }

        private volatile bool _running;

        public bool Running
        {
            get => _running;
            private set => _running = value;
        }

        public Schedule Current { get; private set; }

        /// <summary>The countdown as the EU server's last minute ran it.</summary>
        public static Schedule Observed()
        {
            var lines = new List<Line>();

            for (var i = 0; i < ObservedLineOffsetsMs.Count; i++)
                lines.Add(new Line(ObservedLineOffsetsMs[i], LineText(ObservedFrom, ObservedFrom - i)));

            return new Schedule("observed final-night cadence (measured, fxAtDpxypSw)", lines, ObservedDisconnectMs);
        }

        /// <summary>From <paramref name="from"/> down to 1, one line every step, and the disconnect a step after "1".</summary>
        public static Schedule Uniform(int from, long stepMs)
        {
            if (from < MinFrom || from > MaxFrom)
                throw new ArgumentOutOfRangeException(nameof(from), $"from must be {MinFrom}-{MaxFrom}");

            if (stepMs < MinStepMs || stepMs > MaxStepMs)
                throw new ArgumentOutOfRangeException(nameof(stepMs), $"step must be {MinStepMs}-{MaxStepMs} ms");

            var lines = new List<Line>();

            for (var i = 0; i < from; i++)
                lines.Add(new Line(i * stepMs, LineText(from, from - i)));

            return new Schedule($"uniform, from {from} every {stepMs / 1000.0:0.###} s", lines, from * stepMs);
        }

        private static string LineText(int from, int number)
            => number == from
                ? string.Format(CultureInfo.InvariantCulture, FirstLineFormat, number)
                : number.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads the arguments after "start": nothing for the observed cadence, or "&lt;from&gt; &lt;seconds&gt;"
        /// for a uniform one, either followed by "stay" to keep the server process running afterwards.
        /// </summary>
        public static bool TryParseStart(IReadOnlyList<string> args, out Schedule schedule, out bool stay, out string error)
        {
            schedule = null;
            error = null;

            var words = args.Where(word => !string.IsNullOrEmpty(word)).ToList();
            stay = words.Count > 0 && string.Equals(words[^1], "stay", StringComparison.OrdinalIgnoreCase);

            if (stay)
                words.RemoveAt(words.Count - 1);

            if (words.Count == 0)
            {
                schedule = Observed();
                return true;
            }

            if (words.Count != 2
                || !int.TryParse(words[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var from)
                || !double.TryParse(words[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                error = "usage: shutdown start [<from> <seconds>] [stay]";
                return false;
            }

            var stepMs = (long)Math.Round(seconds * 1000);

            if (from < MinFrom || from > MaxFrom || stepMs < MinStepMs || stepMs > MaxStepMs)
            {
                error = $"from must be {MinFrom}-{MaxFrom} and seconds {MinStepMs / 1000.0}-{MaxStepMs / 1000}";
                return false;
            }

            schedule = Uniform(from, stepMs);
            return true;
        }

        /// <summary>Starts the countdown. False, and nothing changes, if one is already running.</summary>
        public bool Start(Schedule schedule)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            var timers = new List<(string Name, long DelayMs, Action Action)>();

            lock (_lock)
            {
                if (Running)
                    return false;

                Running = true;
                Current = schedule;
                var generation = ++_generation;

                for (var i = 0; i < schedule.Lines.Count; i++)
                {
                    var text = schedule.Lines[i].Text;
                    timers.Add(($"shutdown-countdown-{generation}-{i}", schedule.Lines[i].AtMs,
                        () => Fire(generation, () => _broadcast(text))));
                }

                timers.Add(($"shutdown-countdown-{generation}-disconnect", schedule.DisconnectAtMs,
                    () => Fire(generation, Disconnect)));

                _timerNames.Clear();
                _timerNames.AddRange(timers.Select(timer => timer.Name));
            }

            // Outside the lock: Timer.Update holds its own lock while it runs a callback, and every
            // callback here takes this one, so taking them in the other order could deadlock. A
            // Cancel landing in between is harmless - its generation check makes these no-ops.
            foreach (var (name, delayMs, action) in timers)
                _timer.Add(name, delayMs, false, action);

            return true;
        }

        /// <summary>Stops a running countdown before the disconnect. Nothing is announced.</summary>
        public bool Cancel()
        {
            List<string> names;

            lock (_lock)
            {
                if (!Running)
                    return false;

                Running = false;
                Current = null;
                names = new List<string>(_timerNames);
                _timerNames.Clear();
            }

            foreach (var name in names)
                _timer.Remove(name);

            return true;
        }

        /// <summary>A timer left over from a cancelled run does nothing.</summary>
        private void Fire(int generation, Action action)
        {
            lock (_lock)
            {
                if (!Running || generation != _generation)
                    return;
            }

            action();
        }

        private void Disconnect()
        {
            lock (_lock)
            {
                _timerNames.Clear();
                Running = false;
            }

            _disconnectAll();
            _finished?.Invoke();
        }

        /// <summary>
        /// Closes every connection. Nothing is sent first: the client draws its disconnect dialog
        /// (uielementlanguage 9) only on a connection it did not ask to leave (exitgame.GameOnDisconnect,
        /// g_requestedRestart false), and the socket close is that.
        /// </summary>
        public static int DisconnectAll(IEnumerable<Client> clients)
        {
            var closed = 0;

            foreach (var client in clients.Where(c => c != null && c.State != ClientState.Disconnected).Distinct().ToList())
            {
                try
                {
                    client.Close(false, "the shutdown countdown ended");
                    closed++;
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Shutdown: closing a client threw: {e}");
                }
            }

            return closed;
        }
    }
}
