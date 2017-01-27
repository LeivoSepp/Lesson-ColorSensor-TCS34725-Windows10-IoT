using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using Microsoft.Devices.Tpm;
using Microsoft.Azure.Devices.Client;
using System.Text;

namespace LessonColorSensorTCS34725
{
    public sealed class StartupTask : IBackgroundTask
    {
        private TCS34725 TCS34725Sensor;
        private void initDevice()
        {
            TpmDevice device = new TpmDevice(0);
            string hubUri = device.GetHostName();
            string deviceId = device.GetDeviceId();
            string sasToken = device.GetSASToken();
            _sendDeviceClient = DeviceClient.Create(hubUri, AuthenticationMethodFactory.CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Amqp);
        }
        private DeviceClient _sendDeviceClient;
        private async void SendMessages(string strMessage)
        {
            string messageString = strMessage;
            var message = new Message(Encoding.ASCII.GetBytes(messageString));
            await _sendDeviceClient.SendEventAsync(message);
        }
   
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            TCS34725Sensor = new TCS34725();
            initDevice();
            while (true)
            {
                //Color RGB = TCS34725Sensor.ColorRGBC(); //to have a color in RGB mode
                var message = $"color name:{TCS34725Sensor.ColorName()}";
                SendMessages(message);
                Task.Delay(1000).Wait();
            }
        }
    }
}
