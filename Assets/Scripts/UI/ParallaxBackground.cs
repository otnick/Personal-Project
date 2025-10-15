using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public float scrollSpeed = 0.005f;
    public Renderer rend;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!rend) rend = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        float offset = Time.time * scrollSpeed;
        rend.material.mainTextureOffset = new Vector2(offset, 0);
    }
}
