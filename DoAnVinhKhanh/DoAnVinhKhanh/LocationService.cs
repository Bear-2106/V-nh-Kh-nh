using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace DoAnVinhKhanh
{
    public class LocationService
    {
        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (permission != PermissionStatus.Granted)
                {
                    permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (permission != PermissionStatus.Granted)
                {
                    return null;
                }

                var request = new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(10));

                var location = await Geolocation.Default.GetLocationAsync(request);

                return location;
            }
            catch (Exception ex)
            {
                throw new Exception($"Không lấy được vị trí: {ex.Message}");
            }
        }
    }
}