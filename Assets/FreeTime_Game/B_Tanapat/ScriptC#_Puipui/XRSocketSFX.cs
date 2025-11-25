using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSocketInteractor))]
public class XRSocketSFX : MonoBehaviour
{
    public AudioClip attachClip;
    public AudioClip detachClip;
    [Range(0f,1f)] public float volume = 0.8f;
    public Vector2 pitchRandomRange = new Vector2(0.98f, 1.02f);

    AudioSource src;
    XRSocketInteractor socket;

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;

        socket.selectEntered.AddListener(_ => Play(attachClip));
        socket.selectExited.AddListener(_ => Play(detachClip));
    }

    void OnDestroy()
    {
        if (socket == null) return;
        socket.selectEntered.RemoveAllListeners();
        socket.selectExited.RemoveAllListeners();
    }

    void Play(AudioClip clip)
    {
        if (!clip) return;
        src.pitch = Random.Range(pitchRandomRange.x, pitchRandomRange.y);
        src.PlayOneShot(clip, volume);
    }
}