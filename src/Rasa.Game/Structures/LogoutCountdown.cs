namespace Rasa.Structures
{
    public class LogoutCountdown
    {
        // Official Deployment 10.6 live notes, 23 July 2008; see docs/death-retail-evidence.md.
        public const int DurationMilliseconds = 10000;

        private long? _deadline;

        public bool IsWaiting(long nowMilliseconds)
            => _deadline.HasValue && nowMilliseconds < _deadline.Value;

        public int Begin(long nowMilliseconds)
        {
            // Repeated requests refer to the same pending logout.
            _deadline ??= nowMilliseconds + DurationMilliseconds;
            return (int)System.Math.Max(0, _deadline.Value - nowMilliseconds);
        }

        public bool TryComplete(long nowMilliseconds)
        {
            if (!_deadline.HasValue || nowMilliseconds < _deadline.Value)
                return false;

            _deadline = null;
            return true;
        }

        public void Cancel()
        {
            _deadline = null;
        }
    }
}
