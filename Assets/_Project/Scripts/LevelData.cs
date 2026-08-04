using UnityEngine;

[System.Serializable]
public class LevelData
{
    public string avatarName;
    public GameObject playerAvatar;

    public GameObject[] playerCompanions;
    public bool playerCanHaveCompanion;
    public bool playerOwnsCompanion;

    public int maxLevelScore;

    public GameObject[] food;
    public ParticleSystem foodCollectEffect;
}