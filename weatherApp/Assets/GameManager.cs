using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject sunny;
    public GameObject rain;
    public GameObject cloud;
    public Text state;
    public Text time;

    public Material lightskybox;
    public Material cloudskybox;

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        StartCoroutine(GetLocation());
    }

    void Update()
    {
        time.text = DateTime.Now.ToString(("yyyy-MM-dd tt HH:mm:ss"));
    }

    bool WaitCondition() {
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

            // 날씨 API로 넘어가기
            StartCoroutine(GetWeather(latitude, longitude));
        }

        Input.location.Stop(); // 위치 서비스 종료
    }

    [System.Serializable]
    public class Weather
    {
        public string main; // "Rain", "Clear", 등
    }

    [System.Serializable]
    public class WeatherData
    {
        public Weather[] weather;
    }

    IEnumerator GetWeather(float lat, float lon)
    {
        string apiKey = "6d6e46e067938e1eb676e7b2aaf1d827";
        string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric";

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            WeatherData weatherData = JsonUtility.FromJson<WeatherData>(FixJson(json));

            string weather = weatherData.weather[0].main;
            Debug.Log($"현재 날씨: {weather}");
            state.text = $"현재 날씨: {weather}";

            ApplyWeatherEffect(weather);
        }
        else
        {
            Debug.Log("날씨 정보 요청 실패: " + www.error);
            
            state.text = "날씨 정보 요청 실패: " + www.error;
        }
    }

    string FixJson(string value)
    {
        value = "{\"weather\":" + value.Split(new[] { "\"weather\":" }, System.StringSplitOptions.None)[1];
        return value;
    }

    void ApplyWeatherEffect(string weather)
    {
        switch (weather)
        {
            case "Clear":
                Sunny();
                break;

            case "Rain":
                Rain();
                break;
                
            case "Clouds":
                Cloud();
                break;

            default:
                sunny.SetActive(false);
                rain.SetActive(false);
                cloud.SetActive(false);
                break;
        }
    }

    private void Sunny(){
        sunny.SetActive(true);
        rain.SetActive(false);
        cloud.SetActive(false);

        RenderSettings.skybox = lightskybox;
    }
    
    private void Rain(){
        sunny.SetActive(false);
        rain.SetActive(true);
        cloud.SetActive(false);

        RenderSettings.skybox = cloudskybox;
    }
    
    private void Cloud(){
        sunny.SetActive(false);
        rain.SetActive(false);
        cloud.SetActive(true);

        RenderSettings.skybox = cloudskybox;
    }
}
