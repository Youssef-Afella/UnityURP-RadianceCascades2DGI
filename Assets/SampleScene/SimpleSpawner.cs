using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SimpleSpawner : MonoBehaviour, IPointerClickHandler
{
    public GameObject circle;

    public Volume volume;

    List<Transform> balls = new List<Transform>();

    void Start()
    {

    }

    float timer = 0;
    int frames = 0;
    float fps = 0;
    void LateUpdate()
    {
        if (timer > 0.2f)
        {
            transform.parent.Find("FPS").GetComponent<Text>().text = "FPS : " + Mathf.Round(fps / frames);
            timer = 0;
            frames = 0;
            fps = 0;
        }
        frames++;
        fps += 1 / Time.deltaTime;
        timer += Time.deltaTime;

        transform.parent.Find("Slider").Find("Label").GetComponent<Text>().text = "Render Scale : "+ transform.parent.Find("Slider").GetComponent<Slider>().value.ToString("0.00");
    }

    public void UpdateRenderScale(float input)
    {
        if (volume.profile.TryGet<RadianceCascades2DGIVolume>(out RadianceCascades2DGIVolume r))
        {
            r.renderScale.value = input;
        }
    }

    public void UpdateSkyRadiance(bool input)
    {
        if (volume.profile.TryGet<RadianceCascades2DGIVolume>(out RadianceCascades2DGIVolume r))
        {
            r.skyRadiance.value = input;
        }
    }

    public void DestroyAllBalls() {
        foreach (Transform t in balls) {
            Destroy(t.gameObject);
        }
        balls = new List<Transform>();
    }

    float hue = 0;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (hue > 1)
        {
            hue -= 1;
        }

        Vector2 position = eventData.position;
        position = Camera.main.ScreenToWorldPoint(position);

        Transform t = Instantiate(circle, position, Quaternion.identity).transform;
        balls.Add(t);
        t.localScale = Vector3.one * Random.Range(0.2f, 0.4f);

        Color color = Color.HSVToRGB(hue, 1, 1);
        t.GetComponent<SpriteRenderer>().color = color;
        t.GetChild(0).GetComponent<TrailRenderer>().startColor = color;
        t.GetChild(0).GetComponent<TrailRenderer>().endColor = color;

        Vector2 dir = new Vector2(Random.value > 0.5 ? 1 : -1, Random.value > 0.5 ? 1 : -1);
        t.GetComponent<Rigidbody2D>().velocity = new Vector2(Random.Range(4, 6), Random.Range(4, 6)) * dir;

        hue += 0.11f;
    }
}
