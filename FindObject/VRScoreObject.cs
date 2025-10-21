using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections; 

[RequireComponent(typeof(Collider))]
public class VRScoreObject : MonoBehaviour
{
    [Header("Effect Settings")]
    public GameObject sparkleEffectPrefab;  
    public AudioClip scoreSound;             
    public float soundVolume = 1f;
    public float soundDelay = 0.7f;            

    private bool isPressed = false;
    private bool hasPlayedEffect = false;
    private XRSimpleInteractable interactable;
    private AudioSource audioSource;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.activated.AddListener(OnActivated);

        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; 
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.activated.RemoveListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (args.interactableObject != interactable)
            return;
        if (isPressed) return;
        isPressed = true;

        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore();
            Debug.Log($"correct [{name}] score this: {GameManager.Instance.GetScore()}");
        }

        
        if (!hasPlayedEffect && sparkleEffectPrefab != null)
        {
            hasPlayedEffect = true;
            GameObject effect = Instantiate(sparkleEffectPrefab, transform.position, Quaternion.identity);
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Simulate(0, true, true);
                ps.Play(true);
            }
        }

        
        if (scoreSound != null)
        {
            StartCoroutine(PlaySoundAfterDelay(soundDelay));
        }

        
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }

    private IEnumerator PlaySoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.PlayOneShot(scoreSound, soundVolume);
    }
}
