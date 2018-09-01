using System.Threading.Tasks;
using NObservable;

namespace BlazorTest
{
    [Observable]
    public class AppModel
    {
        public int Counter { get; set; }

        public AppModel()
        {
            Tick();
        }

        async void Tick()
        {
            while (true)
            {
                Counter++;
                await Task.Delay(1000);
            }
        }
    }
    
    
}