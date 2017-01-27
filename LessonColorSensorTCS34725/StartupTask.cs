using Windows.ApplicationModel.Background;
using Windows.UI;
using System.Threading.Tasks;
using Glovebox.Graphics.Drivers;
using Glovebox.Graphics.Components;

namespace LessonColorSensorTCS34725
{
    public sealed class StartupTask : IBackgroundTask
    {
        //LED matrix
        Ht16K33 driver = new Ht16K33(new byte[] { 0x70 }, Ht16K33.Rotate.None);

        private TCS34725 TCS34725Sensor;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            LED8x8Matrix matrix = new LED8x8Matrix(driver);
            TCS34725Sensor = new TCS34725();
            while (true)
            {
                Color RGB = TCS34725Sensor.ColorRGBC();
                var message = $"R:{RGB.R} G:{RGB.G} B:{RGB.B} C:{RGB.A} ";
                matrix.ScrollStringInFromRight(message, 70);
                Task.Delay(1000).Wait();
            }
        }
    }
}
