using System.Threading;

namespace DumbedDownCron
{
    static class Program
    {
        static void Main()
        {
            new PseudoCron();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
