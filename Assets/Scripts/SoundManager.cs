using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = Object.FindAnyObjectByType<SoundManager>();
#else
                instance = Object.FindObjectOfType<SoundManager>();
#endif
                if (instance == null)
                {
                    GameObject go = new GameObject("SoundManager");
                    instance = go.AddComponent<SoundManager>();
                }
            }
            return instance;
        }
    }

    [Header("Audio Clips")]
    [Tooltip("Walking sound - Gravel.mp3 / Gravel.ogg")]
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip fallClip;
    [SerializeField] private AudioClip explosionClip;

    [Header("Sound Variation Arrays (Optional)")]
    [SerializeField] private AudioClip[] explosionClips;
    [SerializeField] private AudioClip[] jumpClips;
    [SerializeField] private AudioClip[] hurtClips;
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField] private AudioClip[] fallClips;

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float walkVolume = 0.85f;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource footstepSource;

    public AudioClip WalkClip => walkClip;
    public AudioClip JumpClip => jumpClip;
    public AudioClip HurtClip => hurtClip;
    public AudioClip DeathClip => deathClip;
    public AudioClip FallClip => fallClip;
    public AudioClip ExplosionClip => explosionClip;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        EnsureClipsLoaded();
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D
        }

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0f; // 2D
        }
    }

    public void EnsureClipsLoaded()
    {
#if UNITY_EDITOR
        if (walkClip == null)
        {
            walkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Gravel.ogg");
            if (walkClip == null) walkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/Gravel.mp3");
        }

        if (jumpClip == null) jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Jump_v1.ogg");
        if (hurtClip == null) hurtClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Hurt_v1.ogg");
        if (deathClip == null) deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Death_v1.ogg");
        if (fallClip == null) fallClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Fall_v1.ogg");
        if (explosionClip == null) explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Explosion_v1.ogg");

        if (explosionClips == null || explosionClips.Length == 0)
        {
            explosionClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Explosion_v1.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Explosion_v2.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Explosion_v3.ogg")
            };
        }

        if (jumpClips == null || jumpClips.Length == 0)
        {
            jumpClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Jump_v1.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Jump_v2.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Jump_v3.ogg")
            };
        }

        if (hurtClips == null || hurtClips.Length == 0)
        {
            hurtClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Hurt_v1.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Hurt_v2.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Hurt_v3.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Hurt_v4.ogg")
            };
        }

        if (deathClips == null || deathClips.Length == 0)
        {
            deathClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Death_v1.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Death_v2.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Death_v3.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Death_v4.ogg")
            };
        }

        if (fallClips == null || fallClips.Length == 0)
        {
            fallClips = new AudioClip[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Fall_v1.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Fall_v2.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Fall_v3.ogg"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/OGG/01_Fall_v4.ogg")
            };
        }
#endif
    }

    // -------------------------------------------------------------
    // PLAY METHODS
    // -------------------------------------------------------------

    public void PlayWalk(float volumeScale = 1f)
    {
        EnsureAudioSources();
        if (walkClip == null) EnsureClipsLoaded();
        if (walkClip == null || footstepSource == null) return;

        footstepSource.pitch = Random.Range(0.92f, 1.08f);
        footstepSource.PlayOneShot(walkClip, masterVolume * walkVolume * volumeScale);
    }

    public void PlayJump(float volumeScale = 1f)
    {
        AudioClip clip = GetRandomClip(jumpClips, jumpClip);
        PlayOneShotSFX(clip, volumeScale, Random.Range(0.95f, 1.05f));
    }

    public void PlayHurt(float volumeScale = 1f)
    {
        AudioClip clip = GetRandomClip(hurtClips, hurtClip);
        PlayOneShotSFX(clip, volumeScale, Random.Range(0.95f, 1.05f));
    }

    public void PlayDeath(float volumeScale = 1f)
    {
        AudioClip clip = GetRandomClip(deathClips, deathClip);
        PlayOneShotSFX(clip, volumeScale, 1f);
    }

    public void PlayFall(float volumeScale = 1f)
    {
        AudioClip clip = GetRandomClip(fallClips, fallClip);
        PlayOneShotSFX(clip, volumeScale, 1f);
    }

    public void PlayExplosion(Vector3? position = null, float volumeScale = 1f)
    {
        AudioClip clip = GetRandomClip(explosionClips, explosionClip);
        if (clip == null)
        {
            EnsureClipsLoaded();
            clip = GetRandomClip(explosionClips, explosionClip);
        }
        if (clip == null) return;

        float volume = masterVolume * sfxVolume * volumeScale;
        if (position.HasValue)
        {
            AudioSource.PlayClipAtPoint(clip, position.Value, volume);
        }
        else
        {
            PlayOneShotSFX(clip, volumeScale, Random.Range(0.92f, 1.08f));
        }
    }

    /// <summary>
    /// Whenever a bomb is initiated (spawned, thrown or detonated), plays the explosion sound effect.
    /// </summary>
    public void PlayBombInitiated(Vector3? position = null, float volumeScale = 1f)
    {
        PlayExplosion(position, volumeScale);
    }

    private void PlayOneShotSFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        EnsureAudioSources();
        if (clip == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, masterVolume * sfxVolume * volumeScale);
    }

    private AudioClip GetRandomClip(AudioClip[] clips, AudioClip fallback)
    {
        if (clips != null && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            if (clips[index] != null) return clips[index];
        }
        return fallback;
    }

    // -------------------------------------------------------------
    // LEVEL RESTART METHODS
    // -------------------------------------------------------------

    public static void RestartCurrentLevel()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(currentScene))
        {
            SceneManager.LoadScene(currentScene);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public static void LoadLevel1()
    {
        SceneManager.LoadScene("Leve1");
    }

    public static void LoadLevel2()
    {
        SceneManager.LoadScene("Leve2");
    }
}
