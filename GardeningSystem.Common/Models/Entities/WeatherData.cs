using System;

namespace GardeningSystem.Common.Models.Entities {
    public class WeatherData {

        #region Forecast

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public float AmountOfRainInMm { get; set; }

        #endregion

        #region Historical daily data

        public DateTime Day { get; set; }

        public float Temp_min { get; set; }

        public float Temp_max { get; set; }

        public float Pressure { get; set; }

        public float Humidity { get; set; }

        public float WindSpeed { get; set; }

        public float PrecipitationVolume_mm { get; set; }

        public float MaximumUVIndex { get; set; }

        #endregion

        public WeatherData() {
            Temp_min = float.NaN;
            Temp_max = float.NaN;
            Pressure = float.NaN;
            Humidity = float.NaN;
            WindSpeed = float.NaN;
            PrecipitationVolume_mm = float.NaN;
            MaximumUVIndex = float.NaN;
            Day = DateTime.MinValue;
            From = DateTime.MinValue;
            To = DateTime.MinValue;
            AmountOfRainInMm = float.NaN;
        }
    }
}
