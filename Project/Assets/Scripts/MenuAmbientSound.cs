using UnityEngine;

public class MenuAmbientSound : MonoBehaviour
{
    public AudioClip menuClip;
    [Range(0f,1f)] public float menuVolume = 1f;

    public string ambientObjectName = "AmbientAudio";

    private AudioSource menuSource;
    private AudioSource ambientSource;

    void Awake()
    {
        menuSource = GetComponent<AudioSource>();
        if (menuSource == null) menuSource = gameObject.AddComponent<AudioSource>();
        menuSource.playOnAwake = false;
        menuSource.loop = true;
        menuSource.spatialBlend = 0f;
        menuSource.volume = menuVolume;
        if (menuClip != null) menuSource.clip = menuClip;

        var ambientGO = GameObject.Find(ambientObjectName);
        if (ambientGO != null) ambientSource = ambientGO.GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (ambientSource != null && ambientSource.isPlaying) ambientSource.Pause();
        if (menuSource.clip != null) menuSource.Play();
    }

    void OnDisable()
    {
        if (menuSource.isPlaying) menuSource.Stop();
        if (ambientSource != null) ambientSource.UnPause();
    }
}