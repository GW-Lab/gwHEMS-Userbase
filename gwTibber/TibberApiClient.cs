// Program..: TibberApiClient.cs
// Author...: G. Wassink
// Design...:
// Date.....: 12/06/2024 Last revised: 22/08/2025
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (TibberApiClient)  dynamic Time-of-Use (TOU) electricity

// https://github.com/edwh/sma-octopus

using gwTibber.Classes;

namespace gwTibber;

public class TibberApiClient(string accessToken, double lat = 52.25917, double lng = 5.60694)
{
   private const double SunriseSunsetAltitude = -35d / 60d;
   private const double INV360 = 1.0d / 360.0d;

   public ToDay ToDay;
   public ToMorrow ToMorrow;

   public bool EPEX(int year, int month, int day)
   {
      try
      {
         using var EPEX = new EPEX(accessToken);
         var priceInfo = EPEX.GetEPEX();

         SunriseSunset(year, month, day, lng, lat, SunriseSunsetAltitude, true, out double riseToDay, out double setToDay);
         ToDay = new ToDay(priceInfo, riseToDay, setToDay);                                                                            // on availability -> Populate 

         SunriseSunset(year, month, day, lng, lat, SunriseSunsetAltitude, true, out double riseToMorrow, out double setToMorrow);
         ToMorrow = new ToMorrow(priceInfo, riseToMorrow, setToMorrow);                                                                // on availability -> Populate 

         return true;
      }
      catch
      {
         return false;
      }
   }

   private void SunriseSunset(int year, int month, int day, double lat, double lng, out double tsunrise, out double tsunset)
   {
      SunriseSunset(year, month, day, lng, lat, SunriseSunsetAltitude, true, out tsunrise, out tsunset);
   }

   private static long DaysSince2000Jan0(int y, int m, int d)
   {
      return 367L * y - 7 * (y + (m + 9) / 12) / 4 + 275 * m / 9 + d - 730530L;
   }

   private const double RadDeg = 180.0 / Math.PI;                    // Some conversion factors between radians and degrees
   private const double DegRad = Math.PI / 180.0;

   // The trigonometric functions in degrees
   private static double Sind(double x) => Math.Sin(x * DegRad);
   private static double Cosd(double x) => Math.Cos(x * DegRad);
   private static double Tand(double x) => Math.Tan(x * DegRad);
   private static double Atand(double x) => RadDeg * Math.Atan(x);
   private static double Asind(double x) => RadDeg * Math.Asin(x);
   private static double Acosd(double x) => RadDeg * Math.Acos(x);
   private static double Atan2d(double y, double x) => RadDeg * Math.Atan2(y, x);
   private int SunriseSunset(int year, int month, int day, double lon, double lat, double altit, bool upper_limb, out double trise, out double tset)
   {
      double d;                                                      // Days since 2000 Jan 0.0 (negative before)
      double sr;                                                     // Solar distance, astronomical units
      double sRA;                                                    // Sun's Right Ascension
      double sdec;                                                   // Sun's declination
      double sradius;                                                // Sun's apparent radius
      double t;                                                      // Diurnal arc
      double tsouth;                                                 // Time when Sun is at south
      double sidtime;                                                // Local sidereal time

      int rc = 0;                                                    // Return cde from function - usually 0
      d = DaysSince2000Jan0(year, month, day) + 0.5 - lon / 360.0;   // Compute d of 12h local mean solar time
      sidtime = Revolution(GMST0(d) + 180.0 + lon);                  // Compute the local sidereal time of this moment

      Sun_RA_dec(d, out sRA, out sdec, out sr);                      // Compute Sun's RA, Decl and distance at this moment

      tsouth = 12.0 - Rev180(sidtime - sRA) / 15.0;                  // Compute time when Sun is at south - in hours UT
      sradius = 0.2666 / sr;                                         // Compute the Sun's apparent radius in degrees

      if (upper_limb)                                                // Do correction to upper limb, if necessary
         altit -= sradius;

      double cost;                                                   // Compute the diurnal arc that the Sun traverses to reach the specified altitude altit:
      cost = (Sind(altit) - Sind(lat) * Sind(sdec)) /
      (Cosd(lat) * Cosd(sdec));

      switch (cost)                                                  // Sun always below altit */
      {
         case >= 1.0:
            rc = -1;
            t = 0.0;

            break;
         case <= -1.0:
            rc = +1;
            t = 12.0;

            break;
         default:
            t = Acosd(cost) / 15.0;                                  // The diurnal arc, hours */

            break;
      }

      trise = tsouth - t;                                            // Out Store rise and set times - in hours UT
      tset = tsouth + t;                                             // Out Store set and set times - in hours UT

      return rc;
   }

