using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.UI;

public enum WeatherState
{
    Sunny,
    Cloudy,
    Rainy,
    Snowy
}

public class GameManager : MonoBehaviour
{
    public GameObject sunny;
    public GameObject rain;
    public GameObject cloud;
    public GameObject snow;
    public Text state;
    public Text time;
    public string baseDate;
    public string baseTime;

    public Material lightskybox;
    public Material littlecloudskybox;
    public Material cloudskybox;

    public WeatherState nowWeather;

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        StartCoroutine(GetLocation());
    }

    void Update()
    {
        time.text = DateTime.Now.ToString(("yyyy-MM-dd tt HH:mm:ss"));
    }

    bool WaitCondition()
    {
        state.text = "GPS 꺼져 있음";
        return Input.location.isEnabledByUser;
    }

    IEnumerator GetLocation()
    {
        Permission.RequestUserPermission(Permission.FineLocation);

        yield return new WaitUntil(WaitCondition);

        // if (!Input.location.isEnabledByUser)
        // {
        //     Debug.Log("GPS 꺼져 있음");
        //     state.text = "GPS 꺼져 있음";
        //     yield break;
        // }

        Input.location.Start();

        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("위치 못 가져옴");
            state.text = "위치 못 가져옴";
            yield break;
        }
        else
        {
            float latitude = Input.location.lastData.latitude;
            float longitude = Input.location.lastData.longitude;
            Debug.Log($"위도: {latitude}, 경도: {longitude}");
            state.text = $"위도: {latitude}, 경도: {longitude}";

            Vector2 gridXY = KMA_GridConverter.LatLonToGrid(latitude, longitude);
            int nx = (int)gridXY.x;
            int ny = (int)gridXY.y;
            Debug.Log($"격자 좌표: nx = {nx}, ny = {ny}");

            baseDate = DateTime.Now.ToString(("yyyyMMdd"));
            baseTime = DateTime.Now.ToString(("HHmm"));

            // 날씨 API로 넘어가기
            StartCoroutine(GetWeather(baseDate, baseTime, nx, ny));
        }

        Input.location.Stop(); // 위치 서비스 종료
    }

    [System.Serializable]
    public class WeatherItem
    {
        public string category;
        public string obsrValue;
    }

    [System.Serializable]
    public class WeatherItemList
    {
        public List<WeatherItem> item;
    }

    [System.Serializable]
    public class WeatherItems
    {
        public WeatherItemList itemList;
    }

    [System.Serializable]
    public class WeatherBody
    {
        public WeatherItems items;
    }

    [System.Serializable]
    public class WeatherResponse
    {
        public WeatherBody body;
    }

    [System.Serializable]
    public class WeatherRoot
    {
        public WeatherResponse response;
    }

    IEnumerator GetWeather(string baseDate, string baseTime, int nx = 60, int ny = 127)
    {
        string serviceKey = "vgmMOjb9HXtZVJOcHkChOttoFTbGqaTPIXoECVG7JG17ggjxxKhctweGOp02xjAKQwHeXxcB3op4yfT4b6mc9Q%3D%3D";
        string url = $"https://apis.data.go.kr/1360000/VilageFcstInfoService_2.0/getUltraSrtNcst"
            + $"?serviceKey={serviceKey}"
            + $"&pageNo=1&numOfRows=1000&dataType=JSON"
            + $"&base_date={baseDate}&base_time={baseTime}&nx={nx}&ny={ny}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string rawJson = request.downloadHandler.text;
            string fixedJson = FixJson(rawJson);

            WeatherRoot root = JsonUtility.FromJson<WeatherRoot>(fixedJson);
            var items = root.response.body.items.itemList.item;

            int resultT = 0;
            int resultR = 0;
            int resultC = 0;

            foreach (var item in items)
            {
                if (item.category == "T1H")
                    resultT = int.Parse(item.obsrValue);
                else if (item.category == "PTY")
                    resultR = int.Parse(item.obsrValue);
                else if (item.category == "SKY")
                    resultC = int.Parse(item.obsrValue);
            }

            ApplyWeatherRainyEffect(resultR);
            ApplyWeatherCloudEffect(resultC);
        }
        else
        {
            Debug.LogError(request.error);

            state.text = "날씨 정보 요청 실패: " + request.error;
        }
    }

    string FixJson(string value)
    {
        value = "{\"weather\":" + value.Split(new[] { "\"weather\":" }, System.StringSplitOptions.None)[1];
        return value;
    }

    void ApplyWeatherRainyEffect(int weather)
    {
        sunny.SetActive(false);
        rain.SetActive(false);
        cloud.SetActive(false);
        switch (weather)
        {
            case 0:
                sunny.SetActive(true);
                nowWeather = WeatherState.Sunny;
                break;

            case 1:
            case 2:
            case 4:
                rain.SetActive(true);
                nowWeather = WeatherState.Rainy;
                break;

            case 3:
                snow.SetActive(true);
                nowWeather = WeatherState.Snowy;
                break;

            default:
                break;
        }
    }
    void ApplyWeatherCloudEffect(int weather)
    {
        if (nowWeather == WeatherState.Sunny)
        {
            switch (weather)
            {
                case 1:
                    RenderSettings.skybox = lightskybox;
                    break;

                case 3:
                    RenderSettings.skybox = littlecloudskybox;
                    nowWeather = WeatherState.Cloudy;
                    break;

                case 4:
                    RenderSettings.skybox = cloudskybox;
                    nowWeather = WeatherState.Cloudy;
                    break;

                default:
                    break;
            }
        }
        else
        {
            RenderSettings.skybox = cloudskybox;
        }
    }
}
