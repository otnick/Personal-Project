using UnityEngine;
using UnityEngine.Audio;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] audioClips;
    private AudioSource audioSource;
    void Start()
    {
    }
    // play a random song and keep player alaive between scenes
    void Awake()
    {
        // Sicherstellen, dass nur ein Player existiert
        var objs = GameObject.FindGameObjectsWithTag("Music");
        if (objs.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true; 
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;

        if (audioClips.Length > 0)
        {
            int randomIndex = Random.Range(0, audioClips.Length);
            audioSource.clip = audioClips[randomIndex];
            audioSource.Play();
        }
    }
    void Update()
    {
        
    }
}
