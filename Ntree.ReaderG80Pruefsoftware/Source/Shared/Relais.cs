namespace Shared
{
    public class Relay
    {
        public Relay(int bit, double timeSeconds)
        {
            Bit = bit;
            TimeSeconds = timeSeconds;
            State = false;
        }

        public Relay(int bit, bool state)
        {
            TimeSeconds = 0;
            Bit = bit;
            State = state;
        }

        public int Bit { get; set; }
        public double TimeSeconds { get; set; }
        public bool State { get; set; }
    }
}