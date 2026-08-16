using UnityEngine;

public class SoundManager : MonoBehaviour
{

    private AudioSource audioSource;
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip eatSound;
    [SerializeField] private AudioClip characterChangeSound;



    void Start()
    {
        audioSource = GetComponents<AudioSource>()[1];
    }


    public void PlayEnemyHitSound()
    {
        if (enemyHitSound != null)
        {
            audioSource.PlayOneShot(enemyHitSound);
        }
    }

    public void PlayEatSound()
    {
        if (enemyHitSound != null)
        {
            audioSource.PlayOneShot(eatSound);
        }
    }

    public void PlayCharacterChangeSound()
    {
        if (characterChangeSound != null)
        {
            audioSource.PlayOneShot(characterChangeSound);
        }
    }
}
