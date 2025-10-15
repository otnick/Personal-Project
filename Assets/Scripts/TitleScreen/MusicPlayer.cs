using UnityEngine;
using UnityEngine.Audio;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] audioClips;
    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }
    
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

        // AudioSource automatisch anlegen (wenn nicht vorhanden)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;           // Musik soll durchlaufen
        audioSource.playOnAwake = false;   // erst starten, wenn wir’s sagen
        audioSource.volume = 0.5f;

        // Zufälligen Song starten
        if (audioClips.Length > 0)
        {
            int randomIndex = Random.Range(0, audioClips.Length);
            audioSource.clip = audioClips[randomIndex];
            audioSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