   private double DayLenght(int year, int month, int day, double lon, double lat, double altit, bool upper_limb)
   {
      double d;                                                      // Days since 2000 Jan 0.0 (negative before)
      double obl_ecl;                                                // Obliquity (inclination) of Earth's axis
      double sr;                                                     // Solar distance, astronomical units
      double slon;                                                   // True solar longitude
      double sin_sdecl;                                              // Sine of Sun's declination
      double cos_sdecl;                                              // Cosine of Sun's declination
      double sradius;                                                // Sun's apparent radius

      d = DaysSince2000Jan0(year, month, day) + 0.5 - lon / 360.0;   // Compute d of 12h local mean solar time
      obl_ecl = 23.4393 - 3.563E-7 * d;                              // Compute obliquity of ecliptic (inclination of Earth's axis)

      SunPosition(d, out slon, out sr);                              // Compute Sun's ecliptic longitude and distance

      sin_sdecl = Sind(obl_ecl) * Sind(slon);                        // Compute sine and cosine of Sun's declination
      cos_sdecl = Math.Sqrt(1.0 - sin_sdecl * sin_sdecl);
      sradius = 0.2666 / sr;                                         // Compute the Sun's apparent radius, degrees

      if (upper_limb)                                                // Do correction to upper limb, if necessary
         altit -= sradius;

      double cost = (Sind(altit) - Sind(lat) * sin_sdecl) / (Cosd(lat) * cos_sdecl); // Compute the diurnal arc that the Sun traverses to reach the specified altitude altit

      return cost switch                                             // Sun always below altit 
      {
         >= 1.0 => 0.0,                                              // Diurnal arc
         <= -1.0 => 24.0,                                            // Diurnal arc
         _ => 2.0 / 15.0 * Acosd(cost),                              // Diurnal arc
      };
   }

   private void SunPosition(double d, out double lon, out double r)
   {
      double M;                                                      // Mean anomaly of the Sun 
      double w;                                                      // Mean longitude of perihelion  Note: Sun's mean longitude = M + w 
      double e;                                                      // Eccentricity of Earth's orbit
      double E;                                                      // Eccentric anomaly 
      double x, y;                                                   // x, y coordinates in orbit 
      double v;                                                      // True anomaly 

      M = Revolution(356.0470 + 0.9856002585 * d);                   // Compute mean elements
      w = 282.9404 + 4.70935E-5 * d;
      e = 0.016709 - 1.151E-9 * d;

      E = M + e * RadDeg * Sind(M) * (1.0 + e * Cosd(M));            // Compute true longitude and radius vector
      x = Cosd(E) - e;
      y = Math.Sqrt(1.0 - e * e) * Sind(E);
      r = Math.Sqrt(x * x + y * y);                                  // Solar distance
      v = Atan2d(y, x);                                              // True anomaly
      lon = v + w;                                                   // True solar longitude 

      if (lon >= 360.0)
         lon -= 360.0;                                               // Make it 0..360 degrees
   }

   private void Sun_RA_dec(double d, out double RA, out double dec, out double r)
   {
      double obl_ecl, x, y, z;

      SunPosition(d, out double lon, out r);                         // Compute Sun's ecliptical coordinates

      x = r * Cosd(lon);                                             // Compute ecliptic rectangular coordinates (z=0)
      y = r * Sind(lon);
      obl_ecl = 23.4393 - 3.563E-7 * d;                              // Compute obliquity of ecliptic (inclination of Earth's axis)

      z = y * Sind(obl_ecl);                                         // Convert to equatorial rectangular coordinates - x is unchanged
      y *= Cosd(obl_ecl);

      RA = Atan2d(y, x);                                             // Convert to spherical coordinates
      dec = Atan2d(z, Math.Sqrt(x * x + y * y));
   }

   private static double Revolution(double x) => x - 360.0 * Math.Floor(x * INV360);

   private static double Rev180(double x) => x - 360.0 * Math.Floor(x * INV360 + 0.5);

   private static double GMST0(double d) => Revolution(180.0 + 356.0470 + 282.9404 + (0.9856002585 + 4.70935E-5) * d);
}

