using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class KMA_GridConverter
{
    const double RE = 6371.00877; // 지구 반경(km)
    const double GRID = 5.0;      // 격자 간격 (km)
    const double SLAT1 = 30.0;    // 투영 위도1(degree)
    const double SLAT2 = 60.0;    // 투영 위도2(degree)
    const double OLON = 126.0;    // 기준점 경도(degree)
    const double OLAT = 38.0;     // 기준점 위도(degree)
    const double XO = 43;         // 기준점 X좌표(GRID)
    const double YO = 136;        // 기준점 Y좌표(GRID)

    public static Vector2 LatLonToGrid(double lat, double lon)
    {
        double DEGRAD = Math.PI / 180.0;
        double re = RE / GRID;
        double slat1 = SLAT1 * DEGRAD;
        double slat2 = SLAT2 * DEGRAD;
        double olon = OLON * DEGRAD;
        double olat = OLAT * DEGRAD;

        double sn = Math.Tan(Math.PI * 0.25 + slat2 * 0.5) / Math.Tan(Math.PI * 0.25 + slat1 * 0.5);
        sn = Math.Log(Math.Cos(slat1) / Math.Cos(slat2)) / Math.Log(sn);
        double sf = Math.Tan(Math.PI * 0.25 + slat1 * 0.5);
        sf = Math.Pow(sf, sn) * Math.Cos(slat1) / sn;
        double ro = Math.Tan(Math.PI * 0.25 + olat * 0.5);
        ro = re * sf / Math.Pow(ro, sn);

        double ra = Math.Tan(Math.PI * 0.25 + lat * DEGRAD * 0.5);
        ra = re * sf / Math.Pow(ra, sn);
        double theta = lon * DEGRAD - olon;
        if (theta > Math.PI) theta -= 2.0 * Math.PI;
        if (theta < -Math.PI) theta += 2.0 * Math.PI;
        theta *= sn;

        double x = ra * Math.Sin(theta) + XO + 0.5;
        double y = ro - ra * Math.Cos(theta) + YO + 0.5;

        return new Vector2((int)x, (int)y);
    }
}
