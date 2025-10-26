using UnityEngine;

public class AmbientSound : MonoBehaviour
{
    public AudioClip ambientClip;
    [Range(0f,1f)] public float volume = 1f;
    public bool loop = true;
    public bool playOnStart = true;
    public bool persistAcrossScenes = true;

    private AudioSource src;

    void Start()
    {
        src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        src.volume = volume;

        if (ambientClip != null) src.clip = ambientClip;

        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        if (playOnStart && src.clip != null)
            src.Play();
    }

    void Update()
    {
        if (!loop && src != null && src.clip != null && !src.isPlaying)
        {
            src.Play();
        }
    }
}